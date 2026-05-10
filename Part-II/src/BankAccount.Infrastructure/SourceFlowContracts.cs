// BankAccount.Infrastructure/SourceFlowContracts.cs
// These interfaces are defined in SourceFlow.Net and shown here for reference.
// In a project that references the SourceFlow NuGet package, these come from
// the package. We reproduce them here only so the chapter code is self-contained.
#nullable enable

using System.Threading.Tasks;

namespace BankAccount.Infrastructure;

/// <summary>
/// Marker interface for entities managed by IEntityStore.
/// In SourceFlow.Net: public interface IEntity { int Id { get; set; } }
/// </summary>
public interface IEntity
{
    /// <summary>The unique identifier of this entity.</summary>
    int Id { get; set; }
}

/// <summary>
/// SourceFlow.Net's entity store contract.
/// Stores, retrieves, and deletes typed entities by integer ID.
/// </summary>
public interface IEntityStore
{
    /// <summary>Retrieves an entity by its unique identifier.</summary>
    Task<TEntity> Get<TEntity>(int id) where TEntity : class, IEntity;

    /// <summary>Creates or updates an entity in the store.</summary>
    Task<TEntity> Persist<TEntity>(TEntity entity) where TEntity : class, IEntity;

    /// <summary>Deletes an entity from the store.</summary>
    Task Delete<TEntity>(TEntity entity) where TEntity : class, IEntity;
}
