using LibHac.Tools.Fs;
using LibHacToolNX.Logging;
using LibHacToolNX.Tools.Writers.Impl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LibHacToolNX.Tools.Decryption.Impl;

/// <summary>
/// A mechanism to decrypt an NSP file.
/// </summary>
public class XciDecrypter : StreamDecrypter
{
    /// <summary>
    /// Initializes an instance of the class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public XciDecrypter(ILogger logger)
        : base(logger) { }

    public override async Task DecryptAsync(Stream source, Stream outputStream, DecryptionOptions options, CancellationToken cancellationToken = default)
    {
        var xci = SecureXci2.Create(source);
        var context = new XciDecryptionContext
        {
            Source = source,
            Xci = xci,
            Options = options,
            TempDirectory = options.TempDirectory,
            RequestAborted = cancellationToken,
            OutputStream = outputStream
        };

        await ExtractPartitionsAsync(context);
        await RebuildPartitionsAsync(context);
        await RebuildRootFsAsync(context);

        await WriteToDiskAsync(context);
    }

    private async Task ExtractPartitionsAsync(XciDecryptionContext context)
    {
        Logger.WriteLine("Extracting partitions...");
        var tempDirectory = context.TempDirectory;

        try
        {
            foreach (var partitionType in Enum.GetValues<XciPartitionType>())
            {
                if (!context.Xci.HasPartition(partitionType))
                {
                    continue;
                }

                context.RequestAborted.ThrowIfCancellationRequested();

                var partitionFileName = partitionType.GetFileName();
                Logger.WriteLine($"/{partitionFileName}");
                context.TempDirectory = tempDirectory.CreateSubdirectory(partitionFileName);

                try
                {
                    Logger.Indent();

                    context.FileSystem = context.Xci.OpenPartition(partitionType);

                    try
                    {
                        context.Extractions.Add(partitionType, await ExtractContentsAsync(context));
                    }
                    finally
                    {
                        context.FileSystem.Dispose();
                        context.FileSystem = null;
                    }
                }
                finally
                {
                    Logger.Outdent();
                }
            }
        }
        finally
        {
            context.TempDirectory = tempDirectory;
        }
    }

    private async Task RebuildPartitionsAsync(XciDecryptionContext context)
    {
        Logger.WriteLine("Rebuilding partitions...");

        foreach (var extraction in context.Extractions)
        {
            var hfsWriter = new Sha256PartitionFileSystemWriter();

            foreach (var file in extraction.Value)
            {
                hfsWriter.AddFile(file.SourcePath, file.TemporaryPath);
            }

            var tempFile = context.GetRandomTempFileName();

            try
            {
                await using var tempFs = tempFile.OpenWrite();
                await hfsWriter.WriteAsync(tempFs, context.RequestAborted);

                context.Partitions.Add(extraction.Key, new PartitionInfo
                {
                    File = tempFile,
                    HeaderSize = hfsWriter.OutputHeaderSize
                });
            }
            catch (Exception)
            {
                if (tempFile.Exists)
                {
                    tempFile.Delete();
                }

                throw;
            }
        }
    }

    private async Task RebuildRootFsAsync(XciDecryptionContext context)
    {
        Logger.WriteLine("Rebuilding file system...");

        long length = 0;
        var rootFs = new Sha256PartitionFileSystemWriter();

        foreach (var partition in context.Partitions)
        {
            if (partition.Key == XciPartitionType.Secure)
            {
                context.RomAreaStartPage = (int)(length / 0x200);
            }

            rootFs.AddFile($"/{partition.Key.GetFileName()}", new Sha256PartitionFileEntry
            {
                File = partition.Value.File,
                HashTargetSize = partition.Value.HeaderSize
            });

            length += partition.Value.File.Length;

            if (partition.Key == XciPartitionType.Secure)
            {
                context.ValidDataEndPage = (int)(length / 0x200);
            }
        }

        var tempFile = context.GetRandomTempFileName();

        try
        {
            await using var tempFs = tempFile.OpenWrite();
            await rootFs.WriteAsync(tempFs, context.RequestAborted);

            context.RootPartition = tempFile;
            context.RootPartitionHeaderSize = rootFs.OutputHeaderSize;
        }
        catch (Exception)
        {
            if (tempFile.Exists)
            {
                tempFile.Delete();
            }

            throw;
        }
    }

    private async Task WriteToDiskAsync(XciDecryptionContext context)
    {
        Logger.WriteLine("Writing output file...");

        var header = context.Xci.Header;

        var writer = new XciWriter
        {
            Description = new XciDescription
            {
                PackageId = header.PackageId,
                GameCardSize = header.GameCardSize,
                Version = header.Version,
                Flags = header.Flags,
                RootPartitionOffset = header.RootPartitionOffset,
                RootPartitionHeaderSize = context.RootPartitionHeaderSize,
                RomAreaStartPage = context.RomAreaStartPage,
                ValidDataEndPage = context.ValidDataEndPage
            },
            RootPartition = context.RootPartition,
            UseAlternateFormat = context.Options.UseAlternateFormat
        };

        await writer.WriteAsync(context.OutputStream, context.RequestAborted);
    }

    /// <summary>
    /// Describes partition information for a partition file.
    /// </summary>
    private class PartitionInfo
    {
        /// <summary>
        /// The file.
        /// </summary>
        public required FileInfo File { get; init; }

        /// <summary>
        /// The size of the header.
        /// </summary>
        public int HeaderSize { get; internal set; }
    }

    /// <summary>
    /// Provides contextual information for an XCI decryption process.
    /// </summary>
    private class XciDecryptionContext : DecryptionContext
    {
        /// <summary>
        /// The target XCI file.
        /// </summary>
        public required Xci2 Xci { get; init; }

        /// <summary>
        /// The source stream containing the file.
        /// </summary>
        public required Stream Source { get; init; }

        /// <summary>
        /// The extracted partitions.
        /// </summary>
        public Dictionary<XciPartitionType, List<ExtractedFileInfo>> Extractions { get; } = new();

        /// <summary>
        /// The rebuilt partitions.
        /// </summary>
        public Dictionary<XciPartitionType, PartitionInfo> Partitions { get; } = new();

        /// <summary>
        /// The ROM area start page.
        /// </summary>
        public int RomAreaStartPage { get; set; }

        /// <summary>
        /// The valid data end page.
        /// </summary>
        public int ValidDataEndPage { get; set; }

        /// <summary>
        /// The rebuilt root partition.
        /// </summary>
        public FileInfo? RootPartition { get; set; }

        /// <summary>
        /// The output size of the root partition header.
        /// </summary>
        public int RootPartitionHeaderSize { get; set; }

        /// <summary>
        /// The stream which should receive the result.
        /// </summary>
        public required Stream OutputStream { get; init; }
    }
}