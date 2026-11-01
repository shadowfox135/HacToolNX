using LibHac.Common.Keys;
using LibHac.Fs.Fsa;
using LibHac.Tools.FsSystem;
using System.IO;
using System.Threading;

namespace LibHacToolNX.Tools.Decryption;

/// <summary>
/// Provides contextual information for a decryption process.
/// </summary>
public class DecryptionContext
{
    /// <summary>
    /// The file system to decrypt.
    /// </summary>
    public IFileSystem? FileSystem { get; set; }

    /// <summary>
    /// The current temp directory in use.
    /// </summary>
    public required DirectoryInfo TempDirectory { get; set; }

    /// <summary>
    /// The integrity check level.
    /// </summary>
    public required IntegrityCheckLevel IntegrityCheckLevel { get; init; }

    /// <summary>
    /// The keyset.
    /// </summary>
    public required SecureKeySet KeySet { get; init; }

    /// <summary>
    /// The cancellation token which notifies if the request has been aborted.
    /// </summary>
    public CancellationToken RequestAborted { get; set; }

    /// <summary>
    /// Gets the name of a random temp file.
    /// </summary>
    /// <returns>The file.</returns>
    public FileInfo GetRandomTempFileName()
    {
        return new FileInfo(Path.Combine(TempDirectory.FullName, Path.GetRandomFileName()));
    }
}

/// <summary>
/// Describes an extracted file.
/// </summary>
public class ExtractedFileInfo
{
    /// <summary>
    /// The original path within the archive.
    /// </summary>
    public required string SourcePath { get; init; }

    /// <summary>
    /// The temporary path where the file is being held.
    /// </summary>
    public required FileInfo TemporaryPath { get; init; }
}