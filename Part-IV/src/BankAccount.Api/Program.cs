// Chapter 20-21 — Program.cs (Part IV: in-memory stores, no EF dependency)
using BankAccount.Api.Middleware;
using BankAccount.Api.Validators;
using BankAccount.Domain.Sagas;
using FluentValidation;
using FluentValidation.AspNetCore;
using SourceFlow;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// FluentValidation (Layer 1 — API boundary validation)
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<DepositRequestValidator>();

// Register SourceFlow.Net with the domain assembly
builder.Services.UseSourceFlow(typeof(BankAccountSaga).Assembly);

// In-memory stores for Part IV (EF stores are introduced in Part V)
builder.Services.AddSingleton<IEntityStore, InMemoryEntityStore>();
builder.Services.AddSingleton<ICommandStore, InMemoryCommandStore>();
builder.Services.AddSingleton<IViewModelStore, InMemoryViewModelStore>();

var app = builder.Build();

// Domain exception handler before other middleware
app.UseDomainExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();
app.Run();

// Make Program accessible for WebApplicationFactory<Program> in tests
public partial class Program { }

// --- In-memory store implementations for Part IV ---
// (EF Core stores replace these in Part V)

internal class InMemoryEntityStore : IEntityStore
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<(Type, int), object> _store = new();

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

internal class InMemoryCommandStore : ICommandStore
{
    private readonly System.Collections.Concurrent.ConcurrentBag<SourceFlow.Messaging.Commands.CommandData> _commands = new();

    public Task Append(SourceFlow.Messaging.Commands.CommandData commandData)
    {
        _commands.Add(commandData);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<SourceFlow.Messaging.Commands.CommandData>> Load(int aggregateId)
    {
        var result = _commands.Where(c => c.EntityId == aggregateId).OrderBy(c => c.SequenceNo);
        return Task.FromResult<IEnumerable<SourceFlow.Messaging.Commands.CommandData>>(result);
    }
}

internal class InMemoryViewModelStore : IViewModelStore
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<(Type, int), object> _store = new();

    public Task<TViewModel> Get<TViewModel>(int id) where TViewModel : class, SourceFlow.Projections.IViewModel
    {
        _store.TryGetValue((typeof(TViewModel), id), out var vm);
        return Task.FromResult(vm as TViewModel)!;
    }

    public Task<TViewModel> Persist<TViewModel>(TViewModel model) where TViewModel : class, SourceFlow.Projections.IViewModel
    {
        _store[(typeof(TViewModel), model.Id)] = model;
        return Task.FromResult(model);
    }

    public Task Delete<TViewModel>(TViewModel model) where TViewModel : class, SourceFlow.Projections.IViewModel
    {
        _store.TryRemove((typeof(TViewModel), model.Id), out _);
        return Task.CompletedTask;
    }
}
