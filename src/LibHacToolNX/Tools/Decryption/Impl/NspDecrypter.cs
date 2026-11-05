using LibHac.Fs;
using LibHac.FsSystem;
using LibHacToolNX.Logging;
using LibHacToolNX.Tools.Writers.Impl;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LibHacToolNX.Tools.Decryption.Impl;

/// <summary>
/// A mechanism to decrypt an NSP file.
/// </summary>
public class NspDecrypter : StreamDecrypter
{
    /// <summary>
    /// Initializes an instance of the class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public NspDecrypter(ILogger logger)
        : base(logger) { }

    public override async Task DecryptAsync(Stream source, Stream outputStream, DecryptionOptions options, CancellationToken cancellationToken = default)
    {
        var fileSystem = PartitionFileSystem2.Create(source.AsStorage2());

        var context = new DecryptionContext
        {
            Options = options,
            FileSystem = fileSystem,
            TempDirectory = options.TempDirectory,
            RequestAborted = cancellationToken
        };

        var files = await ExtractContentsAsync(context);

        var writer = new PartitionFileSystemWriter();
        foreach (var file in files)
        {
            writer.AddFile(file.SourcePath, file.TemporaryPath);
        }

        await writer.WriteAsync(outputStream, cancellationToken);
    }
}