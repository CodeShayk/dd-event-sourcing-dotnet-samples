// BankAccount.Application/Sagas/TransferMoneySaga.cs
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
/// Orchestrates the multi-step money transfer workflow.
/// Demonstrates command chaining via the protected Publish() helper
/// and the compensation pattern for distributed operation rollback.
/// </summary>
public sealed class TransferMoneySaga
    : Saga<BankAccountEntity>,
      IHandles<TransferMoneyCommand>,
      IHandles<CreditAccountCommand>
{
    public TransferMoneySaga(
        Lazy<ICommandPublisher> commandPublisher,
        IEventQueue eventQueue,
        IEntityStoreAdapter entityStore,
        ILogger<ISaga> logger)
        : base(commandPublisher, eventQueue, entityStore, logger)
    {
    }

    /// <summary>
    /// Handles the initial transfer request.
    /// Validates the source account and publishes a DebitAccountCommand.
    /// </summary>
    public async Task<IEntity> Handle(IEntity entity, TransferMoneyCommand command)
    {
        var sourceAccount = (BankAccountEntity)entity;
        var payload = command.Payload;

        if (!sourceAccount.IsActive)
            throw new InvalidOperationException(
                $"Cannot transfer from closed account {sourceAccount.Id}.");

        if (payload.Amount <= 0)
            throw new ArgumentException("Transfer amount must be positive.", nameof(command));

        if (sourceAccount.Balance < payload.Amount)
            throw new InvalidOperationException(
                $"Insufficient funds for transfer. Balance: {sourceAccount.Balance}, " +
                $"Requested: {payload.Amount}.");

        var transferId = Guid.NewGuid();

        // Command chaining — NOT event raising.
        // Publish() routes follow-on COMMANDS through the command bus.
        // Raise<TEvent>() and IHandlesWithEvent route EVENTS to IEventQueue.
        await Publish(
            new DebitAccountCommand(
                payload.SourceAccountId,
                new DebitAccountPayload(payload.Amount, transferId)
            )
        );

        return sourceAccount;
    }

    /// <summary>
    /// Handles the credit step of a transfer.
    /// On failure, publishes ReverseDebitCommand to the SOURCE account to compensate.
    /// </summary>
    public async Task<IEntity> Handle(IEntity entity, CreditAccountCommand command)
    {
        var targetAccount = (BankAccountEntity)entity;
        var payload = command.Payload;

        try
        {
            if (!targetAccount.IsActive)
            {
                logger.LogWarning(
                    "Transfer {TransferId}: credit failed — target account {TargetId} is closed. " +
                    "Publishing ReverseDebitCommand to source account {SourceId}.",
                    payload.TransferId, targetAccount.Id, payload.SourceAccountId);

                await Publish(
                    new ReverseDebitCommand(
                        payload.SourceAccountId,
                        new ReverseDebitPayload(payload.Amount, payload.TransferId)
                    )
                );

                return targetAccount;
            }

            targetAccount.Balance += payload.Amount;
            return targetAccount;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Transfer {TransferId}: unexpected error crediting account {TargetId}.",
                payload.TransferId, targetAccount.Id);

            await Publish(
                new ReverseDebitCommand(
                    payload.SourceAccountId,
                    new ReverseDebitPayload(payload.Amount, payload.TransferId)
                )
            );

            throw;
        }
    }
}
