// BankAccount.Infrastructure/CommandReplayer.cs
#nullable enable

using System;
using System.Text.Json;
using BankAccount.Commands;
using BankAccount.Domain;
using BankAccountEntity = BankAccount.Domain.BankAccount;

namespace BankAccount.Infrastructure;

/// <summary>
/// Rebuilds a BankAccount's state by replaying its complete command history
/// from the command log. Uses the Apply() path on BankAccount, which mutates
/// state without raising events, to ensure idempotent replay.
///
/// CommandReplayer depends on ICommandLog (not InMemoryCommandLog directly)
/// so it can be used with any backing store — in-memory, SQL, or the
/// framework's ICommandStore in Chapter 12.
///
/// This replayer is specific to BankAccount. In a general-purpose framework,
/// the type resolution and dispatch would be handled generically — exactly as
/// SourceFlow.Net's ICommandBus.Replay() does.
/// </summary>
public sealed class CommandReplayer
{
    private readonly ICommandLog _commandLog;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Initialises the replayer with the command log to read from.
    /// </summary>
    public CommandReplayer(ICommandLog commandLog)
    {
        _commandLog = commandLog ?? throw new ArgumentNullException(nameof(commandLog));
    }

    /// <summary>
    /// Rebuilds the state of a BankAccount by replaying all commands in its history.
    /// Returns a fresh BankAccount instance with the correct current state.
    /// The stored state of the account in any database is ignored.
    /// </summary>
    public BankAccountEntity Rebuild(int entityId)
    {
        var records = _commandLog.Load(entityId);

        // Start with a fresh account. No state is pre-loaded from any database.
        // The log is the only source of truth.
        var account = new BankAccountEntity();

        foreach (var record in records)
        {
            Type? commandType = Type.GetType(record.CommandType)
                ?? throw new InvalidOperationException(
                    $"Cannot resolve command type '{record.CommandType}'. " +
                    "The assembly may have been renamed or the type may have been moved. " +
                    "See Chapter 10 for type migration strategies.");

            ApplyRecord(account, commandType, record.PayloadData);
        }

        return account;
    }

    // Deserialises the payload and dispatches to the correct Apply() overload.
    private static void ApplyRecord(BankAccountEntity account, Type commandType, string payloadData)
    {
        if (commandType == typeof(OpenAccountCommand))
        {
            var command = Deserialise<OpenAccountCommand>(payloadData);
            account.Apply(command);
        }
        else if (commandType == typeof(DepositMoneyCommand))
        {
            var command = Deserialise<DepositMoneyCommand>(payloadData);
            account.Apply(command);
        }
        else if (commandType == typeof(WithdrawMoneyCommand))
        {
            var command = Deserialise<WithdrawMoneyCommand>(payloadData);
            account.Apply(command);
        }
        else if (commandType == typeof(CloseAccountCommand))
        {
            var command = Deserialise<CloseAccountCommand>(payloadData);
            account.Apply(command);
        }
        else
        {
            throw new InvalidOperationException(
                $"No Apply() overload registered for command type '{commandType.FullName}'. " +
                "Add an Apply() method to BankAccount and register the type here.");
        }
    }

    private static TCommand Deserialise<TCommand>(string payloadData)
    {
        return JsonSerializer.Deserialize<TCommand>(payloadData, SerializerOptions)
            ?? throw new InvalidOperationException(
                $"Deserialisation of {typeof(TCommand).Name} returned null. " +
                "The payload may be malformed or the type structure may have changed.");
    }
}
