// Chapter 20 — API integration tests
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using BankAccount.Domain.Views;
using BankAccount.Tests.Infrastructure;
using Xunit;

namespace BankAccount.Tests.Api;

public sealed class AccountCommandControllerTests
{
    private static HttpClient CreateClient()
    {
        var factory = new WebApplicationFactory<Program>();
        return factory
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");
                builder.ConfigureServices(services =>
                    services.AddInMemorySourceFlowStores());
            })
            .CreateClient();
    }

    [Fact]
    public async Task Deposit_ShouldReturn202Accepted_WithQueryUrl()
    {
        var client = CreateClient();

        await client.PostAsJsonAsync("/api/accounts", new
        {
            AccountNumber  = "ACC-100",
            AccountHolder  = "Test User",
            InitialBalance = 1000.00m
        });

        var response = await client.PostAsJsonAsync("/api/accounts/0/deposit", new
        {
            Amount      = 250.00m,
            Description = "Test deposit"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var body = await response.Content.ReadFromJsonAsync<AcceptedBody>();
        body!.QueryUrl.Should().Contain("/api/accounts/0/summary");
        body.Message.Should().Contain("250");
    }

    [Fact]
    public async Task Deposit_AndQuery_ShouldReflectUpdatedBalance()
    {
        var client = CreateClient();

        await client.PostAsJsonAsync("/api/accounts", new
        {
            AccountNumber  = "ACC-101",
            AccountHolder  = "Balance Check",
            InitialBalance = 500.00m
        });

        await client.PostAsJsonAsync("/api/accounts/0/deposit", new
        {
            Amount      = 300.00m,
            Description = "Paycheck"
        });

        var queryResponse = await client.GetAsync("/api/accounts/0/summary");
        queryResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var view = await queryResponse.Content.ReadFromJsonAsync<AccountSummaryView>();
        view!.Balance.Should().Be(800.00m);
    }

    [Fact]
    public async Task Withdraw_BeyondBalance_ShouldReturn400_WithStructuredError()
    {
        var client = CreateClient();

        await client.PostAsJsonAsync("/api/accounts", new
        {
            AccountNumber  = "ACC-102",
            AccountHolder  = "Overdraft Test",
            InitialBalance = 100.00m
        });

        var response = await client.PostAsJsonAsync("/api/accounts/0/withdraw", new
        {
            Amount      = 500.00m,
            Description = "Attempted overdraft"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var error = await response.Content.ReadFromJsonAsync<ErrorBody>();
        error!.Code.Should().Be("INSUFFICIENT_FUNDS");
        error.Error.Should().Be("Domain rule violation");
    }

    [Fact]
    public async Task Deposit_WithNegativeAmount_ShouldReturn400_FromFluentValidation()
    {
        var client = CreateClient();

        await client.PostAsJsonAsync("/api/accounts", new
        {
            AccountNumber  = "ACC-103",
            AccountHolder  = "Validation Test",
            InitialBalance = 500.00m
        });

        var response = await client.PostAsJsonAsync("/api/accounts/0/deposit", new
        {
            Amount      = -100.00m,
            Description = "Invalid deposit"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed record AcceptedBody(string Message, string? QueryUrl);
    private sealed record ErrorBody(string Error, string Message, string Code);
}
