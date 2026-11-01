namespace HacToolNX.Commands.Decrypt;

/// <summary>
/// Describes the options for the repack command.
/// </summary>
internal class DecryptCommandOptions : AbstractDecryptCommandOptions
{
    /// <summary>
    /// The directory where the files will be placed.
    /// </summary>
    [CommandOption("outdir", Description = "The directory where the file will be placed.", Alias = "o")]
    public string? Destination { get; set; }

    /// <summary>
    /// Identifies whether firmware mode is in use.
    /// </summary>
    [CommandOption("firmware", Description = "Enables decrypting firmware.")]
    public bool Firmware { get; set; }

    /// <summary>
    /// Identifies whether the existing file will be overwritten (if present).
    /// </summary>
    [CommandOption("overwrite", Description = "Overwrites the destination file (if present).")]
    public bool Overwrite { get; set; }

    /// <summary>
    /// Identifies whether integrity checks will be skipped while extracting the contents. Default: false
    /// </summary>
    [CommandOption("skip-integrity-checks", Description = "Skip integrity checks while extracting the contents.", DefaultValue = "false")]
    public bool SkipIntegrityChecks { get; set; }
}