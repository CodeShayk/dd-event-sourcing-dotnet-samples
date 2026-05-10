// Test infrastructure — DI helpers for in-memory testing
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using SourceFlow;
using SourceFlow.Messaging.Commands;
using SourceFlow.Projections;
using BankAccount.Domain.Sagas;

namespace BankAccount.Tests.Infrastructure;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers SourceFlow.Net with in-memory stores for integration testing.
    /// </summary>
    public static IServiceCollection AddInMemorySourceFlow(this IServiceCollection services)
    {
        services.UseSourceFlow(typeof(BankAccountSaga).Assembly);
        services.AddScoped<IEntityStore, TestInMemoryEntityStore>();
        services.AddScoped<ICommandStore, TestInMemoryCommandStore>();
        services.AddScoped<IViewModelStore, TestInMemoryViewModelStore>();
        return services;
    }

    /// <summary>
    /// Replaces store registrations with in-memory stores (for WebApplicationFactory overrides).
    /// </summary>
    public static IServiceCollection AddInMemorySourceFlowStores(this IServiceCollection services)
    {
        services.AddSingleton<IEntityStore, TestInMemoryEntityStore>();
        services.AddSingleton<ICommandStore, TestInMemoryCommandStore>();
        services.AddSingleton<IViewModelStore, TestInMemoryViewModelStore>();
        return services;
    }
}

internal class TestInMemoryEntityStore : IEntityStore
{
    private readonly ConcurrentDictionary<(Type, int), object> _store = new();

    public Task<TEntity> Get<TEntity>(int id) where TEntity : class, IEntity
    {
        _store.TryGetValue((typeof(TEntity), id), out var entity);
        return Task.FromResult(entity as TEntity)!;
    }

    public Task<TEntity> Persist<TEntity>(TEntity entity) where TEntity : class, IEntity
    {
        _store[(typeof(TEntity), entity.Id)] = entity;
        return Task.FromResult(entity);
    }

    public Task Delete<TEntity>(TEntity entity) where TEntity : class, IEntity
    {
        _store.TryRemove((typeof(TEntity), entity.Id), out _);
        return Task.CompletedTask;
    }
}

internal class TestInMemoryCommandStore : ICommandStore
{
    private readonly ConcurrentBag<CommandData> _commands = new();

    public Task Append(CommandData commandData)
    {
        _commands.Add(commandData);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<CommandData>> Load(int aggregateId)
    {
        var result = _commands.Where(c => c.EntityId == aggregateId).OrderBy(c => c.SequenceNo);
        return Task.FromResult<IEnumerable<CommandData>>(result);
    }
}

internal class TestInMemoryViewModelStore : IViewModelStore
{
    private readonly ConcurrentDictionary<(Type, int), object> _store = new();

    public Task<TViewModel> Get<TViewModel>(int id) where TViewModel : class, IViewModel
    {
        _store.TryGetValue((typeof(TViewModel), id), out var vm);
        return Task.FromResult(vm as TViewModel)!;
    }

    public Task<TViewModel> Persist<TViewModel>(TViewModel model) where TViewModel : class, IViewModel
    {
        _store[(typeof(TViewModel), model.Id)] = model;
        return Task.FromResult(model);
    }

    public Task Delete<TViewModel>(TViewModel model) where TViewModel : class, IViewModel
    {
        _store.TryRemove((typeof(TViewModel), model.Id), out _);
        return Task.CompletedTask;
    }
}
