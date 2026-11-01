namespace HacToolNX.Commands;

/// <summary>
/// Describes options for a decryption command.
/// </summary>
internal abstract class AbstractDecryptCommandOptions : CommandOptions
{
    /// <summary>
    /// The source file.
    /// </summary>
    [CommandOption("source", Description = "The source file.", Alias = "s")]
    public string? Source { get; set; }

    /// <summary>
    /// The directory which contains the keyset files.
    /// </summary>
    [CommandOption("keyset", Description = "The directory which contains the keyset files.", Alias = "k")]
    public string? KeySet { get; set; }
}