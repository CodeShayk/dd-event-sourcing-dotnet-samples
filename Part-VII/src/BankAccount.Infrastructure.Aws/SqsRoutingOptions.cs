// Chapter 32 — Configuration for SQS command routing
#nullable enable

using System.Collections.Generic;

namespace BankAccount.Infrastructure.Aws;

/// <summary>
/// Configuration for which command types should be routed to SQS and which queue URL to use.
/// Populated from appsettings.json or environment variables.
/// </summary>
public sealed class SqsRoutingOptions
{
    public const string SectionName = "SqsRouting";

    /// <summary>
    /// Maps command type full names to their SQS queue URLs.
    /// Commands not listed here are processed locally only.
    /// </summary>
    public Dictionary<string, string> CommandRoutes { get; set; } = new();

    /// <summary>
    /// Returns true if the given command type has a configured SQS route.
    /// </summary>
    public bool ShouldRouteToSqs<TCommand>()
        => CommandRoutes.ContainsKey(typeof(TCommand).FullName ?? string.Empty);

    /// <summary>
    /// Returns the SQS queue URL for the given command type.
    /// Throws if the command type is not configured for SQS routing.
    /// </summary>
    public string GetQueueUrl<TCommand>()
    {
        var key = typeof(TCommand).FullName ?? string.Empty;
        if (!CommandRoutes.TryGetValue(key, out var url))
            throw new KeyNotFoundException(
                $"No SQS queue configured for command type '{key}'. " +
                $"Add it to SqsRouting:CommandRoutes in appsettings.");
        return url;
    }
}
