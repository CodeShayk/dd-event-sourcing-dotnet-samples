// File: BankAccount.Domain.Tests/BankAccountAggregateTests.cs
#nullable enable

using FluentAssertions;
using Xunit;

namespace BankAccount.Domain.Tests;

/// <summary>
/// Tests for the BankAccount aggregate root.
/// Tests are organised around business invariants: the rules the aggregate must enforce.
/// </summary>
public class BankAccountAggregateTests
{
    private static BankAccount OpenTestAccount(
        string holder = "Test Holder",
        string number = "ACC-00001")
        => BankAccount.Open(holder, new AccountNumber(number));

    /// <summary>
    /// Invariant: Crediting an account increases balance and records a transaction.
    /// Both the balance and the transaction must be consistent after the operation.
    /// </summary>
    [Fact]
    public void Credit_IncreasesBalanceAndRecordsTransaction()
    {
        var account = OpenTestAccount();

        account.Credit(new Money(500m), "Opening deposit");

        account.Balance.Amount.Should().Be(500m,
            because: "crediting GBP 500 increases the balance from zero to 500");
        account.Transactions.Should().HaveCount(1,
            because: "one transaction must be recorded for each credit operation");
        account.Transactions[0].Type.Should().Be(TransactionType.Credit);
        account.Transactions[0].Amount.Amount.Should().Be(500m);
    }

    /// <summary>
    /// Invariant: Debiting an account decreases balance and records a transaction.
    /// The transaction line and balance change are atomic within the aggregate.
    /// </summary>
    [Fact]
    public void Debit_DecreasesBalanceAndRecordsTransaction()
    {
        var account = OpenTestAccount();
        account.Credit(new Money(500m));

        account.Debit(new Money(200m), "ATM withdrawal");

        account.Balance.Amount.Should().Be(300m,
            because: "debiting 200 from a 500 balance leaves 300");
        account.Transactions.Should().HaveCount(2,
            because: "one transaction for the credit, one for the debit");
        account.Transactions[1].Type.Should().Be(TransactionType.Debit);
    }

    /// <summary>
    /// Invariant: A closed account cannot be credited.
    /// The aggregate enforces this — no service layer check required.
    /// </summary>
    [Fact]
    public void Credit_OnClosedAccount_ThrowsAccountNotActiveException()
    {
        var account = OpenTestAccount();
        account.Close();

        var act = () => account.Credit(new Money(100m));

        act.Should().Throw<AccountNotActiveException>(
            because: "closed accounts must not accept any further transactions");
    }

    /// <summary>
    /// Invariant: A debit cannot take the balance below zero.
    /// The aggregate enforces this invariant — it is not a service-layer concern.
    /// </summary>
    [Fact]
    public void Debit_ExceedingBalance_ThrowsInsufficientFundsException()
    {
        var account = OpenTestAccount();
        account.Credit(new Money(100m));

        var act = () => account.Debit(new Money(150m));

        act.Should().Throw<InsufficientFundsException>()
            .Which.CurrentBalance.Amount.Should().Be(100m,
                because: "the exception must carry the actual balance at the time of the failed debit");
    }

    /// <summary>
    /// Invariant: Closing an already-closed account is idempotent.
    /// The domain accepts this gracefully rather than throwing.
    /// </summary>
    [Fact]
    public void Close_OnAlreadyClosedAccount_IsIdempotent()
    {
        var account = OpenTestAccount();
        account.Close();

        var act = () => account.Close();

        act.Should().NotThrow(
            because: "closing an already-closed account is a no-op, not an error");
        account.IsActive.Should().BeFalse();
    }

    /// <summary>
    /// Invariant: A credit with a zero amount is rejected.
    /// The positive-amount guard exists in both Credit and Debit.
    /// </summary>
    [Fact]
    public void Credit_WithZeroAmount_ThrowsDomainException()
    {
        var account = OpenTestAccount();

        var act = () => account.Credit(Money.Zero());

        act.Should().Throw<DomainException>(
            because: "a zero-value credit is not a valid operation — the amount must be positive");
    }

    /// <summary>
    /// Invariant: A debit with a zero amount is rejected.
    /// </summary>
    [Fact]
    public void Debit_WithZeroAmount_ThrowsDomainException()
    {
        var account = OpenTestAccount();
        account.Credit(new Money(100m));

        var act = () => account.Debit(Money.Zero());

        act.Should().Throw<DomainException>(
            because: "a zero-value debit is not a valid operation — the amount must be positive");
    }
}
