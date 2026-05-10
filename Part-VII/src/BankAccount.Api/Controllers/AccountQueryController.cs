// Chapter 18 — Read-side API controller
using Microsoft.AspNetCore.Mvc;
using SourceFlow;
using BankAccount.Domain.Views;

namespace BankAccount.Api.Controllers;

/// <summary>
/// Read-only endpoints for querying bank account view models.
/// These endpoints serve from the read model (IViewModelStore), not the
/// write model (IEntityStore). They are the "Q" in CQRS.
/// </summary>
[ApiController]
[Route("api/accounts")]
public sealed class AccountQueryController : ControllerBase
{
    private readonly IViewModelStore _viewModelStore;

    /// <summary>
    /// Initialises the controller with the view model store.
    /// Note: IViewModelStore.Get<T>(id) is the method used here — NOT Find<T>().
    /// Find<T>() belongs to IViewModelStoreAdapter, which is an internal
    /// framework layer used only inside projection handlers.
    /// </summary>
    public AccountQueryController(IViewModelStore viewModelStore)
    {
        _viewModelStore = viewModelStore;
    }

    /// <summary>
    /// Returns the current summary for a bank account.
    /// This is the primary balance-check endpoint.
    /// </summary>
    [HttpGet("{id:int}/summary")]
    [ProducesResponseType(typeof(AccountSummaryView), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetSummary(int id)
    {
        // IViewModelStore.Get<T>(id) — method name is GET, not Find
        var view = await _viewModelStore.Get<AccountSummaryView>(id);

        if (view is null)
            return NotFound(new { error = $"No account summary found for id {id}" });

        return Ok(view);
    }

    /// <summary>
    /// Returns the full transaction statement for a bank account.
    /// </summary>
    [HttpGet("{id:int}/statement")]
    [ProducesResponseType(typeof(AccountStatementView), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetStatement(int id)
    {
        var view = await _viewModelStore.Get<AccountStatementView>(id);

        if (view is null)
            return NotFound(new { error = $"No statement found for id {id}" });

        return Ok(view);
    }
}
