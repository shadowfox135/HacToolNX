using LibHacToolNX.Logging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LibHacToolNX.Tools.Decryption;

/// <summary>
/// An abstract decrypter for decrypting <see cref="Stream"/> instances.
/// </summary>
public abstract class StreamDecrypter : Decrypter
{
    /// <summary>
    /// Initializes an instance of the class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    protected StreamDecrypter(ILogger logger)
        : base(logger) { }

    public sealed override async Task DecryptAsync(FileInfo source, Stream outputStream, DecryptionOptions options, CancellationToken cancellationToken = default)
    {
        await using var fs = source.OpenRead();
        await DecryptAsync(fs, outputStream, options, cancellationToken);
    }

    /// <summary>
    /// Decrypts the file.
    /// </summary>
    /// <param name="source">The source <see cref="Stream"/> to decrypt.</param>
    /// <param name="outputStream">The output stream to receive the file.</param>
    /// <param name="options">The options to use while decrypting the stream.</param>
    /// <param name="cancellationToken">The cancellation token to monitor for cancellation requests.</param>
    /// <returns>The task to await.</returns>
    public abstract Task DecryptAsync(Stream source, Stream outputStream, DecryptionOptions options, CancellationToken cancellationToken = default);
}