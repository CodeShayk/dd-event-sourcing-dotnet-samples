// File: BankAccount.Domain/AccountNumber.cs
#nullable enable

using System.Text.RegularExpressions;

namespace BankAccount.Domain;

/// <summary>
/// A bank account number. This is a Value Object that enforces the account number
/// format rule: all account numbers in our system start with "ACC-" followed by
/// one or more digits. An AccountNumber that violates this rule cannot be constructed.
/// </summary>
public record AccountNumber
{
    private static readonly Regex ValidPattern = new(@"^ACC-\d+$", RegexOptions.Compiled);

    /// <summary>The formatted account number string.</summary>
    public string Value { get; }

    /// <summary>
    /// Constructs an AccountNumber, validating the format.
    /// This is a non-positional record with a single constructor that
    /// validates its argument before assigning the property — the standard
    /// C# pattern for value objects that require invariant checking at creation time.
    /// </summary>
    /// <param name="value">The account number string to validate and wrap.</param>
    /// <exception cref="ArgumentException">Thrown if the format is invalid.</exception>
    public AccountNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(
                "Account number cannot be empty or whitespace.", nameof(value));

        if (!ValidPattern.IsMatch(value))
            throw new ArgumentException(
                $"Account number '{value}' does not match the required format 'ACC-<digits>'.",
                nameof(value));

        Value = value;
    }

    /// <summary>Returns the account number string.</summary>
    public override string ToString() => Value;
}
