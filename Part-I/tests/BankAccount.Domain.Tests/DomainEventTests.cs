// File: BankAccount.Domain.Tests/DomainEventTests.cs
#nullable enable

using BankAccount.Domain.Events;
using FluentAssertions;
using Xunit;

namespace BankAccount.Domain.Tests;

/// <summary>
/// Tests that verify domain event raising in the BankAccount aggregate.
/// Each test verifies that the correct event is raised with the correct data.
/// </summary>
public class DomainEventTests
{
    /// <summary>
    /// Opening an account must raise exactly one AccountOpened event.
    /// The event carries the account holder and account number.
    /// </summary>
    [Fact]
    public void OpenAccount_RaisesAccountOpenedEvent()
    {
        var account = BankAccount.Open("Alice Nguyen", new AccountNumber("ACC-00001"));

        account.DomainEvents.Should().HaveCount(1,
            because: "opening an account is one domain event");

        var opened = account.DomainEvents[0].Should().BeOfType<AccountOpened>().Subject;
        opened.AccountHolder.Should().Be("Alice Nguyen");
        opened.AccountNumber.Should().Be("ACC-00001");
        opened.OccurredOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// A credit operation must raise an AccountCredited event with the amount
    /// and the resulting balance after the credit, both expressed as Money.
    /// </summary>
    [Fact]
    public void Credit_RaisesAccountCreditedEvent_WithNewBalance()
    {
        var account = BankAccount.Open("Bob Chen", new AccountNumber("ACC-00002"));
        account.ClearDomainEvents(); // Clear the AccountOpened event

        account.Credit(new Money(500m), "Opening deposit");

        account.DomainEvents.Should().HaveCount(1);
        var credited = account.DomainEvents[0].Should().BeOfType<AccountCredited>().Subject;
        credited.Amount.Amount.Should().Be(500m,
            because: "the event carries the credited amount");
        credited.Amount.Currency.Should().Be("GBP",
            because: "the event preserves the currency from the Money value object");
        credited.NewBalance.Amount.Should().Be(500m,
            because: "the event carries the balance after the credit — crucial for read model updates");
        credited.Reference.Should().Be("Opening deposit");
    }

    /// <summary>
    /// A debit operation must raise an AccountDebited event with the amount
    /// and the resulting balance after the debit, both expressed as Money.
    /// </summary>
    [Fact]
    public void Debit_RaisesAccountDebitedEvent_WithNewBalance()
    {
        var account = BankAccount.Open("Diana Ross", new AccountNumber("ACC-00004"));
        account.Credit(new Money(300m));
        account.ClearDomainEvents(); // Clear AccountOpened and AccountCredited events

        account.Debit(new Money(100m), "ATM withdrawal");

        account.DomainEvents.Should().HaveCount(1);
        var debited = account.DomainEvents[0].Should().BeOfType<AccountDebited>().Subject;
        debited.Amount.Amount.Should().Be(100m,
            because: "the event carries the debited amount");
        debited.Amount.Currency.Should().Be("GBP",
            because: "the event preserves the currency from the Money value object");
        debited.NewBalance.Amount.Should().Be(200m,
            because: "debiting 100 from a 300 balance leaves 200");
        debited.Reference.Should().Be("ATM withdrawal");
    }

    /// <summary>
    /// Closing an account raises AccountClosed.
    /// Closing again (idempotent) raises no additional event.
    /// </summary>
    [Fact]
    public void Close_RaisesAccountClosedEvent_AndSubsequentCloseRaisesNoEvent()
    {
        var account = BankAccount.Open("Carol Park", new AccountNumber("ACC-00003"));
        account.ClearDomainEvents();

        account.Close();

        account.DomainEvents.Should().HaveCount(1);
        account.DomainEvents[0].Should().BeOfType<AccountClosed>();

        // Second close is idempotent — no new event
        account.ClearDomainEvents();
        account.Close();

        account.DomainEvents.Should().BeEmpty(
            because: "closing an already-closed account is a no-op and raises no event");
    }

    /// <summary>
    /// Verifies that the dispatcher routes events to the correct handlers
    /// and clears the aggregate's pending events after dispatch.
    /// The dispatcher accepts IHasDomainEvents rather than BankAccount directly,
    /// making it reusable across any aggregate type.
    /// </summary>
    [Fact]
    public async Task Dispatcher_RoutesEventsToHandlersAndClearsAggregate()
    {
        var account = BankAccount.Open("Eve Adams", new AccountNumber("ACC-00005"));
        account.Credit(new Money(200m));

        var capturedEvents = new List<IDomainEvent>();
        var dispatcher = new InMemoryDomainEventDispatcher();
        dispatcher.Register<AccountOpened>(new CapturingHandler<AccountOpened>(capturedEvents));
        dispatcher.Register<AccountCredited>(new CapturingHandler<AccountCredited>(capturedEvents));

        // DispatchAndClear accepts IHasDomainEvents — BankAccount implements it
        await dispatcher.DispatchAndClear(account);

        capturedEvents.Should().HaveCount(2,
            because: "one AccountOpened event and one AccountCredited event were raised");
        capturedEvents[0].Should().BeOfType<AccountOpened>();
        capturedEvents[1].Should().BeOfType<AccountCredited>();

        account.DomainEvents.Should().BeEmpty(
            because: "DispatchAndClear must remove all pending events from the aggregate");
    }
}

/// <summary>
/// A test handler that captures events into a shared list for assertion.
/// </summary>
internal sealed class CapturingHandler<TEvent> : IDomainEventHandler<TEvent>
    where TEvent : IDomainEvent
{
    private readonly List<IDomainEvent> _captured;

    public CapturingHandler(List<IDomainEvent> captured) => _captured = captured;

    public Task Handle(TEvent @event)
    {
        _captured.Add(@event);
        return Task.CompletedTask;
    }
}
