// BankAccount.Commands/CommandRecord.cs
#nullable enable

using System;

namespace BankAccount.Commands;

/// <summary>
/// Represents a single stored command entry in the append-only command log.
/// Every command that mutates a bank account is recorded as a CommandRecord.
/// Records are immutable once written: they may never be modified or deleted.
/// </summary>
public sealed class CommandRecord
{
    /// <summary>
    /// The identifier of the entity (bank account) this command belongs to.
    /// All commands for a single account share the same EntityId.
    /// </summary>
    public int EntityId { get; init; }

    /// <summary>
    /// The monotonically increasing sequence number for commands belonging to
    /// this entity. Sequence numbers start at 1 and increase by 1 for each
    /// appended command. The ordering of commands during replay is determined
    /// solely by SequenceNo, never by Timestamp.
    /// </summary>
    public int SequenceNo { get; init; }

    /// <summary>
    /// The assembly-qualified type name of the command class. Used during
    /// replay to deserialise PayloadData back into the correct command type.
    /// Example: "BankAccount.Commands.DepositMoneyCommand, BankAccount.Commands"
    /// </summary>
    public string CommandType { get; init; } = string.Empty;

    /// <summary>
    /// The JSON-serialised payload of the command. Contains all data required
    /// to reconstruct and re-execute the command during replay.
    /// </summary>
    public string PayloadData { get; init; } = string.Empty;

    /// <summary>
    /// The UTC timestamp at which this command was appended to the log.
    /// Used for audit and diagnostic purposes only — never for ordering.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }
}
