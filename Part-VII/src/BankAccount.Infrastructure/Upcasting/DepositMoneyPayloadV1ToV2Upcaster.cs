// Chapter 25 — Upcaster: transforms v1 DepositMoneyPayload to v2 format
using System.Text.Json.Nodes;

namespace BankAccount.Infrastructure.Upcasting;

/// <summary>
/// Upcasts a v1 DepositMoneyPayload (with Amount field)
/// to a v2 DepositMoneyPayload (with DepositAmount and CurrencyCode fields).
/// </summary>
public sealed class DepositMoneyPayloadV1ToV2Upcaster : IUpcaster
{
    public string SourceTypeName =>
        "BankAccount.Domain.Commands.DepositMoneyPayload_v1, BankAccount.Domain";

    public string TargetTypeName =>
        "BankAccount.Domain.Commands.DepositMoneyPayload, BankAccount.Domain";

    public string Upcast(string json)
    {
        var node = JsonNode.Parse(json)?.AsObject()
            ?? throw new InvalidOperationException(
                $"Cannot parse JSON for {nameof(DepositMoneyPayloadV1ToV2Upcaster)}: {json}");

        var amount = node["Amount"]?.GetValue<decimal>() ?? 0m;

        var newNode = new JsonObject
        {
            ["DepositAmount"] = amount,
            ["CurrencyCode"] = "GBP"
        };

        return newNode.ToJsonString();
    }
}
