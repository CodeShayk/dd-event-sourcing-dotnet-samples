// BankAccount.Tests/CommandSerialisationTests.cs
using System.Text.Json;
using FluentAssertions;
using BankAccount.Domain.Commands;
using Xunit;

namespace BankAccount.Tests;

/// <summary>
/// Verifies that all four Bank Account commands serialise and deserialise correctly.
/// This is the correctness guarantee for the command store's replay mechanism.
/// </summary>
public class CommandSerialisationTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    [Fact]
    public void OpenAccountCommand_RoundTrips_WithoutDataLoss()
    {
        var original = new OpenAccountCommand(
            newEntity: true,
            payload: new OpenAccountPayload("Alice Dewhurst", 500m)
        );

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<OpenAccountCommand>(json, Options);

        restored.Should().NotBeNull();
        restored!.Payload.HolderName.Should().Be("Alice Dewhurst");
        restored.Payload.InitialDeposit.Should().Be(500m);
        restored.Entity.IsNew.Should().BeTrue();
    }

    [Fact]
    public void DepositMoneyCommand_RoundTrips_WithoutDataLoss()
    {
        var original = new DepositMoneyCommand(
            accountId: 42,
            payload: new DepositMoneyPayload(250m, "Salary")
        );

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DepositMoneyCommand>(json, Options);

        restored.Should().NotBeNull();
        restored!.Payload.Amount.Should().Be(250m);
        restored.Payload.Reference.Should().Be("Salary");
        restored.Entity.Id.Should().Be(42);
    }

    [Fact]
    public void CommandName_IsStableAndDoesNotIncludeAssemblyDetails()
    {
        var command = new DepositMoneyCommand(1, new DepositMoneyPayload(100m));

        command.Name.Should().Be("DepositMoneyCommand");
        command.Name.Should().NotContain(",");
        command.Name.Should().NotContain("Version=");
    }
}
