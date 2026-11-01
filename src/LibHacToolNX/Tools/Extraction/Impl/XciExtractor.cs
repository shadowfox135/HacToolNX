using LibHac.Fs.Fsa;
using LibHac.Tools.Fs;
using LibHac.Tools.FsSystem;
using LibHacToolNX.Logging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LibHacToolNX.Tools.Extraction.Impl;

/// <summary>
/// A mechanism to extract the contents of an XCI file.
/// </summary>
public class XciExtractor : Extractor
{
    /// <summary>
    /// Initializes an instance of the class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public XciExtractor(ILogger logger)
        : base(logger) { }

    /// <summary>
    /// Defines the partition types available.
    /// </summary>
    private static readonly XciPartitionType[] _partitionTypes =
        typeof(XciPartitionType).GetEnumValues().Cast<XciPartitionType>().ToArray();

    public override async Task ExtractAsync(FileInfo source, ExtractionOptions options, CancellationToken cancellationToken = default)
    {
        await using var inStream = source.OpenRead();
        var xci = Xci2.Create(inStream);

        foreach (var partitionType in _partitionTypes)
        {
            if (!xci.HasPartition(partitionType))
                continue;

            cancellationToken.ThrowIfCancellationRequested();

            Logger.WriteLine($"/{partitionType.GetFileName()}");
            Logger.Indent();

            try
            {
                var partitionDirectory = options.Destination.CreateSubdirectory(partitionType.GetFileName());
                await ExtractPartitionAsync(xci, partitionType, partitionDirectory, options, cancellationToken);
            }
            finally
            {
                Logger.Outdent();
            }
        }
    }

    private async Task ExtractPartitionAsync(Xci2 xci, XciPartitionType partitionType, DirectoryInfo partitionDirectory, ExtractionOptions options, CancellationToken cancellationToken)
    {
        IFileSystem fileSystem = xci.OpenPartition(partitionType);

        foreach (var file in fileSystem.EnumerateEntries("/", "*", SearchOptions.RecurseSubdirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();

            Logger.WriteLine(file.FullPath);

            await ExtractFileAsync(fileSystem, file, options, partitionDirectory, cancellationToken);
        }
    }
}