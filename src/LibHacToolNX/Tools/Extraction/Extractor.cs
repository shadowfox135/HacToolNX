using LibHac.Common;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.Tools.Fs;
using LibHac.Tools.FsSystem;
using LibHacToolNX.Logging;
using LibHacToolNX.Tools.Extraction.Impl;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Path = System.IO.Path;

namespace LibHacToolNX.Tools.Extraction;

/// <summary>
/// An abstract extractor.
/// </summary>
/// <remarks>This implementation has utility methods for assisting with extracting common file formats.</remarks>
public abstract class Extractor : IExtractor
{
    /// <summary>
    /// Gets the logger.
    /// </summary>
    protected ILogger Logger { get; }

    /// <summary>
    /// Initializes an instance of the class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    protected Extractor(ILogger logger)
    {
        Logger = logger;
    }

    public abstract Task ExtractAsync(FileInfo source, ExtractionOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts a file.
    /// </summary>
    /// <param name="fileSystem">The file system containing the file to extract.</param>
    /// <param name="file">The file to extract.</param>
    /// <param name="options">The extraction options.</param>
    /// <param name="currentDirectory">The current directory in use for extracting files.</param>
    /// <param name="cancellationToken">The cancellation token to monitor for cancellation requests.</param>
    /// <returns>The task to await.</returns>
    protected async Task ExtractFileAsync(IFileSystem fileSystem, DirectoryEntryEx file,
        ExtractionOptions options, DirectoryInfo currentDirectory, CancellationToken cancellationToken)
    {
        using var fileRef = new UniqueRef<IFile>();
        fileSystem.OpenFile(ref fileRef.Ref, file.FullPath.ToU8Span(), OpenMode.Read).ThrowIfFailure();

        await using var fs = fileRef.Get.AsStream();

        var fileName = Path.GetFileName(file.Name);
        if (options.Recursive && string.Equals(Path.GetExtension(fileName).ToLowerInvariant(), ".nca"))
        {
            var outDirectory = currentDirectory.CreateSubdirectory(fileName);

            try
            {
                Logger.Indent();

                var nca = new NcaExtractor(Logger);
                await nca.ExtractAsync(fs,
                    new ExtractionOptions
                    {
                        Destination = outDirectory,
                        KeySet = options.KeySet,
                        Recursive = options.Recursive,
                        Continue = options.Continue,
                        IntegrityCheckLevel = options.IntegrityCheckLevel
                    },
                    cancellationToken);

                return;
            }
            finally
            {
                Logger.Outdent();
            }
        }

        var outPath = Path.Combine(currentDirectory.FullName, file.ToLocalPath());
        await using var outStream = File.OpenWrite(outPath);

        await fs.CopyToAsync(outStream, cancellationToken);
        await outStream.FlushAsync(cancellationToken);
    }
}