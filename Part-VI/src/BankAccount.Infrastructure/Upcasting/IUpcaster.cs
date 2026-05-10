// Chapter 25 — IUpcaster interface (conceptual; SourceFlow.Stores.EntityFramework provides this)
namespace BankAccount.Infrastructure.Upcasting;

/// <summary>
/// Interface for command payload upcasters. Transforms serialised payload
/// JSON from an older schema version to the current version.
/// In production, this interface is provided by SourceFlow.Stores.EntityFramework.
/// </summary>
public interface IUpcaster
{
    /// <summary>The AssemblyQualifiedName of the old payload type.</summary>
    string SourceTypeName { get; }

    /// <summary>The AssemblyQualifiedName of the current payload type.</summary>
    string TargetTypeName { get; }

    /// <summary>Transforms old JSON to new JSON format.</summary>
    string Upcast(string json);
}
