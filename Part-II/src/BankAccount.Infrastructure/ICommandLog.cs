// BankAccount.Infrastructure/ICommandLog.cs
#nullable enable

using System.Collections.Generic;
using BankAccount.Commands;

namespace BankAccount.Infrastructure;

/// <summary>
/// Minimal abstraction over an append-only command log.
/// Provides the read (Load) side needed by CommandReplayer.
/// SnapshotEntityStore also depends on this interface for the same reason.
///
/// In SourceFlow.Net, the framework equivalent is ICommandStore, which
/// exposes Load(int aggregateId) returning IEnumerable&lt;CommandData&gt;.
/// Chapter 12 maps this hand-built interface to the framework's version.
/// </summary>
public interface ICommandLog
{
    /// <summary>
    /// Loads all command records for the specified entity in ascending sequence order.
    /// Returns an empty collection if no records exist.
    /// </summary>
    /// <param name="entityId">The ID of the entity whose commands should be loaded.</param>
    IReadOnlyList<CommandRecord> Load(int entityId);
}
