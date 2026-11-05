using LibHac.Fs.Fsa;
using System.IO;
using System.Threading;

namespace LibHacToolNX.Tools.Decryption;

/// <summary>
/// Provides contextual information for a decryption process.
/// </summary>
public class DecryptionContext
{
    /// <summary>
    /// The options to use while performing decryption operations.
    /// </summary>
    public required DecryptionOptions Options { get; init; }

    /// <summary>
    /// The file system to decrypt.
    /// </summary>
    public IFileSystem? FileSystem { get; set; }

    /// <summary>
    /// The current temp directory in use.
    /// </summary>
    public required DirectoryInfo TempDirectory { get; set; }

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