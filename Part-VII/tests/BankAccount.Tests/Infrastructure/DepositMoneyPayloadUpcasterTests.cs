// Chapter 25 — Upcaster tests
using System.Text.Json;
using BankAccount.Infrastructure.Upcasting;
using FluentAssertions;
using Xunit;

namespace BankAccount.Tests.Infrastructure;

public sealed class DepositMoneyPayloadUpcasterTests
{
    [Fact]
    public void Upcast_TransformsAmountToDepositAmount_AndAddsDefaultCurrencyCode()
    {
        var v1Json = """{"Amount": 250.00}""";
        var upcaster = new DepositMoneyPayloadV1ToV2Upcaster();

        var v2Json = upcaster.Upcast(v1Json);

        var doc = JsonDocument.Parse(v2Json);
        doc.RootElement.GetProperty("DepositAmount").GetDecimal().Should().Be(250.00m);
        doc.RootElement.GetProperty("CurrencyCode").GetString().Should().Be("GBP");
    }

    [Fact]
    public void Upcaster_HasCorrectSourceAndTargetTypeNames()
    {
        var upcaster = new DepositMoneyPayloadV1ToV2Upcaster();

        upcaster.SourceTypeName.Should()
            .Contain("DepositMoneyPayload_v1");

        upcaster.TargetTypeName.Should()
            .Contain("DepositMoneyPayload");

        upcaster.SourceTypeName.Should()
            .NotBe(upcaster.TargetTypeName);
    }
}
