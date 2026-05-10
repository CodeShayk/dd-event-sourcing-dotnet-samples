// BankAccount.Tests/EfCommandLogTests.cs
#nullable enable

using System;
using System.Text.Json;
using System.Threading.Tasks;
using BankAccount.Domain;
using BankAccount.Infrastructure;
using BankAccount.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BankAccount.Tests;

/// <summary>
/// Integration tests for EfCommandLog using an in-process SQLite database.
/// Tests verify round-trip serialisation, sequence ordering, and
/// AsNoTracking() behaviour.
/// </summary>
public sealed class EfCommandLogTests : IAsyncLifetime
{
    private CommandLogDbContext _dbContext = null!;
    private EfCommandLog _log = null!;

    public async Task InitializeAsync()
    {
        // Use a unique database name per test run to ensure test isolation.
        var options = new DbContextOptionsBuilder<CommandLogDbContext>()
            .UseSqlite($"DataSource=file:commandlog_{Guid.NewGuid():N}?mode=memory&cache=shared")
            .Options;

        _dbContext = new CommandLogDbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();
        _log = new EfCommandLog(_dbContext);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.Database.EnsureDeletedAsync();
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task Append_DepositCommand_CanBeLoadedAndDeserialised()
    {
        // Arrange
        const int accountId = 1;
        var original = new DepositMoneyCommand(accountId, 250m);

        // Act
        await _log.AppendAsync(accountId, original);
        var records = await _log.LoadAsync(accountId);

        // Assert
        records.Should().HaveCount(1);

        var record = records[0];
        record.EntityId.Should().Be(accountId);
        record.SequenceNo.Should().Be(1);
        record.CommandType.Should().Contain(nameof(DepositMoneyCommand));

        // Round-trip deserialisation.
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var reconstructed = JsonSerializer.Deserialize<DepositMoneyCommand>(record.PayloadData, options);

        reconstructed.Should().NotBeNull("the payload must deserialise correctly");
        reconstructed!.AccountId.Should().Be(original.AccountId);
        reconstructed.Amount.Should().Be(original.Amount);
    }

    [Fact]
    public async Task Load_ReturnsRecordsInAscendingSequenceOrder()
    {
        // Arrange — append commands out of expected sequence to verify ordering.
        const int accountId = 10;

        await _log.AppendAsync(accountId, new OpenAccountCommand(accountId, "Alice", 0m));
        await _log.AppendAsync(accountId, new DepositMoneyCommand(accountId, 100m));
        await _log.AppendAsync(accountId, new DepositMoneyCommand(accountId, 200m));
        await _log.AppendAsync(accountId, new WithdrawMoneyCommand(accountId, 50m));

        // Act
        var records = await _log.LoadAsync(accountId);

        // Assert
        records.Should().HaveCount(4);
        records.Select(r => r.SequenceNo).Should()
            .BeInAscendingOrder("replay requires ascending sequence order");

        records[0].SequenceNo.Should().Be(1);
        records[3].SequenceNo.Should().Be(4);
    }

    [Fact]
    public async Task Load_UsesAsNoTracking_DoesNotPopulateChangeTracker()
    {
        // Arrange
        const int accountId = 5;
        await _log.AppendAsync(accountId, new DepositMoneyCommand(accountId, 100m));

        // Clear the tracker entries created by AppendAsync before testing Load.
        _dbContext.ChangeTracker.Clear();

        // Act
        await _log.LoadAsync(accountId);

        // Assert — the change tracker should have no entries from the Load() call.
        _dbContext.ChangeTracker.Entries().Should().BeEmpty(
            "Load() uses AsNoTracking() and must not populate the change tracker");
    }

    [Fact]
    public async Task AppendedRecords_SurviveDbContextDisposalAndRecreation()
    {
        // This test proves durability: records written through one DbContext instance
        // are readable through a new instance backed by the same database.
        const int accountId = 99;

        // Arrange — use a named file database for this test.
        string dbName = $"durability_test_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<CommandLogDbContext>()
            .UseSqlite($"DataSource={dbName}.db")
            .Options;

        try
        {
            // First context: write a record.
            await using (var context1 = new CommandLogDbContext(options))
            {
                await context1.Database.EnsureCreatedAsync();
                var log1 = new EfCommandLog(context1);
                await log1.AppendAsync(accountId, new DepositMoneyCommand(accountId, 500m));
                await context1.Database.CloseConnectionAsync();
            }

            // Second context: verify the record persisted.
            await using (var context2 = new CommandLogDbContext(options))
            {
                var log2 = new EfCommandLog(context2);
                var records = await log2.LoadAsync(accountId);

                // Assert
                records.Should().HaveCount(1,
                    "records must survive DbContext disposal and process-equivalent re-creation");
                records[0].EntityId.Should().Be(accountId);
                await context2.Database.CloseConnectionAsync();
            }
        }
        finally
        {
            // Clean up the file database.
            try { System.IO.File.Delete($"{dbName}.db"); } catch { /* best effort */ }
        }
    }
}
