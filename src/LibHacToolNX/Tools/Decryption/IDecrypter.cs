using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LibHacToolNX.Tools.Decryption;

/// <summary>
/// A mechanism capable of decrypting a file.
/// </summary>
public interface IDecrypter
{
    /// <summary>
    /// Decrypts the file.
    /// </summary>
    /// <param name="source">The source file to decrypt.</param>
    /// <param name="outputStream">The output stream to receive the file.</param>
    /// <param name="options">The options to use while decrypting the file.</param>
    /// <param name="cancellationToken">The cancellation token to monitor for cancellation requests.</param>
    /// <returns>The task to await.</returns>
    Task DecryptAsync(FileInfo source, Stream outputStream, DecryptionOptions options, CancellationToken cancellationToken = default);
}