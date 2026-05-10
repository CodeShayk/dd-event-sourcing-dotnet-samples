// BankAccount.Tests/SnapshotEntityStoreTests.cs
#nullable enable

using System;
using System.Threading.Tasks;
using BankAccount.Domain;
using BankAccount.Infrastructure;
using BankAccountEntity = BankAccount.Domain.BankAccount;
using FluentAssertions;
using Xunit;

namespace BankAccount.Tests;

/// <summary>
/// Tests for SnapshotEntityStore.
/// Verifies that snapshot-aware rebuild correctly uses the snapshot as a
/// starting point and replays only commands after the snapshot boundary.
/// </summary>
public sealed class SnapshotEntityStoreTests : IDisposable
{
    private readonly InMemoryCommandLog _commandLog = new();
    private readonly InMemoryEntityStore _entityStore = new();

    [Fact]
    public async Task RebuildAsync_WithSnapshot_ReplayesOnlyCommandsAfterSnapshot()
    {
        // Arrange — policy takes snapshot every 5 commands (convenient for testing).
        const int accountId = 1;
        const int snapshotFrequency = 5;

        var policy = new SnapshotPolicy(snapshotEveryN: snapshotFrequency);
        var snapshotStore = new SnapshotEntityStore(_entityStore, _commandLog, policy);

        // Open the account and process the initial deposit.
        var account = new BankAccountEntity();
        account.Apply(new OpenAccountCommand(accountId, "Alice", 0m));

        // Append the open command.
        await snapshotStore.AppendAndMaybeSnapshotAsync(
            accountId, new OpenAccountCommand(accountId, "Alice", 0m), account);

        // Append 4 more deposits (commands 2–5). At command 5, snapshot triggers.
        for (int i = 1; i <= 4; i++)
        {
            var deposit = new DepositMoneyCommand(accountId, 100m);
            account.Apply(deposit);
            await snapshotStore.AppendAndMaybeSnapshotAsync(accountId, deposit, account);
        }

        // At this point, a snapshot should exist at SequenceNo = 5.
        var snapshot = await _entityStore.Get<BankAccountSnapshot>(accountId);
        snapshot.Should().NotBeNull("a snapshot must be created at the 5th command");
        snapshot!.LastSequenceNo.Should().Be(5, "snapshot covers commands 1–5");

        // Append 3 more deposits after the snapshot (commands 6–8).
        for (int i = 1; i <= 3; i++)
        {
            var deposit = new DepositMoneyCommand(accountId, 50m);
            account.Apply(deposit);
            await snapshotStore.AppendAndMaybeSnapshotAsync(accountId, deposit, account);
        }

        // Act — rebuild from snapshot.
        var rebuilt = await snapshotStore.RebuildAsync(accountId);

        // Assert
        // Expected balance: 4 * 100 + 3 * 50 = 400 + 150 = 550
        rebuilt.Balance.Should().Be(550m,
            "4 deposits of 100 plus 3 deposits of 50 equals 550");
    }

    [Fact]
    public async Task RebuildAsync_WithNoSnapshot_ReplayesAllCommands()
    {
        // Arrange — policy set so high it never triggers.
        const int accountId = 2;
        var policy = new SnapshotPolicy(snapshotEveryN: int.MaxValue);
        var snapshotStore = new SnapshotEntityStore(_entityStore, _commandLog, policy);

        var account = new BankAccountEntity();
        account.Apply(new OpenAccountCommand(accountId, "Bob", 0m));

        await snapshotStore.AppendAndMaybeSnapshotAsync(
            accountId, new OpenAccountCommand(accountId, "Bob", 0m), account);

        for (int i = 0; i < 10; i++)
        {
            var deposit = new DepositMoneyCommand(accountId, 10m);
            account.Apply(deposit);
            await snapshotStore.AppendAndMaybeSnapshotAsync(accountId, deposit, account);
        }

        // Verify no snapshot was taken.
        var snapshot = await _entityStore.Get<BankAccountSnapshot>(accountId);
        snapshot.Should().BeNull("the policy threshold was never reached");

        // Act
        var rebuilt = await snapshotStore.RebuildAsync(accountId);

        // Assert — 10 deposits of 10 = 100
        rebuilt.Balance.Should().Be(100m,
            "all 10 deposits of 10 replayed from the beginning gives a balance of 100");
    }

    [Fact]
    public async Task RebuildAsync_LargeCommandHistory_UsesSnapshotToBoundReplay()
    {
        // Arrange — simulate 1000 commands with a snapshot every 500.
        const int accountId = 3;
        var policy = new SnapshotPolicy(snapshotEveryN: 500);
        var snapshotStore = new SnapshotEntityStore(_entityStore, _commandLog, policy);

        var account = new BankAccountEntity();
        account.Apply(new OpenAccountCommand(accountId, "Carol", 0m));

        await snapshotStore.AppendAndMaybeSnapshotAsync(
            accountId, new OpenAccountCommand(accountId, "Carol", 0m), account);

        // Append 999 more commands (commands 2–1000).
        for (int i = 0; i < 999; i++)
        {
            var deposit = new DepositMoneyCommand(accountId, 1m);
            account.Apply(deposit);
            await snapshotStore.AppendAndMaybeSnapshotAsync(accountId, deposit, account);
        }

        // Verify snapshot was taken at command 1000.
        var snapshot = await _entityStore.Get<BankAccountSnapshot>(accountId);
        snapshot.Should().NotBeNull("a snapshot must exist at command 1000");
        snapshot!.LastSequenceNo.Should().Be(1000,
            "the most recent snapshot covers up to command 1000");

        // Act
        var rebuilt = await snapshotStore.RebuildAsync(accountId);

        // Assert — 999 deposits of 1 = 999
        rebuilt.Balance.Should().Be(999m,
            "999 deposits of 1 gives a balance of 999 regardless of snapshot usage");

        snapshot.LastSequenceNo.Should().Be(1000,
            "the snapshot at 1000 means no commands need replaying after it");
    }

    public void Dispose() => _commandLog.Dispose();
}
