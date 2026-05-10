// Chapter 22 — Background service for batch projection rebuild
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SourceFlow;
using SourceFlow.Messaging.Bus;
using BankAccount.Domain.Views;

namespace BankAccount.Api.BackgroundServices;

/// <summary>
/// A one-shot background service that rebuilds projections for a batch of account IDs.
/// </summary>
public sealed class ProjectionRebuildService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProjectionRebuildService> _logger;
    private readonly IReadOnlyList<int> _accountIdsToRebuild;

    public ProjectionRebuildService(
        IServiceScopeFactory scopeFactory,
        ILogger<ProjectionRebuildService> logger,
        IReadOnlyList<int> accountIdsToRebuild)
    {
        _scopeFactory        = scopeFactory;
        _logger              = logger;
        _accountIdsToRebuild = accountIdsToRebuild;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Projection rebuild service starting. Accounts to rebuild: {Count}",
            _accountIdsToRebuild.Count);

        foreach (var accountId in _accountIdsToRebuild)
        {
            if (stoppingToken.IsCancellationRequested)
                break;

            await using var scope = _scopeFactory.CreateAsyncScope();

            var commandBus     = scope.ServiceProvider.GetRequiredService<ICommandBus>();
            var viewModelStore = scope.ServiceProvider.GetRequiredService<IViewModelStore>();

            try
            {
                var summary = await viewModelStore.Get<AccountSummaryView>(accountId);
                if (summary is not null) await viewModelStore.Delete(summary);

                var statement = await viewModelStore.Get<AccountStatementView>(accountId);
                if (statement is not null) await viewModelStore.Delete(statement);

                await commandBus.Replay(accountId);

                _logger.LogInformation("Rebuilt projections for account {Id}", accountId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to rebuild projections for account {Id}", accountId);
            }
        }

        _logger.LogInformation("Projection rebuild service complete.");
    }
}
