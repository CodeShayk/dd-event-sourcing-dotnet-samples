// Chapter 22 — Admin endpoints for projection rebuild
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SourceFlow;
using SourceFlow.Messaging.Bus;
using BankAccount.Domain.Views;

namespace BankAccount.Api.Controllers;

/// <summary>
/// Administrative endpoints for projection rebuilding.
/// </summary>
[ApiController]
[Route("api/admin/accounts")]
[Authorize(Roles = "Admin")]
public sealed class AccountAdminController : ControllerBase
{
    private readonly ICommandBus _commandBus;
    private readonly IViewModelStore _viewModelStore;
    private readonly ILogger<AccountAdminController> _logger;

    public AccountAdminController(
        ICommandBus commandBus,
        IViewModelStore viewModelStore,
        ILogger<AccountAdminController> logger)
    {
        _commandBus     = commandBus;
        _viewModelStore = viewModelStore;
        _logger         = logger;
    }

    [HttpPost("{id:int}/rebuild-projections")]
    [ProducesResponseType(202)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> RebuildProjections(int id)
    {
        _logger.LogInformation(
            "Projection rebuild requested for account {Id} by user {User}",
            id, User.Identity?.Name);

        var existingSummary = await _viewModelStore.Get<AccountSummaryView>(id);
        if (existingSummary is not null)
            await _viewModelStore.Delete(existingSummary);

        var existingStatement = await _viewModelStore.Get<AccountStatementView>(id);
        if (existingStatement is not null)
            await _viewModelStore.Delete(existingStatement);

        await _commandBus.Replay(id);

        _logger.LogInformation("Projection rebuild complete for account {Id}", id);

        return Accepted(new
        {
            message  = $"Projection rebuild initiated for account {id}.",
            queryUrl = Url.Action("GetSummary", "AccountQuery", new { id })
        });
    }
}
