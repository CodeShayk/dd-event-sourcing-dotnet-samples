// BankAccount.Application/Sagas/ReverseDebitSaga.cs
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SourceFlow;
using SourceFlow.Messaging.Commands;
using SourceFlow.Messaging.Events;
using SourceFlow.Saga;
using BankAccount.Domain;
using BankAccount.Domain.Commands;
using BankAccountEntity = BankAccount.Domain.BankAccount;

namespace BankAccount.Application.Sagas;

/// <summary>
/// Handles ReverseDebitCommand — the compensating command for a failed transfer credit.
/// Restores the source account balance to its pre-debit state.
/// </summary>
public sealed class ReverseDebitSaga
    : Saga<BankAccountEntity>,
      IHandles<ReverseDebitCommand>
{
    public ReverseDebitSaga(
        Lazy<ICommandPublisher> commandPublisher,
        IEventQueue eventQueue,
        IEntityStoreAdapter entityStore,
        ILogger<ISaga> logger)
        : base(commandPublisher, eventQueue, entityStore, logger)
    {
    }

    /// <summary>
    /// Handles ReverseDebitCommand: adds the debited amount back to the source account.
    /// </summary>
    public Task<IEntity> Handle(IEntity entity, ReverseDebitCommand command)
    {
        var account = (BankAccountEntity)entity;

        account.Balance += command.Payload.Amount;

        logger.LogInformation(
            "Transfer {TransferId}: reversed debit of {Amount} on account {AccountId}. " +
            "Balance restored to {Balance}.",
            command.Payload.TransferId,
            command.Payload.Amount,
            account.Id,
            account.Balance);

        return Task.FromResult<IEntity>(account);
    }
}
