// BankAccount.Domain/BankAccountState.cs
#nullable enable

namespace BankAccount.Domain;

/// <summary>
/// A data transfer object representing the serialisable state of a BankAccount.
/// Used as the snapshot payload — the BankAccount aggregate itself is not
/// directly serialisable because System.Text.Json cannot deserialise into
/// types with private setters without a custom JsonConverter.
/// </summary>
public sealed record BankAccountState(
    int Id,
    string OwnerName,
    decimal Balance,
    bool IsClosed);
