// Chapter 30 — Custom ICommandDispatcher that writes structured audit log entries
#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SourceFlow.Messaging;
using SourceFlow.Messaging.Commands;

namespace BankAccount.Infrastructure.Audit;

/// <summary>
/// A custom ICommandDispatcher that writes a structured audit log entry for every
/// command that passes through the SourceFlow.Net command pipeline.
///
/// Register this alongside the default CommandDispatcher. Both will receive every
/// command. This dispatcher does not route the command to any saga — it only audits.
/// </summary>
public sealed class AuditCommandDispatcher : ICommandDispatcher
{
    private readonly ILogger<AuditCommandDispatcher> _logger;

    /// <summary>
    /// Initialises a new instance of <see cref="AuditCommandDispatcher"/>.
    /// </summary>
    /// <param name="logger">The structured logger to write audit entries to.</param>
    public AuditCommandDispatcher(ILogger<AuditCommandDispatcher> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Writes a structured audit log entry for the incoming command and returns immediately.
    /// Does not route the command to any saga or subscriber.
    /// </summary>
    /// <typeparam name="TCommand">The concrete command type.</typeparam>
    /// <param name="command">The command being dispatched through the pipeline.</param>
    /// <returns>A completed task; this dispatcher performs no async I/O.</returns>
    public Task Dispatch<TCommand>(TCommand command)
        where TCommand : ICommand
    {
        if (command is null)
            return Task.CompletedTask;

        // Cast to IMetadata to access SequenceNo and IsReplay.
        // ICommand inherits IMetadata in the SourceFlow type hierarchy.
        var metadata = (IMetadata)command;

        _logger.LogInformation(
            "AUDIT | Command={CommandType} | Entity={EntityId} | SequenceNo={SequenceNo} " +
            "| IsReplay={IsReplay} | OccurredOn={OccurredOn:O}",
            command.GetType().Name,
            command.Entity?.Id,
            metadata.Metadata.SequenceNo,
            metadata.Metadata.IsReplay,
            metadata.Metadata.OccurredOn);

        return Task.CompletedTask;
    }
}
