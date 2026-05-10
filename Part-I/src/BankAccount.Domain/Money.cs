// File: BankAccount.Domain/Money.cs
#nullable enable

namespace BankAccount.Domain;

/// <summary>
/// Represents a monetary amount with an associated currency code.
/// Money is a Value Object — two Money instances with equal Amount and Currency
/// are semantically identical and interchangeable.
/// Instances are immutable; arithmetic operations return new instances.
/// </summary>
/// <param name="Amount">The monetary amount. May be zero but not negative for deposits/withdrawals.</param>
/// <param name="Currency">The ISO 4217 currency code, e.g. "GBP", "USD". Defaults to "GBP".</param>
public record Money(decimal Amount, string Currency = "GBP")
{
    /// <summary>
    /// Adds two Money values. Both must have the same currency.
    /// </summary>
    /// <param name="a">The first operand.</param>
    /// <param name="b">The second operand.</param>
    /// <returns>A new Money representing the sum.</returns>
    /// <exception cref="InvalidOperationException">Thrown if currencies do not match.</exception>
    public static Money operator +(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException(
                $"Cannot add {a.Currency} and {b.Currency}. Currency mismatch.");

        return new Money(a.Amount + b.Amount, a.Currency);
    }

    /// <summary>
    /// Subtracts one Money value from another. Both must have the same currency.
    /// </summary>
    /// <param name="a">The first operand (minuend).</param>
    /// <param name="b">The second operand (subtrahend).</param>
    /// <returns>A new Money representing the difference.</returns>
    /// <exception cref="InvalidOperationException">Thrown if currencies do not match.</exception>
    public static Money operator -(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException(
                $"Cannot subtract {b.Currency} from {a.Currency}. Currency mismatch.");

        return new Money(a.Amount - b.Amount, a.Currency);
    }

    /// <summary>
    /// Returns true if this Money amount is less than another.
    /// Both operands must have the same currency.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if currencies do not match.</exception>
    public static bool operator <(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException(
                $"Cannot compare {a.Currency} and {b.Currency}. Currency mismatch.");

        return a.Amount < b.Amount;
    }

    /// <summary>
    /// Returns true if this Money amount is greater than another.
    /// Both operands must have the same currency.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if currencies do not match.</exception>
    public static bool operator >(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException(
                $"Cannot compare {a.Currency} and {b.Currency}. Currency mismatch.");

        return a.Amount > b.Amount;
    }

    /// <summary>
    /// Returns true if this Money amount is negative (less than zero).
    /// Used to check for insufficient funds after a debit operation.
    /// </summary>
    public bool IsNegative => Amount < 0;

    /// <summary>
    /// Returns true if this Money amount is zero.
    /// </summary>
    public bool IsZero => Amount == 0;

    /// <summary>
    /// Returns true if this Money amount is strictly positive.
    /// </summary>
    public bool IsPositive => Amount > 0;

    /// <summary>
    /// Returns a string representation, e.g. "GBP 100.00".
    /// </summary>
    public override string ToString() => $"{Currency} {Amount:F2}";

    /// <summary>
    /// Creates a zero-value Money in the given currency.
    /// </summary>
    /// <param name="currency">The currency code. Defaults to "GBP".</param>
    public static Money Zero(string currency = "GBP") => new(0, currency);
}
