using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.FsSystem;
using LibHac.Tools.FsSystem;
using LibHacToolNX.Logging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LibHacToolNX.Tools.Extraction.Impl;

/// <summary>
/// A mechanism to extract the contents of an NSP file.
/// </summary>
public class NspExtractor : Extractor
{
    /// <summary>
    /// Initializes an instance of the class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public NspExtractor(ILogger logger)
        : base(logger) { }

    public override async Task ExtractAsync(FileInfo source, ExtractionOptions options, CancellationToken cancellationToken = default)
    {
        await using var inStream = source.OpenRead();
        IFileSystem fileSystem = PartitionFileSystem2.Create(inStream.AsStorage2());

        foreach (var entry in fileSystem.EnumerateEntries("/", "*", SearchOptions.RecurseSubdirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();

            Logger.WriteLine(entry.FullPath);

            await ExtractFileAsync(fileSystem, entry, options, options.Destination, cancellationToken);
        }
    }
}