using LibHac.Common.Keys;
using LibHac.Tools.FsSystem;
using System.IO;

namespace LibHacToolNX.Tools.Extraction;

/// <summary>
/// Describes the options when extracting archives.
/// </summary>
public class ExtractionOptions
{
    /// <summary>
    /// The output destination where the files should be placed.
    /// </summary>
    public required DirectoryInfo Destination { get; init; }

    /// <summary>
    /// The keyset.
    /// </summary>
    public required SecureKeySet KeySet { get; init; }

    /// <summary>
    /// Identifies whether extraction will be recursive.
    /// </summary>
    public required bool Recursive { get; init; }

    /// <summary>
    /// Identifies whether extraction will continue where it terminated.
    /// </summary>
    public required bool Continue { get; init; }

    /// <summary>
    /// Identifies the integrity check level used while extracting the contents.
    /// </summary>
    public required IntegrityCheckLevel IntegrityCheckLevel { get; init; }
}