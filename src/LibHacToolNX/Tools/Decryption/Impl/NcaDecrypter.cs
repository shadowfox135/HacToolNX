using LibHac.Fs;
using LibHac.Tools.FsSystem.NcaUtils;
using LibHacToolNX.Logging;
using LibHacToolNX.Tools.Writers.Impl;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LibHacToolNX.Tools.Decryption.Impl;

/// <summary>
/// A mechanism to decrypt an NCA file.
/// </summary>
public class NcaDecrypter : StreamDecrypter
{
    /// <summary>
    /// Initializes an instance of the class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public NcaDecrypter(ILogger logger)
        : base(logger) { }

    public override async Task DecryptAsync(Stream source, Stream outputStream, DecryptionOptions options, CancellationToken cancellationToken = default)
    {
        var nca = SecureNca2.Create(options.KeySet, source);
        var sections = await ExtractSectionsAsync(nca, options.TempDirectory, cancellationToken);

        var header = nca.Header;
        var writer = new NcaWriter
        {
            Description = new NcaDescription
            {
                Magic = header.Magic,
                DistributionType = header.DistributionType,
                ContentType = header.ContentType,
                TitleId = header.TitleId,
                ContentIndex = header.ContentIndex,
                SdkVersion = header.SdkVersion
            }
        };

        foreach (var section in sections)
        {
            var description = section.Value.Description;

            writer.AddSection(description.SectionIndex, section.Value.File, new NcaSectionDescription
            {
                SectionStartOffset = description.SectionStartOffset,
                Version = description.FsHeader.Version,
                FormatType = description.FsHeader.FormatType,
                HashType = description.FsHeader.HashType,
                Checksum = description.FsHeader.Checksum.ToArray(),
                PatchInfo = description.FsHeader.GetPatchInfo()
            });
        }

        await writer.WriteAsync(outputStream, cancellationToken);
    }

    private async Task<Dictionary<NcaSectionType, (SecureNca2.SectionDescription Description, FileInfo File)>> ExtractSectionsAsync(
        SecureNca2 nca, DirectoryInfo tempDirectory, CancellationToken cancellationToken)
    {
        Dictionary<NcaSectionType, (SecureNca2.SectionDescription Header, FileInfo File)> result = new();

        foreach (var section in nca.Sections)
        {
            var outputFile = new FileInfo(System.IO.Path.Combine(tempDirectory.FullName, System.IO.Path.GetRandomFileName()));

            await using var inStream = nca.OpenRawStorage(section.Key).AsStream2();
            await using var outStream = outputFile.Open(FileMode.Create, FileAccess.ReadWrite, FileShare.None);

            await inStream.CopyToAsync(outStream, cancellationToken);
            await outStream.FlushAsync(cancellationToken);

            result[section.Key] = (section.Value, outputFile);
        }

        return result;
    }
}