using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LibHacToolNX.Tools.Writers;

/// <summary>
/// A mechanism used to write to an output stream.
/// </summary>
public abstract class Writer
{
    /// <summary>
    /// Writes the archive.
    /// </summary>
    /// <param name="outputFile">The file name to write.</param>
    /// <param name="cancellationToken">The cancellation token to monitor for cancellation requests.</param>
    /// <returns>The task to await.</returns>
    public async Task WriteAsync(FileInfo outputFile, CancellationToken cancellationToken = default)
    {
        await using var outStream = outputFile.OpenWrite();
        await WriteAsync(outStream, cancellationToken);
    }

    /// <summary>
    /// Writes the archive.
    /// </summary>
    /// <param name="outStream">The <see cref="Stream"/> to receive the archive.</param>
    /// <param name="cancellationToken">The cancellation token to monitor for cancellation requests.</param>
    /// <returns>The task to await.</returns>
    public abstract Task WriteAsync(Stream outStream, CancellationToken cancellationToken = default);
}