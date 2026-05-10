// Chapter 17 — BankAccountAggregate
using Microsoft.Extensions.Logging;
using SourceFlow.Aggregate;
using SourceFlow.Messaging.Commands;
using BankAccount.Domain.Events;
using BankAccountEntity = BankAccount.Domain.BankAccount;

namespace BankAccount.Domain;

/// <summary>
/// The BankAccount aggregate root. Acts as both a command publisher (can send
/// follow-on commands) and an event subscriber (keeps its own state current).
/// </summary>
public sealed class BankAccountAggregate :
    Aggregate<BankAccountEntity>,
    ISubscribes<AccountOpened>,
    ISubscribes<MoneyDeposited>,
    ISubscribes<MoneyWithdrawn>,
    ISubscribes<AccountClosed>
{
    private BankAccountEntity? _current;

    public BankAccountAggregate(
        Lazy<ICommandPublisher> commandPublisher,
        ILogger<IAggregate> logger)
        : base(commandPublisher, logger) { }

    public Task On(AccountOpened @event)
    {
        _current = @event.Payload;
        logger.LogInformation(
            "BankAccountAggregate received AccountOpened for account {Id}",
            _current.Id);
        return Task.CompletedTask;
    }

    public Task On(MoneyDeposited @event)
    {
        _current = @event.Payload;
        logger.LogInformation(
            "BankAccountAggregate received MoneyDeposited for account {Id}, new balance: {Balance}",
            _current.Id, _current.Balance);
        return Task.CompletedTask;
    }

    public Task On(MoneyWithdrawn @event)
    {
        _current = @event.Payload;
        return Task.CompletedTask;
    }

    public Task On(AccountClosed @event)
    {
        _current = @event.Payload;
        logger.LogInformation(
            "BankAccountAggregate received AccountClosed for account {Id}",
            _current.Id);
        return Task.CompletedTask;
    }
}
