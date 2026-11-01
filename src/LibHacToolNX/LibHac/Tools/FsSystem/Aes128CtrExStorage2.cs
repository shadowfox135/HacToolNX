using LibHac.Common;
using LibHac.Crypto;
using LibHac.Fs;
using LibHac.FsSystem;
using LibHac.Tools.FsSystem.NcaUtils;
using System;
using System.Buffers.Binary;
using System.Linq;
using System.Runtime.InteropServices;

namespace LibHac.Tools.FsSystem;

/// <summary>
/// A storage which uses an extended <see cref="Aes128CtrStorage2"/> implementation for ciphered data
/// along with a relocation tree for data block positions.
/// </summary>
public class Aes128CtrExStorage2 : Aes128CtrStorage2
{
    /// <summary>
    /// The bucket tree handling counter translation.
    /// </summary>
    protected BucketTree2<Entry> BucketTree { get; }

    private BucketTree2<Entry>.BucketTreeEntry current;

    private Aes128CtrExStorage2(IStorage baseStorage, Aes128CtrTransform2 transform, Span<ulong> counter, BucketTree2<Entry> index)
        : base(baseStorage, transform, counter)
    {
        BucketTree = index;
    }

    /// <summary>
    /// The definition for an AES-128-CTR-EX encryption bucket tree entry.
    /// </summary>
    /// <remarks>See: https://switchbrew.org/wiki/NCA#RomFs_Patching for more information.</remarks>
    [StructLayout(LayoutKind.Sequential, Size = 0x10)]
    public readonly struct Entry : IBucketTreeEntry
    {
        /// <summary>
        /// The entry offset.
        /// </summary>
        public long Offset { get; init; }

        /// <summary>
        /// Unused.
        /// </summary>
        public int Reserved { get; init; }

        /// <summary>
        /// The generation component of the encryption counter to use for the entry.
        /// </summary>
        public int Generation { get; init; }

        long IBucketTreeEntry.Offset => Offset;

        public override string ToString()
        {
            return $"{{ Offset={Offset}, Reserved={Reserved}, Generation={Generation} }}";
        }
    }

    protected override void ReadCore(long offset, Span<byte> buffer)
    {
        int bytesRead = 0;

        while (bytesRead < buffer.Length)
        {
            // Determine how many bytes still need to be read from storage.
            var remaining = buffer.Length - bytesRead;

            // Adjust the subsection component of the high value to match the generation.
            current = BucketTree.Find(offset);

            var endOffset = current.EndOffset;
            if (endOffset == -1)
            {
                // The block has no end offset specified, it must be the last block.
                GetSize(out endOffset).ThrowIfFailure();
            }

            // Determine how many bytes are remaining in the current block.
            long blockRemaining = Math.Min(remaining, endOffset - offset);
            while (blockRemaining > 0)
            {
                var bytesToRead = (int)Math.Min(blockRemaining, int.MaxValue);
                base.ReadCore(offset, buffer.Slice(bytesRead, bytesToRead));

                offset += bytesToRead;
                bytesRead += bytesToRead;
                blockRemaining -= bytesToRead;
            }
        }
    }

    protected override void FillCounter(long startBlock, Span<ulong> counter)
    {
        base.FillCounter(startBlock, counter);

        Span<byte> counterBytes = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(counterBytes, counter[0]);
        BinaryPrimitives.WriteUInt32BigEndian(counterBytes[4..], (uint)current.Value.Generation);

        counter[0] = BinaryPrimitives.ReadUInt64BigEndian(counterBytes);
    }

    /// <summary>
    /// Creates an <see cref="Aes128CtrExStorage2"/> instance.
    /// </summary>
    /// <param name="baseStorage">The base storage from which the data will be read.</param>
    /// <param name="description">The description regarding the section data.</param>
    /// <returns>The new instance.</returns>
    /// <exception cref="ArgumentException">The header contains invalid data.</exception>
    public static Aes128CtrExStorage2 CreateEx(IStorage baseStorage, StorageDescriptionEx description)
    {
        ArgumentNullException.ThrowIfNull(baseStorage);

        if (description.EncryptionKey.Key.Span.IsZeros())
        {
            throw new ArgumentException("The description must contain an encryption key.", nameof(description));
        }

        var counter = CreateCounter(description.Counter,
            (ulong)description.SectionStartOffset + (ulong)description.SectionEndOffset);

        var encryptionTreeStorage = Create(
            baseStorage.Slice2(description.SectionEndOffset, description.EncryptionTreeSize),
            description.EncryptionKey.Key.Span, counter);

        var encryptionTree = BucketTree2<Entry>.Create(encryptionTreeStorage,
            new BucketTreeDefinition
            {
                Header = description.EncryptionTreeHeader,
                Length = description.EncryptionTreeSize
            });

        var counterEx = CreateCounter(description.Counter, (ulong)description.SectionStartOffset);

        return new Aes128CtrExStorage2(
            baseStorage.Slice2(0, description.SectionEndOffset),
            new Aes128CtrTransform2(description.EncryptionKey.Key.ToArray(), false),
            counterEx, encryptionTree);
    }

    /// <summary>
    /// Describes the section.
    /// </summary>
    public readonly struct StorageDescriptionEx
    {
        /// <summary>
        /// The encryption key.
        /// </summary>
        public NcaEncryptionKey EncryptionKey { get; init; }

        /// <summary>
        /// The encryption tree header.
        /// </summary>
        public BucketTreeHeader EncryptionTreeHeader { get; init; }

        /// <summary>
        /// The section start offset.
        /// </summary>
        public long SectionStartOffset { get; init; }

        /// <summary>
        /// The section end offset.
        /// </summary>
        public long SectionEndOffset { get; init; }

        /// <summary>
        /// The size of the encryption tree.
        /// </summary>
        public long EncryptionTreeSize { get; init; }

        /// <summary>
        /// The counter.
        /// </summary>
        public ulong Counter { get; init; }
    }
}