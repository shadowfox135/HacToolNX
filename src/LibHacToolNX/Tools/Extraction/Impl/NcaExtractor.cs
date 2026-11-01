using LibHac.Common;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.Tools.FsSystem;
using LibHac.Tools.FsSystem.NcaUtils;
using LibHacToolNX.Logging;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Path = System.IO.Path;

namespace LibHacToolNX.Tools.Extraction.Impl;

/// <summary>
/// A mechanism to extract the contents of an NCA file.
/// </summary>
public class NcaExtractor : Extractor
{
    /// <summary>
    /// Initializes an instance of the class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public NcaExtractor(ILogger logger)
        : base(logger) { }

    public override async Task ExtractAsync(FileInfo source, ExtractionOptions options, CancellationToken cancellationToken = default)
    {
        await using var fs = source.OpenRead();
        await ExtractAsync(fs, options, cancellationToken);
    }

    public async Task ExtractAsync(Stream inStream, ExtractionOptions options, CancellationToken cancellationToken = default)
    {
        var nca = SecureNca2.Create(options.KeySet, inStream);

        foreach (var section in nca.Sections)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Logger.WriteLine($"{section.Key.ToString()} (EncryptionType={section.Value.FsHeader.EncryptionType}, HashType={section.Value.FsHeader.HashType}, Size={section.Value.SectionSize})");
            Logger.Indent();

            try
            {
                using var fileSystem = nca.OpenFileSystem(section.Key, options.IntegrityCheckLevel);

                var sectionDirectory = options.Destination.CreateSubdirectory(section.Key.ToString());
                await ExtractSectionAsync(fileSystem, section, sectionDirectory, cancellationToken);
            }
            finally
            {
                Logger.Outdent();
            }
        }
    }

    private async Task ExtractSectionAsync(IFileSystem fileSystem, KeyValuePair<NcaSectionType, Nca2<SecureNcaFsHeader2>.SectionDescription> section, DirectoryInfo outDirectory, CancellationToken cancellationToken)
    {
        foreach (var entry in fileSystem.EnumerateEntries("/", "*", SearchOptions.RecurseSubdirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var localFilePath = entry.ToLocalPath();
            if (entry.Type == DirectoryEntryType.Directory)
            {
                outDirectory.CreateSubdirectory(localFilePath);
            }
            else
            {
                Logger.WriteLine(entry.FullPath);

                var outFile = new FileInfo(Path.Combine(outDirectory.FullName, localFilePath));
                if (!outFile.Directory!.Exists)
                {
                    outFile.Directory.Create();
                }

                using var fileRef = new UniqueRef<IFile>();
                fileSystem.OpenFile(ref fileRef.Ref, entry.FullPath.ToU8Span(), OpenMode.Read).ThrowIfFailure();

                await using var fs = fileRef.Get.AsStream();
                await using var outStream = outFile.Open(FileMode.Create, FileAccess.ReadWrite, FileShare.None);

                await fs.CopyToAsync(outStream, cancellationToken);
                await outStream.FlushAsync(cancellationToken);
            }
        }
    }
}