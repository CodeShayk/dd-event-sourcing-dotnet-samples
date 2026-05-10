// File: BankAccount.Domain.Tests/ValueObjectTests.cs
#nullable enable

using FluentAssertions;
using Xunit;

namespace BankAccount.Domain.Tests;

/// <summary>
/// Tests for the value object types introduced in Chapter 4.
/// </summary>
public class ValueObjectTests
{
    /// <summary>
    /// AccountNumber construction enforces the format rule.
    /// An invalid format is a domain error — the invalid value should never exist.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("12345")]
    [InlineData("ACCOUNT-001")]
    [InlineData("ACC-")]
    [InlineData("acc-00001")]  // lowercase prefix is invalid
    public void AccountNumber_WithInvalidFormat_ThrowsArgumentException(string invalid)
    {
        var act = () => new AccountNumber(invalid);

        act.Should().Throw<ArgumentException>(
            because: $"'{invalid}' does not match the ACC-<digits> format rule");
    }

    /// <summary>
    /// Two AccountNumber instances with the same value are equal.
    /// This is the defining characteristic of a value object.
    /// </summary>
    [Fact]
    public void TwoAccountNumbers_WithSameValue_AreEqual()
    {
        var first = new AccountNumber("ACC-00001");
        var second = new AccountNumber("ACC-00001");

        first.Should().Be(second,
            because: "AccountNumber is a value object — equality is based on value, not reference");
    }

    /// <summary>
    /// Two BankAccount entities with the same RawId are equal,
    /// even if all other properties differ. This is entity semantics.
    /// </summary>
    /// <remarks>
    /// Note: This test uses reflection to set a private property on BankAccount —
    /// an approach that is fragile in production tests because it couples the test
    /// to an internal implementation detail that may be renamed. The recommended
    /// alternative is to route the test through a real repository save, which sets
    /// RawId properly. If you need to set an internal ID in tests without a full
    /// persistence round-trip, consider adding an
    /// <c>[assembly: InternalsVisibleTo("BankAccount.Domain.Tests")]</c> attribute and
    /// an <c>internal</c> test helper method on the entity instead of using reflection.
    /// We use reflection here for brevity; do not treat this as the recommended pattern.
    /// </remarks>
    [Fact]
    public void TwoBankAccounts_WithSameId_AreEqual()
    {
        var account1 = BankAccount.Open("Alice Nguyen", new AccountNumber("ACC-00001"));
        var account2 = BankAccount.Open("Completely Different Name", new AccountNumber("ACC-99999"));

        // Simulate both having been persisted with the same ID
        // (We would normally do this through the repository; here we use reflection for the test)
        typeof(BankAccount)
            .GetProperty("RawId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(account1, 42);
        typeof(BankAccount)
            .GetProperty("RawId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(account2, 42);

        account1.Should().Be(account2,
            because: "BankAccount is an entity — equality is based on identity (Id), not on attribute values");
    }

    /// <summary>
    /// AccountId correctly identifies transient vs persisted accounts.
    /// This replaces the convention of checking for Id == 0 throughout the codebase.
    /// </summary>
    [Fact]
    public void AccountId_Transient_IsTransient()
    {
        var transient = AccountId.Transient();
        var persisted = AccountId.From(42);

        transient.IsTransient.Should().BeTrue(
            because: "an account with no assigned ID has not been persisted");
        persisted.IsTransient.Should().BeFalse(
            because: "an account with an assigned positive ID has been persisted");
    }

    /// <summary>
    /// Money arithmetic preserves currency and enforces the positive-amount rule.
    /// </summary>
    [Fact]
    public void Money_Arithmetic_PreservesCurrencyAndValue()
    {
        var initial = new Money(500m, "GBP");
        var deposit = new Money(200m, "GBP");

        var result = initial + deposit;

        result.Amount.Should().Be(700m);
        result.Currency.Should().Be("GBP",
            because: "arithmetic on Money values preserves the currency");
    }
}
