// BankAccount.Infrastructure/InMemoryEntityStore.cs
#nullable enable

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace BankAccount.Infrastructure;

/// <summary>
/// An in-memory IEntityStore implementation for testing.
/// Stores entities by type and ID. Not durable — for tests only.
/// In production, this is replaced by SourceFlow.Net's EF Core-backed entity store.
/// </summary>
public sealed class InMemoryEntityStore : IEntityStore
{
    // Stores entities by a composite key of (typeName, entityId).
    private readonly ConcurrentDictionary<string, object> _store = new();

    /// <inheritdoc />
    public Task<TEntity> Get<TEntity>(int id) where TEntity : class, IEntity
    {
        string key = MakeKey<TEntity>(id);

        if (_store.TryGetValue(key, out var value) && value is TEntity entity)
            return Task.FromResult(entity);

        // Return default — SnapshotEntityStore interprets null as "no snapshot found."
        return Task.FromResult<TEntity>(null!);
    }

    /// <inheritdoc />
    public Task<TEntity> Persist<TEntity>(TEntity entity) where TEntity : class, IEntity
    {
        string key = MakeKey<TEntity>(entity.Id);
        _store[key] = entity;
        return Task.FromResult(entity);
    }

    /// <inheritdoc />
    public Task Delete<TEntity>(TEntity entity) where TEntity : class, IEntity
    {
        string key = MakeKey<TEntity>(entity.Id);
        _store.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    private static string MakeKey<TEntity>(int id) => $"{typeof(TEntity).Name}:{id}";
}
