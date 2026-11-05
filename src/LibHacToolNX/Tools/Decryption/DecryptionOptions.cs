using LibHac.Common.Keys;
using LibHac.Tools.FsSystem;
using System.IO;

namespace LibHacToolNX.Tools.Decryption;

/// <summary>
/// Describes the options when decrypting archives.
/// </summary>
public class DecryptionOptions
{
    /// <summary>
    /// The keyset.
    /// </summary>
    public required SecureKeySet KeySet { get; init; }

    /// <summary>
    /// The directory to use for holding any temporary files generated during the decryption process.
    /// </summary>
    public required DirectoryInfo TempDirectory { get; init; }

    /// <summary>
    /// Identifies the integrity check level used while extracting the contents.
    /// </summary>
    public required IntegrityCheckLevel IntegrityCheckLevel { get; init; }

    /// <summary>
    /// Identifies whether the alternate format for DNCA file layouts should be written. Default: false
    /// </summary>
    public required bool UseAlternateFormat { get; set; }
}