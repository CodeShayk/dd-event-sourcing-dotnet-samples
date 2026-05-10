// BankAccount.Infrastructure/SnapshotEntityStore.cs
#nullable enable

using System;
using System.Text.Json;
using System.Threading.Tasks;
using BankAccount.Domain;
using BankAccountEntity = BankAccount.Domain.BankAccount;

namespace BankAccount.Infrastructure;

/// <summary>
/// A wrapper around IEntityStore that adds snapshot-aware rebuild logic
/// for BankAccount entities. When a snapshot exists, Rebuild() loads only
/// the commands after the snapshot's LastSequenceNo instead of all commands.
///
/// This is a user-land extension over SourceFlow.Net's IEntityStore.
/// SourceFlow.Net v1.0.0 does not include ISnapshotStore — this class
/// demonstrates how to extend the framework's persistence layer without
/// modifying framework code.
/// </summary>
public sealed class SnapshotEntityStore
{
    private readonly IEntityStore _entityStore;
    private readonly ICommandLog _commandLog;
    private readonly SnapshotPolicy _policy;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Initialises the snapshot store with the underlying entity store, command log,
    /// and snapshot policy.
    /// </summary>
    public SnapshotEntityStore(
        IEntityStore entityStore,
        ICommandLog commandLog,
        SnapshotPolicy policy)
    {
        _entityStore = entityStore ?? throw new ArgumentNullException(nameof(entityStore));
        _commandLog = commandLog ?? throw new ArgumentNullException(nameof(commandLog));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
    }

    /// <summary>
    /// Rebuilds a BankAccount by loading the most recent snapshot (if any) and
    /// replaying only the commands after the snapshot's LastSequenceNo.
    ///
    /// If no snapshot exists, all commands are replayed (identical to
    /// CommandReplayer.Rebuild() from Chapter 10).
    /// </summary>
    public async Task<BankAccountEntity> RebuildAsync(int entityId)
    {
        // Attempt to load the most recent snapshot from the entity store.
        var snapshot = await _entityStore.Get<BankAccountSnapshot>(entityId);

        BankAccountEntity account;
        int replayFromSequenceNo;

        if (snapshot is not null && !string.IsNullOrEmpty(snapshot.SerializedState))
        {
            // Deserialise the snapshot into a BankAccount.
            var state = JsonSerializer.Deserialize<BankAccountState>(
                snapshot.SerializedState, SerializerOptions)
                ?? throw new InvalidOperationException(
                    $"Failed to deserialise snapshot for entity {entityId}.");

            account = RestoreFromState(state);
            replayFromSequenceNo = snapshot.LastSequenceNo;
        }
        else
        {
            // No snapshot — start from a fresh account and replay everything.
            account = new BankAccountEntity();
            replayFromSequenceNo = 0;
        }

        // Load and replay only the commands after the snapshot boundary.
        var records = _commandLog.Load(entityId);

        foreach (var record in records)
        {
            // Skip commands already included in the snapshot.
            if (record.SequenceNo <= replayFromSequenceNo)
                continue;

            ApplyRecord(account, record);
        }

        return account;
    }

    /// <summary>
    /// Appends a command to the log and, if the snapshot policy triggers,
    /// takes a snapshot of the current account state.
    /// </summary>
    public async Task AppendAndMaybeSnapshotAsync<TCommand>(
        int entityId,
        TCommand command,
        BankAccountEntity currentState)
        where TCommand : notnull
    {
        // Append the command — this assigns the next sequence number.
        await ((InMemoryCommandLog)_commandLog).AppendAsync(entityId, command);

        // Find the sequence number just assigned to determine if we should snapshot.
        var records = _commandLog.Load(entityId);
        int latestSequenceNo = records.Count > 0
            ? records[^1].SequenceNo
            : 0;

        if (_policy.ShouldSnapshot(latestSequenceNo))
        {
            await TakeSnapshotAsync(entityId, currentState, latestSequenceNo);
        }
    }

    // Serialises and persists the current account state as a BankAccountSnapshot.
    private async Task TakeSnapshotAsync(int entityId, BankAccountEntity account, int sequenceNo)
    {
        var state = new BankAccountState(
            account.Id,
            account.OwnerName,
            account.Balance,
            account.IsClosed);

        string serializedState = JsonSerializer.Serialize(state, SerializerOptions);

        var snapshot = new BankAccountSnapshot
        {
            Id = entityId,
            SerializedState = serializedState,
            LastSequenceNo = sequenceNo,
            TakenAt = DateTimeOffset.UtcNow
        };

        await _entityStore.Persist(snapshot);
    }

    // Restores a BankAccount from a serialised state record.
    // Note: in production systems, use a dedicated RestoreFrom(BankAccountState)
    // static factory method on BankAccount rather than reusing
    // Apply(OpenAccountCommand). The factory method makes the intent explicit
    // and decouples snapshot restoration from command handling.
    private static BankAccountEntity RestoreFromState(BankAccountState state)
    {
        var account = new BankAccountEntity();
        account.Apply(new OpenAccountCommand(state.Id, state.OwnerName, state.Balance));

        if (state.IsClosed)
            account.Apply(new CloseAccountCommand(state.Id, "Restored from snapshot"));

        return account;
    }

    // Applies a single command record to the account using the replay (Apply) path.
    private static void ApplyRecord(BankAccountEntity account, Commands.CommandRecord record)
    {
        Type? commandType = Type.GetType(record.CommandType)
            ?? throw new InvalidOperationException(
                $"Cannot resolve type '{record.CommandType}' during snapshot replay.");

        if (commandType == typeof(OpenAccountCommand))
            account.Apply(Deserialise<OpenAccountCommand>(record.PayloadData));
        else if (commandType == typeof(DepositMoneyCommand))
            account.Apply(Deserialise<DepositMoneyCommand>(record.PayloadData));
        else if (commandType == typeof(WithdrawMoneyCommand))
            account.Apply(Deserialise<WithdrawMoneyCommand>(record.PayloadData));
        else if (commandType == typeof(CloseAccountCommand))
            account.Apply(Deserialise<CloseAccountCommand>(record.PayloadData));
        else
            throw new InvalidOperationException(
                $"No Apply() registered for command type '{commandType.FullName}'.");
    }

    private static TCommand Deserialise<TCommand>(string payloadData)
    {
        return JsonSerializer.Deserialize<TCommand>(payloadData, SerializerOptions)
            ?? throw new InvalidOperationException(
                $"Deserialisation of {typeof(TCommand).Name} returned null.");
    }
}
