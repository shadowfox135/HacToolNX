using System.Diagnostics;

namespace HacToolNX;

/// <summary>
/// Describes an application argument.
/// </summary>
[DebuggerDisplay("\"{Key}\":\"{Value}\"")]
internal class ApplicationArgument
{
    /// <summary>
    /// The prefix used to identify an option.
    /// </summary>
    public static string OptionPrefix = "--";

    /// <summary>
    /// The prefix used to identify an alias.
    /// </summary>
    public static char AliasPrefix = '-';

    /// <summary>
    /// The key.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// Optional. The value (if provided).
    /// </summary>
    public string? Value { get; set; }
}