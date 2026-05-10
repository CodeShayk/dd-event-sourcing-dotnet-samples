// File: BankAccount.Domain/IRepository.cs
#nullable enable

namespace BankAccount.Domain;

/// <summary>
/// A simple generic repository abstraction for loading and persisting entities.
/// This is intentionally minimal — we will outgrow it by Chapter 4.
/// </summary>
/// <typeparam name="T">The entity type this repository manages.</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>Loads an entity by its integer identifier.</summary>
    /// <param name="id">The entity identifier.</param>
    /// <returns>The entity, or null if not found.</returns>
    Task<T?> GetById(int id);

    /// <summary>Persists a new or updated entity and returns the persisted instance.</summary>
    /// <param name="entity">The entity to persist.</param>
    /// <returns>The persisted entity, potentially with a database-generated Id.</returns>
    Task<T> Save(T entity);
}
