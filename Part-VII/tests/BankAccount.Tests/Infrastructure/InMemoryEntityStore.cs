// Chapter 31 — Thread-safe in-memory IEntityStore for saga unit tests
#nullable enable

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using SourceFlow;

namespace BankAccount.Tests.Infrastructure;

/// <summary>
/// A thread-safe in-memory implementation of IEntityStore for use in saga unit tests.
/// State is stored in a ConcurrentDictionary keyed by (Type, EntityId).
/// Not suitable for production — there is no persistence across process restarts.
/// </summary>
public sealed class InMemoryEntityStore : IEntityStore
{
    private readonly ConcurrentDictionary<(Type, int), object> _store = new();

    public Task<TEntity> Get<TEntity>(int id)
        where TEntity : class, IEntity
    {
        var key = (typeof(TEntity), id);

        if (_store.TryGetValue(key, out var stored) && stored is TEntity entity)
            return Task.FromResult(entity);

        return Task.FromResult<TEntity>(null!);
    }

    public Task<TEntity> Persist<TEntity>(TEntity entity)
        where TEntity : class, IEntity
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        var key = (typeof(TEntity), entity.Id);
        _store[key] = entity;

        return Task.FromResult(entity);
    }

    public Task Delete<TEntity>(TEntity entity)
        where TEntity : class, IEntity
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        var key = (typeof(TEntity), entity.Id);
        _store.TryRemove(key, out _);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Returns the count of entities currently in the store, for test assertions.
    /// </summary>
    public int Count => _store.Count;
}
