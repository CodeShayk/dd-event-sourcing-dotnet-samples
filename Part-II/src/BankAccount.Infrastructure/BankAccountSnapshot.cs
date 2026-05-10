// BankAccount.Infrastructure/BankAccountSnapshot.cs
#nullable enable

using System;

namespace BankAccount.Infrastructure;

/// <summary>
/// Represents a periodic checkpoint of a BankAccount's state.
/// Stored via IEntityStore alongside the command log.
///
/// The snapshot records the full serialised state of the account and the
/// sequence number at which the snapshot was taken. Replay after this
/// snapshot only needs to process commands with SequenceNo > LastSequenceNo.
/// </summary>
public sealed class BankAccountSnapshot : IEntity
{
    /// <summary>
    /// The entity ID, which must match the BankAccount.Id this snapshot belongs to.
    /// Required by IEntity.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The JSON-serialised state of the BankAccount at the time this snapshot was taken.
    /// Deserialise using System.Text.Json with the same options used during serialisation.
    /// </summary>
    public string SerializedState { get; set; } = string.Empty;

    /// <summary>
    /// The SequenceNo of the last command included in this snapshot.
    /// Replay should load commands with SequenceNo strictly greater than this value.
    /// </summary>
    public int LastSequenceNo { get; set; }

    /// <summary>
    /// The UTC time at which this snapshot was taken. For diagnostics only.
    /// </summary>
    public DateTimeOffset TakenAt { get; set; }
}
