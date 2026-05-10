// BankAccount.Infrastructure/Diagnostics/DiagnosticCommandDispatcher.cs
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SourceFlow.Messaging;
using SourceFlow.Messaging.Commands;

namespace BankAccount.Infrastructure.Diagnostics;

/// <summary>
/// A custom ICommandDispatcher that logs a structured trace entry before forwarding
/// each command to the next step in the pipeline.
///
/// Register this alongside the framework's built-in CommandDispatcher to add
/// diagnostic output without modifying any framework code.
/// </summary>
public sealed class DiagnosticCommandDispatcher : ICommandDispatcher
{
    private readonly ILogger<DiagnosticCommandDispatcher> _logger;

    /// <summary>
    /// Initialises the dispatcher with the provided logger.
    /// </summary>
    public DiagnosticCommandDispatcher(ILogger<DiagnosticCommandDispatcher> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task Dispatch<TCommand>(TCommand command) where TCommand : ICommand
    {
        var metadata = (IMetadata)command;

        _logger.LogInformation(
            "DIAGNOSTIC: Command={CommandType}, EntityId={EntityId}, SequenceNo={SequenceNo}, IsReplay={IsReplay}",
            command.GetType().Name,
            command.Entity.Id,
            metadata.Metadata.SequenceNo,
            metadata.Metadata.IsReplay);

        return Task.CompletedTask;
    }
}
