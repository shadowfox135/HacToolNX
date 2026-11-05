using LibHac.Crypto;
using LibHac.Tools.FsSystem.NcaUtils;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using static LibHac.Tools.FsSystem.NcaUtils.NativeTypes;

namespace LibHacToolNX.Tools.Writers.Impl;

/// <summary>
/// A mechanism used to write NCA archives.
/// </summary>
public class NcaWriter : Writer
{
    private readonly (FileInfo File, NcaSectionDescription Description)?[] sections =
        new (FileInfo File, NcaSectionDescription Description)?[SectionCount];

    /// <summary>
    /// The description of the archive.
    /// </summary>
    public required NcaDescription Description { get; init; }

    /// <summary>
    /// Identifies whether the writer should write the output file with an alternate format.
    /// </summary>
    public required bool UseAlternateFormat { get; init; }

    /// <summary>
    /// Adds a section.
    /// </summary>
    /// <param name="sectionIndex">The section index.</param>
    /// <param name="file">The file containing the section contents.</param>
    /// <param name="description">The description of the section.</param>
    public void AddSection(int sectionIndex, FileInfo file, NcaSectionDescription description)
    {
        if (sectionIndex < 0 || sectionIndex >= SectionCount)
        {
            throw new ArgumentOutOfRangeException($"The section index must greater than or equal to zero, and less than {SectionCount}.");
        }

        sections[sectionIndex] = (file, description);
    }

    public override async Task WriteAsync(Stream outStream, CancellationToken cancellationToken = default)
    {
        var mergeFile = new FileInfo(Path.GetTempFileName());

        try
        {
            await using var mergeStream = mergeFile.Open(FileMode.Create, FileAccess.ReadWrite, FileShare.None);

            var context = new WriteContext
            {
                MergeStream = mergeStream
            };

            // The start block does not begin at the expected start block, prepend a buffer to re-align everything
            var initialOffset = sections.Where(o => o.HasValue).Min(o => o!.Value.Description.SectionStartOffset);
            context.StartBlock = (int)(initialOffset / BlockSize);

            var prepend = context.StartBlock - (HeaderSize / BlockSize);
            if (prepend > 0)
            {
                await mergeStream.WriteAsync(new byte[prepend * BlockSize], cancellationToken);
            }

            for (var i = SectionCount - 1; i >= 0; i--) // The sections are written in reverse order.
            {
                var section = sections[i];
                if (section == null)
                    continue;

                ref NcaSectionEntryStruct entry = ref context.GetSectionEntry(i);
                entry.StartBlock = context.StartBlock;
                entry.IsEnabled = true;

                var sectionSize = section.Value.File.Length;
                var blocks = (int)(sectionSize / BlockSize);
                if (sectionSize % BlockSize != 0)
                {
                    // Shift it to the next block as it doesn't fit perfectly.
                    blocks++;
                }

                entry.EndBlock = entry.StartBlock + blocks;
                context.StartBlock = entry.EndBlock;

                await using var fs = section.Value.File.OpenRead();
                await fs.CopyToAsync(mergeStream, cancellationToken);

                var padding = ((long)blocks * BlockSize) - sectionSize;
                if (padding > 0)
                {
                    await mergeStream.WriteAsync(new byte[padding], cancellationToken);
                    await mergeStream.FlushAsync(cancellationToken);
                }

                var sectionDescription = section.Value.Description;

                ref FsHeaderStruct fsHeader = ref context.GetFsHeader(i);
                fsHeader.Version = sectionDescription.Version;
                fsHeader.FormatType = (byte)sectionDescription.FormatType;
                fsHeader.HashType = (byte)sectionDescription.HashType;
                fsHeader.EncryptionType = (byte)NcaEncryptionType.None;

                Memory<byte> fsHeaderBytes = context.GetFsHeaderBytes(i);

                if (sectionDescription.Checksum != null)
                {
                    sectionDescription.Checksum.CopyTo(fsHeaderBytes.Slice(IntegrityInfoOffset, IntegrityInfoSize));
                }

                var oldPatchInfo = sectionDescription.PatchInfo;
                if (oldPatchInfo != null)
                {
                    // Wrap the FsHeader being constructed and transfer the data into it.
                    new NcaFsPatchInfo(fsHeaderBytes.Slice(PatchInfoOffset, PatchInfoSize))
                    {
                        EncryptionTreeOffset = oldPatchInfo.EncryptionTreeOffset,
                        EncryptionTreeSize = 0,
                        RelocationTreeOffset = oldPatchInfo.RelocationTreeOffset,
                        RelocationTreeSize = oldPatchInfo.RelocationTreeSize,
                        RelocationTreeHeader = oldPatchInfo.RelocationTreeHeader
                    };
                }

                var hash = context.GetFsHeaderHash(i);
                Sha256.GenerateSha256Hash(fsHeaderBytes.Span, hash.Span);
            }

            // Flush everything.
            await mergeStream.FlushAsync(cancellationToken);

            // Build the output header.
            ref NcaHeaderStruct header = ref context.Header;

            if (UseAlternateFormat)
            {
                header.Magic = BinaryPrimitives.ReadUInt32LittleEndian("DNCA"u8);
                header.KeyGeneration1 = 1;
            }
            else
            {
                header.Magic = Description.Magic;
                header.KeyGeneration1 = 0;
            }

            header.DistributionType = (byte)Description.DistributionType;
            header.ContentType = (byte)Description.ContentType;
            header.KeyAreaKeyIndex = 0;
            header.NcaSize = HeaderSize + mergeStream.Length;
            header.TitleId = Description.TitleId;
            header.ContentIndex = Description.ContentIndex;
            header.SdkVersion = Description.SdkVersion;
            header.KeyGeneration2 = 0;
            header.SignatureKeyGeneration = 0;

            // Write the header.
            await outStream.WriteAsync(context.HeaderBytes, cancellationToken);

            // Re-position the merged data.
            mergeStream.Seek(0, SeekOrigin.Begin);

            // Write the merged data.
            await mergeStream.CopyToAsync(outStream, cancellationToken);
            await outStream.FlushAsync(cancellationToken);
        }
        finally
        {
            if (mergeFile.Exists)
            {
                mergeFile.Delete();
            }
        }
    }

    private class WriteContext
    {
        public Memory<byte> HeaderBytes { get; } = new byte[HeaderSize];
        public ref NcaHeaderStruct Header => ref MemoryMarshal.Cast<byte, NcaHeaderStruct>(HeaderBytes.Span)[0];
        public required Stream MergeStream { get; init; }
        public int StartBlock { get; set; }

        public ref NcaSectionEntryStruct GetSectionEntry(int sectionIndex)
        {
            return ref MemoryMarshal.Cast<byte, NcaSectionEntryStruct>(
                HeaderBytes.Span.Slice(SectionEntriesOffset + (sectionIndex * SectionEntrySize), SectionEntrySize))[0];
        }

        public ref FsHeaderStruct GetFsHeader(int sectionIndex)
        {
            var headerBytes = GetFsHeaderBytes(sectionIndex);

            return ref MemoryMarshal.Cast<byte, FsHeaderStruct>(headerBytes.Span)[0];
        }

        public Memory<byte> GetFsHeaderBytes(int sectionIndex)
        {
            return HeaderBytes.Slice(FsHeadersOffset + (sectionIndex * FsHeaderSize), FsHeaderSize);
        }

        public Memory<byte> GetFsHeaderHash(int sectionIndex)
        {
            return HeaderBytes.Slice(FsHeaderHashOffset + (sectionIndex * FsHeaderHashSize), FsHeaderHashSize);
        }
    }
}