namespace HacToolNX.Commands.Unpack;

/// <summary>
/// Describes the options for the unpack command.
/// </summary>
internal class UnpackCommandOptions : AbstractDecryptCommandOptions
{
    /// <summary>
    /// The directory where the files will be placed.
    /// </summary>
    [CommandOption("outdir", Description = "The directory where the files will be placed.", Alias = "o")]
    public string? Destination { get; set; }

    /// <summary>
    /// Identifies whether the extraction should be recursive.
    /// </summary>
    [CommandOption("recursive", Description = "Identifies whether the extraction should be recursive.",
        DefaultValue = "false")]
    public bool Recursive { get; set; }

    /// <summary>
    /// Identifies whether extraction should continue where it left off. Default: false
    /// </summary>
    [CommandOption("continue", Description = "Identifies whether extraction should continue where it left off.",
        DefaultValue = "false")]
    public bool Continue { get; set; }

    /// <summary>
    /// Identifies whether integrity checks will be skipped while extracting the contents. Default: false
    /// </summary>
    [CommandOption("skip-integrity-checks", Description = "Skip integrity checks while extracting the contents.", DefaultValue = "false")]
    public bool SkipIntegrityChecks { get; set; }
}