// BankAccount.Infrastructure/InMemoryCommandLog.cs (revised for Chapter 8)
#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BankAccount.Commands;

namespace BankAccount.Infrastructure;

/// <summary>
/// An in-memory, append-only log of command records with per-entity sequence
/// number locking. This revision replaces the single global SemaphoreSlim
/// from Chapter 7 with a per-entity lock, so concurrent appends to different
/// entities do not contend with each other.
/// </summary>
public sealed class InMemoryCommandLog : ICommandLog, IDisposable
{
    // The backing store. CommandRecords are never removed or modified.
    // We use a lock around writes because two concurrent appends for *different*
    // entities each hold different per-entity semaphores, yet both call
    // _records.Add() on the same shared list. List<T>.Add is not thread-safe
    // for concurrent writes from different threads. The _recordsLock below
    // guards only the Add() call; per-entity semaphores guard sequence
    // number assignment, which is the more expensive operation.
    private readonly List<CommandRecord> _records = new();
    private readonly object _recordsLock = new();

    // Per-entity semaphores. ConcurrentDictionary ensures that the semaphore
    // for a given entity is created exactly once, even under concurrent load.
    private readonly ConcurrentDictionary<int, SemaphoreSlim> _entityLocks = new();

    // Maximum number of times we will retry a failed sequence assignment before
    // propagating the exception to the caller.
    private const int MaxRetries = 3;

    /// <summary>
    /// Appends a command to the log for the given entity.
    /// Acquires an entity-scoped lock to ensure sequence number uniqueness.
    /// Retries up to <see cref="MaxRetries"/> times if a sequence collision is detected.
    /// </summary>
    public async Task AppendAsync<TCommand>(int entityId, TCommand command)
        where TCommand : notnull
    {
        ArgumentNullException.ThrowIfNull(command);

        // Get or create the per-entity lock.
        var entityLock = _entityLocks.GetOrAdd(entityId, _ => new SemaphoreSlim(1, 1));

        await entityLock.WaitAsync();
        try
        {
            await AppendWithRetryAsync(entityId, command);
        }
        finally
        {
            entityLock.Release();
        }
    }

    /// <summary>
    /// Loads all command records for the specified entity in ascending sequence order.
    /// </summary>
    public IReadOnlyList<CommandRecord> Load(int entityId)
    {
        lock (_recordsLock)
        {
            return _records
                .Where(r => r.EntityId == entityId)
                .OrderBy(r => r.SequenceNo)
                .ToList()
                .AsReadOnly();
        }
    }

    /// <summary>
    /// Returns the next available sequence number for the specified entity without
    /// acquiring the entity lock. Intended for diagnostics and testing only.
    /// In production, sequence number assignment always occurs inside AppendAsync.
    /// </summary>
    public int PeekNextSequenceNo(int entityId)
        => GetCurrentMaxSequenceNo(entityId) + 1;

    /// <summary>The total number of records across all entities.</summary>
    public int TotalCount
    {
        get
        {
            return _records.Count;
        }
    }

    // Implements the append with an optimistic retry loop.
    //
    // Important distinction between in-memory and SQL implementations:
    // In this in-memory version, the entity lock prevents true sequence number
    // collisions — so the retry loop is dead code under normal execution.
    // It is retained here solely to mirror the structural pattern of the SQL
    // implementation in Chapter 9. In the SQL EfCommandLog, there is NO
    // application-level lock: the database's unique constraint on
    // (EntityId, SequenceNo) is the only protection, and the retry loop is
    // the sole mechanism for handling a constraint violation. Do not infer
    // from this in-memory version that SQL retries would also run inside a
    // held lock — they do not.
    private async Task AppendWithRetryAsync<TCommand>(int entityId, TCommand command)
        where TCommand : notnull
    {
        for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            int nextSequenceNo = GetCurrentMaxSequenceNo(entityId) + 1;

            bool collision = _records.Any(r =>
                r.EntityId == entityId && r.SequenceNo == nextSequenceNo);

            if (collision)
            {
                if (attempt == MaxRetries - 1)
                {
                    throw new InvalidOperationException(
                        $"Failed to assign sequence number for entity {entityId} " +
                        $"after {MaxRetries} attempts. Possible lock contention or data corruption.");
                }

                await Task.Yield();
                continue;
            }

            string payloadData = JsonSerializer.Serialize(command);
            string commandType = command.GetType().AssemblyQualifiedName
                ?? throw new InvalidOperationException(
                    $"Cannot determine AssemblyQualifiedName for {command.GetType().Name}.");

            var record = new CommandRecord
            {
                EntityId = entityId,
                SequenceNo = nextSequenceNo,
                CommandType = commandType,
                PayloadData = payloadData,
                Timestamp = DateTimeOffset.UtcNow
            };

            // Guard the Add() call with _recordsLock so that concurrent appends
            // for different entities (which hold different per-entity semaphores)
            // cannot corrupt the shared List<T> by calling Add() simultaneously.
            lock (_recordsLock)
            {
                _records.Add(record);
            }

            return; // Success — exit the retry loop.
        }
    }

    private int GetCurrentMaxSequenceNo(int entityId)
    {
        lock (_recordsLock)
        {
            return _records
                .Where(r => r.EntityId == entityId)
                .Select(r => r.SequenceNo)
                .DefaultIfEmpty(0)
                .Max();
        }
    }

    /// <summary>Releases all per-entity semaphores.</summary>
    public void Dispose()
    {
        foreach (var semaphore in _entityLocks.Values)
            semaphore.Dispose();
    }
}
