using LibHac.Common;
using LibHac.Crypto;
using LibHac.Fs.Fsa;
using LibHac.Tools.FsSystem;
using LibHac.Tools.FsSystem.NcaUtils;
using LibHacToolNX.Logging;
using LibHacToolNX.Tools.Writers.Impl;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenMode = LibHac.Fs.OpenMode;

namespace LibHacToolNX.Tools.Decryption.Impl;

/// <summary>
/// A mechanism to decrypt a firmware file.
/// </summary>
public class ZipFileFirmwareDecrypter : Decrypter
{
    /// <summary>
    /// Initializes an instance of the class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public ZipFileFirmwareDecrypter(ILogger logger)
        : base(logger) { }

    private async Task<byte[]> CalculateHashForFileAsync(FileInfo file, CancellationToken cancellationToken)
    {
        // Read the content data into memory.
        Memory<byte> contentBytes = new byte[file.Length];
        await using var contentStream = file.OpenRead();
        await contentStream.ReadExactlyAsync(contentBytes, cancellationToken);

        // Generate the new hash for the content.
        byte[] digest = new byte[Sha256.DigestSize];
        Sha256.GenerateSha256Hash(contentBytes.Span, digest);

        return digest;
    }

    public override async Task DecryptAsync(FileInfo source, Stream outputStream, DecryptionOptions options, CancellationToken cancellationToken = default)
    {
        var patchDir = options.TempDirectory.CreateSubdirectory("patch");

        try
        {
            var context = new ZipFirmwareDecryptionContext
            {
                Source = source,
                Options = options,
                OutputStream = outputStream,
                PatchDirectory = patchDir,
                TempDirectory = options.TempDirectory,
                RequestAborted = cancellationToken
            };

            await ExtractAndDecryptContentsAsync(context);
            await MapFilesAsync(context);
            await PatchTitlesAsync(context);

            await WriteToDiskAsync(context);
        }
        finally
        {
            if (patchDir.Exists)
            {
                patchDir.Delete(true);
            }
        }
    }

    protected async Task ExtractAndDecryptContentsAsync(ZipFirmwareDecryptionContext context)
    {
        Logger.WriteLine("Extracting contents...");
        Logger.Indent();

        using var zip = ZipFile.OpenRead(context.Source.FullName);

        try
        {
            foreach (var entry in zip.Entries)
            {
                context.RequestAborted.ThrowIfCancellationRequested();

                Logger.WriteLine(entry.FullName);

                await ExtractAndDecryptFileAsync(context, entry);
            }
        }
        finally
        {
            Logger.Outdent();
        }
    }

    private async Task ExtractAndDecryptFileAsync(ZipFirmwareDecryptionContext context, ZipArchiveEntry entry)
    {
        var tempFile = context.GetRandomTempFileName();

        try
        {
            entry.ExtractToFile(tempFile.FullName);

            var outputFile = new FileInfo(Path.Combine(context.TempDirectory.FullName, entry.Name));
            var tempDirectory = context.TempDirectory.CreateRandomSubdirectory();

            try
            {
                await using var outStream = outputFile.Open(FileMode.Create, FileAccess.Write, FileShare.None);

                var decrypter = CreateDecrypter(outputFile);
                await decrypter.DecryptAsync(tempFile, outStream, new DecryptionOptions
                {
                    KeySet = context.Options.KeySet,
                    TempDirectory = tempDirectory,
                    IntegrityCheckLevel = context.Options.IntegrityCheckLevel,
                    UseAlternateFormat = context.Options.UseAlternateFormat
                }, context.RequestAborted);

                context.DecryptedFiles.Add(outputFile);
            }
            finally
            {
                if (tempDirectory.Exists)
                {
                    tempDirectory.Delete(true);
                }
            }
        }
        finally
        {
            if (tempFile.Exists)
            {
                tempFile.Delete();
            }
        }
    }

    private IDecrypter CreateDecrypter(FileInfo source)
    {
        return source.Extension switch
        {
            ".nca" => new NcaDecrypter(Logger),
            _ => throw new NotSupportedException($"The extension '{source.Extension}' is not supported.")
        };
    }

    private async Task MapFilesAsync(ZipFirmwareDecryptionContext context)
    {
        Logger.WriteLine("Mapping files...");

        foreach (var file in context.DecryptedFiles)
        {
            await using var fs = file.OpenRead();
            var nca = BasicNca2.Create(fs);

            if (!context.Mappings.TryGetValue(nca.Header.TitleId, out var values))
            {
                values = new List<(NcaContentType, FileInfo)>();
                context.Mappings.Add(nca.Header.TitleId, values);
            }

            values.Add((nca.Header.ContentType, file));
        }
    }

    private async Task PatchTitlesAsync(ZipFirmwareDecryptionContext context)
    {
        Logger.WriteLine("Patching titles...");
        Logger.Indent();

        try
        {
            foreach (var entry in context.Mappings)
            {
                context.RequestAborted.ThrowIfCancellationRequested();

                Logger.WriteLine(entry.Key.ToString());
                Logger.Indent();

                try
                {
                    var metaFileEntry = entry.Value.Find(o => o.Type == NcaContentType.Meta).File;
                    var contentFileEntry = entry.Value.Find(o => o.Type != NcaContentType.Meta).File;

                    Logger.WriteLine(metaFileEntry.Name);
                    Logger.WriteLine(contentFileEntry?.Name);

                    if (contentFileEntry is null)
                    {
                        // There is no content associated with the file, just move it to the output folder.
                        context.OutputFiles.Add(metaFileEntry);
                    }
                    else
                    {
                        await RebuildContentMetaFileAsync(context, contentFileEntry, metaFileEntry);
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
            Logger.Outdent();
        }
    }

    private async Task RebuildContentMetaFileAsync(ZipFirmwareDecryptionContext context, FileInfo contentFile, FileInfo metaFile)
    {
        await using var metaStream = metaFile.OpenRead();

        var nca = BasicNca2.Create(metaStream);
        var sectionDescription = nca.Sections[NcaSectionType.Data];

        using IFileSystem fs = nca.OpenFileSystem(NcaSectionType.Data, IntegrityCheckLevel.ErrorOnInvalid);
        var cnmtPath = fs.EnumerateEntries("/", "*.cnmt").Single().FullPath;

        var metaFileRef = new UniqueRef<IFile>();
        fs.OpenFile(ref metaFileRef.Ref, cnmtPath.ToU8Span(), OpenMode.Read).ThrowIfFailure();

        await using var cnmtFile = metaFileRef.Get.AsStream();
        Memory<byte> cnmtBuffer = new byte[cnmtFile.Length];
        await cnmtFile.ReadExactlyAsync(cnmtBuffer, context.RequestAborted);

        CnmtWriter cnmtWriter = new(cnmtBuffer);
        if (cnmtWriter.ContentEntryCount != 1)
        {
            throw new InvalidOperationException($"The file contained an invalid number of content entries. Expected: 1, Actual: {cnmtWriter.ContentEntryCount}");
        }

        // Update the fields which need to be modified for the new contents.
        var e = cnmtWriter.GetPackagedContentInfo(0);
        e.Hash = await CalculateHashForFileAsync(contentFile, context.RequestAborted);
        e.Size = contentFile.Length;

        var patchFile = context.GetRandomTempFileName();

        try
        {
            await cnmtWriter.WriteAsync(patchFile, context.RequestAborted);

            var pfsWriter = new PartitionFileSystemWriter();
            pfsWriter.AddFile(cnmtPath, patchFile);

            var stageFile = context.GetRandomTempFileName();

            try
            {
                await pfsWriter.WriteAsync(stageFile, context.RequestAborted);

                NcaWriter ncaWriter = new()
                {
                    Description = new()
                    {
                        TitleId = nca.Header.TitleId,
                        ContentIndex = nca.Header.ContentIndex,
                        ContentType = nca.Header.ContentType,
                        DistributionType = nca.Header.DistributionType,
                        Magic = nca.Header.Magic,
                        SdkVersion = nca.Header.SdkVersion
                    },
                    UseAlternateFormat = context.Options.UseAlternateFormat
                };

                ncaWriter.AddSection(sectionDescription.SectionIndex, stageFile,
                    new NcaSectionDescription
                    {
                        SectionStartOffset = sectionDescription.SectionStartOffset,
                        FormatType = sectionDescription.FsHeader.FormatType,
                        Version = sectionDescription.FsHeader.Version,
                        HashType = NcaHashType.None,
                        Checksum = null,
                        PatchInfo = sectionDescription.FsHeader.GetPatchInfo()
                    });

                var outFile = new FileInfo(Path.Combine(context.PatchDirectory.FullName, metaFile.Name));
                await ncaWriter.WriteAsync(outFile, context.RequestAborted);

                context.OutputFiles.Add(contentFile);
                context.OutputFiles.Add(outFile);
            }
            finally
            {
                if (stageFile.Exists)
                {
                    stageFile.Delete();
                }
            }
        }
        finally
        {
            if (patchFile.Exists)
            {
                patchFile.Delete();
            }
        }
    }

    private async Task WriteToDiskAsync(ZipFirmwareDecryptionContext context)
    {
        Logger.WriteLine("Building archive...");
        Logger.Indent();

        try
        {
            using var archive = new ZipArchive(context.OutputStream, ZipArchiveMode.Create);

            foreach (var item in context.OutputFiles)
            {
                Logger.WriteLine(item.Name);

                var entry = archive.CreateEntry(item.Name);
                await using var inStream = item.OpenRead();
                await using var entryStream = entry.Open();

                await inStream.CopyToAsync(entryStream, context.RequestAborted);
                await entryStream.FlushAsync();
            }
        }
        finally
        {
            Logger.Outdent();
        }
    }

    /// <summary>
    /// Provides contextual information for a firmware decryption process.
    /// </summary>
    protected class ZipFirmwareDecryptionContext : DecryptionContext
    {
        /// <summary>
        /// The source file.
        /// </summary>
        public required FileInfo Source { get; init; }

        /// <summary>
        /// The output stream.
        /// </summary>
        public required Stream OutputStream { get; init; }

        /// <summary>
        /// The directory to use for patch files.
        /// </summary>
        public required DirectoryInfo PatchDirectory { get; init; }

        /// <summary>
        /// The list of extracted files which originated from the source.
        /// </summary>
        public List<FileInfo> DecryptedFiles { get; } = new();

        /// <summary>
        /// The list of output files to place within the output archive.
        /// </summary>
        public List<FileInfo> OutputFiles { get; } = new();

        /// <summary>
        /// The mappings for title ids with their respective files.
        /// </summary>
        public Dictionary<ulong, List<(NcaContentType Type, FileInfo File)>> Mappings { get; } = new();
    }
}