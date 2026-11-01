using LibHac.Common;
using LibHac.Crypto;
using LibHac.Tools.Fs;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using static LibHac.Tools.Fs.NativeTypes;

namespace LibHacToolNX.Tools.Writers.Impl;

/// <summary>
/// A writer for creating XCI archives.
/// </summary>
public class XciWriter : Writer
{
    /// <summary>
    /// The description of the archive.
    /// </summary>
    public required XciDescription Description { get; init; }

    /// <summary>
    /// The file to use as the root file system.
    /// </summary>
    public FileInfo? RootPartition { get; set; }

    public override async Task WriteAsync(Stream outStream, CancellationToken cancellationToken = default)
    {
        if (RootPartition == null)
        {
            throw new InvalidOperationException("The root partition must be set.");
        }

        using var headerBytes = new RentedArray2<byte>(HeaderSize);

        var header = new XciHeader2(headerBytes.Memory)
        {
            PackageId = Description.PackageId,
            GameCardSize = Description.GameCardSize,
            Flags = Description.Flags,
            BackupAreaStartPage = -1,
            Version = Description.Version,
            RootPartitionOffset = Description.RootPartitionOffset,
            RootPartitionHeaderSize = Description.RootPartitionHeaderSize,
            LimAreaStartPage = Description.RomAreaStartPage,
            RomAreaStartPage = Description.RomAreaStartPage,
            ValidDataEndPage = Description.ValidDataEndPage
        };

        HeaderSignature.CopyTo(header.Magic.Span);

        var hash = await GenerateRootPartitionHeaderHashAsync(cancellationToken);
        if (hash != null)
        {
            // Update the header.
            header.RootPartitionHeaderHash = hash;
        }

        // Write header
        await outStream.WriteAsync(headerBytes.Memory, cancellationToken);

        // Write the unused data blocks.
        await outStream.WriteAsync(new byte[Description.RootPartitionOffset - HeaderSize], cancellationToken);
        await outStream.FlushAsync(cancellationToken);

        // Write the normal area.
        await using var rootFs = RootPartition.OpenRead();

        await rootFs.CopyToAsync(outStream, cancellationToken);
        await outStream.FlushAsync(cancellationToken);
    }

    private async Task<byte[]?> GenerateRootPartitionHeaderHashAsync(CancellationToken cancellationToken)
    {
        await using var rootFs = RootPartition!.OpenRead();

        Memory<byte> tempBuffer = new byte[Description.RootPartitionHeaderSize];
        await rootFs.ReadExactlyAsync(tempBuffer, cancellationToken);

        Memory<byte> buffer = new byte[Sha256.DigestSize];
        Sha256.GenerateSha256Hash(tempBuffer.Span, buffer.Span);

        return buffer.ToArray();
    }
}