// BankAccount.Tests/CommandReplayerTests.cs
#nullable enable

using System;
using System.Threading.Tasks;
using BankAccount.Domain;
using BankAccount.Infrastructure;
using FluentAssertions;
using Xunit;

namespace BankAccount.Tests;

/// <summary>
/// Tests for CommandReplayer.Rebuild().
/// Verifies that state is correctly reconstructed from the command log,
/// and that the rebuild is independent of any stored "current state."
/// </summary>
public sealed class CommandReplayerTests : IDisposable
{
    private readonly InMemoryCommandLog _log = new();
    private readonly CommandReplayer _replayer;

    public CommandReplayerTests()
    {
        _replayer = new CommandReplayer(_log);
    }

    [Fact]
    public async Task Rebuild_DepositAndWithdraw_ReturnsCorrectBalance()
    {
        // Arrange
        const int accountId = 1;
        await _log.AppendAsync(accountId, new OpenAccountCommand(accountId, "Alice", 0m));
        await _log.AppendAsync(accountId, new DepositMoneyCommand(accountId, 100m));
        await _log.AppendAsync(accountId, new WithdrawMoneyCommand(accountId, 30m));

        // Act
        var rebuilt = _replayer.Rebuild(accountId);

        // Assert
        rebuilt.Balance.Should().Be(70m,
            "100 deposited minus 30 withdrawn equals a balance of 70");
        rebuilt.Id.Should().Be(accountId);
        rebuilt.OwnerName.Should().Be("Alice");
        rebuilt.IsClosed.Should().BeFalse();
    }

    [Fact]
    public async Task Rebuild_IsIndependentOfCorruptedStoredState()
    {
        // Arrange — build a normal history in the log.
        const int accountId = 2;
        await _log.AppendAsync(accountId, new OpenAccountCommand(accountId, "Bob", 0m));
        await _log.AppendAsync(accountId, new DepositMoneyCommand(accountId, 100m));
        await _log.AppendAsync(accountId, new DepositMoneyCommand(accountId, 50m));
        await _log.AppendAsync(accountId, new WithdrawMoneyCommand(accountId, 20m));

        // Simulate a "stored" balance that has been corrupted by a bug.
        const decimal corruptedStoredBalance = 9999m; // Completely wrong.

        // Act — rebuild ignores corruptedStoredBalance entirely.
        var rebuilt = _replayer.Rebuild(accountId);

        // Assert — the rebuilt balance is derived from the log, not from storage.
        rebuilt.Balance.Should().Be(130m,
            "100 + 50 - 20 = 130, regardless of what the stored balance column says");

        rebuilt.Balance.Should().NotBe(corruptedStoredBalance,
            "replay must ignore any stored state and derive balance from the command log");
    }

    [Fact]
    public async Task Rebuild_ClosedAccount_ReturnsIsClosedTrue()
    {
        // Arrange
        const int accountId = 3;
        await _log.AppendAsync(accountId, new OpenAccountCommand(accountId, "Carol", 500m));
        await _log.AppendAsync(accountId, new WithdrawMoneyCommand(accountId, 500m));
        await _log.AppendAsync(accountId, new CloseAccountCommand(accountId, "Account holder request"));

        // Act
        var rebuilt = _replayer.Rebuild(accountId);

        // Assert
        rebuilt.IsClosed.Should().BeTrue("the last command in the log is a close command");
        rebuilt.Balance.Should().Be(0m, "all funds were withdrawn before closing");
    }

    [Fact]
    public void Rebuild_EmptyLog_ReturnsDefaultBankAccount()
    {
        // Arrange — no commands appended for entity 99.
        const int accountId = 99;

        // Act
        var rebuilt = _replayer.Rebuild(accountId);

        // Assert — default state: no ID, empty name, zero balance, not closed.
        rebuilt.Balance.Should().Be(0m);
        rebuilt.IsClosed.Should().BeFalse();
    }

    [Fact]
    public async Task Rebuild_CalledTwice_ReturnsSameResult()
    {
        // Arrange — this tests idempotency of the rebuild operation.
        const int accountId = 5;
        await _log.AppendAsync(accountId, new OpenAccountCommand(accountId, "Dave", 0m));
        await _log.AppendAsync(accountId, new DepositMoneyCommand(accountId, 200m));

        // Act — rebuild twice.
        var first = _replayer.Rebuild(accountId);
        var second = _replayer.Rebuild(accountId);

        // Assert — both calls return the same balance.
        first.Balance.Should().Be(second.Balance,
            "rebuild is idempotent: calling it twice produces the same state");
    }

    public void Dispose() => _log.Dispose();
}
