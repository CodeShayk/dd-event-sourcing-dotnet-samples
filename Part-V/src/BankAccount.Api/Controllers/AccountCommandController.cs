// Chapter 20 — Write-side API controller
using Microsoft.AspNetCore.Mvc;
using SourceFlow.Messaging.Bus;
using BankAccount.Api.Models;
using BankAccount.Domain.Commands;

namespace BankAccount.Api.Controllers;

/// <summary>
/// Write-side endpoints for the Bank Account API. All endpoints return 202 Accepted.
/// </summary>
[ApiController]
[Route("api/accounts")]
public sealed class AccountCommandController : ControllerBase
{
    private readonly ICommandBus _commandBus;

    public AccountCommandController(ICommandBus commandBus)
    {
        _commandBus = commandBus;
    }

    [HttpPost]
    [ProducesResponseType(202)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> OpenAccount([FromBody] OpenAccountRequest request)
    {
        var command = new OpenAccountCommand(
            true,
            new OpenAccountPayload(
                request.AccountNumber,
                request.AccountHolder,
                request.InitialBalance));

        await _commandBus.Publish(command);

        return Accepted(new
        {
            message = "Account opening accepted.",
        });
    }

    [HttpPost("{id:int}/deposit")]
    [ProducesResponseType(202)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Deposit(int id, [FromBody] DepositRequest request)
    {
        var command = new DepositMoneyCommand(
            id,
            new DepositPayload(request.Amount, request.Description));

        await _commandBus.Publish(command);

        return Accepted(new
        {
            message  = $"Deposit of {request.Amount:C} accepted for account {id}.",
            queryUrl = Url.Action("GetSummary", "AccountQuery", new { id })
        });
    }

    [HttpPost("{id:int}/withdraw")]
    [ProducesResponseType(202)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Withdraw(int id, [FromBody] WithdrawRequest request)
    {
        var command = new WithdrawMoneyCommand(
            id,
            new WithdrawPayload(request.Amount, request.Description));

        await _commandBus.Publish(command);

        return Accepted(new
        {
            message  = $"Withdrawal of {request.Amount:C} accepted for account {id}.",
            queryUrl = Url.Action("GetSummary", "AccountQuery", new { id })
        });
    }

    [HttpPost("{id:int}/close")]
    [ProducesResponseType(202)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CloseAccount(int id, [FromBody] CloseAccountRequest request)
    {
        var command = new CloseAccountCommand(
            id,
            new CloseAccountPayload(request.Reason));

        await _commandBus.Publish(command);

        return Accepted(new
        {
            message  = $"Account {id} closure accepted.",
            queryUrl = Url.Action("GetSummary", "AccountQuery", new { id })
        });
    }
}
