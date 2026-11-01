using LibHac.Crypto;
using LibHac.Fs;
using LibHac.Tools.FsSystem.NcaUtils;
using LibHac.Util;
using System;

namespace LibHac.Tools.FsSystem;

/// <summary>
/// A storage which uses an <see cref="Aes128CtrTransform2"/> cipher implementation to transform the data within the blocks.
/// </summary>
public class Aes128CtrStorage2 : Storage2
{
    private readonly IStorage _baseStorage;
    private readonly Aes128CtrTransform2 _transform;
    private readonly ulong[] _startCounter;

    protected Aes128CtrStorage2(IStorage baseStorage, Aes128CtrTransform2 transform, Span<ulong> counter)
    {
        _baseStorage = baseStorage;
        _transform = transform;
        _startCounter = counter.ToArray();
    }

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    /// <param name="baseStorage">The base storage from which the data will be read.</param>
    /// <param name="description">The description regarding the section data.</param>
    /// <returns>The new instance.</returns>
    public static Aes128CtrStorage2 Create(IStorage baseStorage, StorageDescription description)
    {
        ArgumentNullException.ThrowIfNull(baseStorage);

        Span<ulong> counter = CreateCounter(description.Counter, (ulong)description.SectionStartOffset);

        return Create(baseStorage, description.EncryptionKey.Key.Span, counter);
    }

    /// <summary>
    /// Describes the storage.
    /// </summary>
    public readonly struct StorageDescription
    {
        /// <summary>
        /// The upper counter.
        /// </summary>
        public ulong Counter { get; init; }

        /// <summary>
        /// The section start offset.
        /// </summary>
        /// <remarks>Used to calculate the low component of the counter.</remarks>
        public long SectionStartOffset { get; init; }

        /// <summary>
        /// The encryption key.
        /// </summary>
        public NcaEncryptionKey EncryptionKey { get; init; }
    }

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    /// <param name="baseStorage">The base storage from which the data will be read.</param>
    /// <param name="key">The encryption key.</param>
    /// <param name="counter">The counter value.</param>
    /// <returns>The new instance.</returns>
    public static Aes128CtrStorage2 Create(IStorage baseStorage, Span<byte> key, Span<ulong> counter)
    {
        ArgumentNullException.ThrowIfNull(baseStorage);

        // Transfer the counter into the starting counter.
        var startCounter = new ulong[counter.Length];
        counter.CopyTo(startCounter);

        return new Aes128CtrStorage2(baseStorage,
            new Aes128CtrTransform2(key.ToArray(), false),
            startCounter);
    }

    /// <summary>
    /// Creates a counter.
    /// </summary>
    /// <remarks>Be advised, the result of this operation is little-endian, and expects for the <see cref="Aes128CtrStorage2"/> to convert it as big-endian where required.</remarks>
    /// <param name="high">The high part.</param>
    /// <param name="low">The low part.</param>
    /// <returns>The binary counter.</returns>
    public static ulong[] CreateCounter(ulong high, ulong low)
    {
        return
        [
            high,
            low / Aes128CtrTransform2.BlockSizeBytes
        ];
    }

    public override Result GetSize(out long size)
    {
        _baseStorage.GetSize(out size).ThrowIfFailure();
        return Result.Success;
    }

    protected override void ReadCore(long offset, Span<byte> buffer)
    {
        int bytesRead = 0;

        // Calculate the starting block of the offset.
        long startBlock = BitUtil.DivideUp(offset, Aes128CtrTransform2.BlockSizeBytes);
        if (offset % Aes128CtrTransform2.BlockSizeBytes != 0)
        {
            // Reset the start block so it's the previous block.
            startBlock--;

            // Calculate the actual offset to to begin reading the data (since it will not match the original offset).
            var startOffset = startBlock * Aes128CtrTransform2.BlockSizeBytes;
            bytesRead += ReadHeadAdjustment(offset, startOffset, startBlock, buffer);

            // Reset the start block back to the original value.
            startBlock++;
        }

        var blocks = (buffer.Length - bytesRead) / Aes128CtrTransform2.BlockSizeBytes;
        if (blocks >= 1)
        {
            bytesRead += ReadAndTransform(offset + bytesRead, startBlock,
                buffer.Slice(bytesRead, blocks * Aes128CtrTransform2.BlockSizeBytes));

            startBlock += blocks;
        }

        var remainder = buffer.Length - bytesRead;
        if (remainder > 0)
        {
            ReadTailAdjustment(remainder, offset + bytesRead, startBlock,
                buffer.Slice(bytesRead, remainder));
        }
    }

    private int ReadHeadAdjustment(long originalOffset, long startOffset, long startBlock, Span<byte> buffer)
    {
        // Read the head into the buffer to get it back into alignment.
        Span<byte> temp = stackalloc byte[Aes128CtrTransform2.BlockSizeBytes];
        ReadAndTransform(startOffset, startBlock, temp);

        var readOffset = (int)(originalOffset - startOffset);

        var bytesToCopy = Aes128CtrTransform2.BlockSizeBytes - readOffset;
        if (bytesToCopy > buffer.Length)
        {
            bytesToCopy = buffer.Length;
        }

        // Transfer the data into the output buffer.
        temp.Slice(readOffset, bytesToCopy).CopyTo(buffer);

        return bytesToCopy;
    }

    private int ReadAndTransform(long offset, long startBlock, Span<byte> buffer)
    {
        // Read the data from the underlying storage.
        _baseStorage.Read(offset, buffer);

        // Decrypt it.
        Span<ulong> counter = stackalloc ulong[2];
        FillCounter(startBlock, counter);
        _transform.Transform(counter, buffer);

        return buffer.Length;
    }

    /// <summary>
    /// Fills the counter.
    /// </summary>
    /// <param name="startBlock">The start block.</param>
    /// <param name="counter">The counter to fill.</param>
    protected virtual void FillCounter(long startBlock, Span<ulong> counter)
    {
        counter[0] = _startCounter[0];
        counter[1] = _startCounter[1] + (ulong)startBlock;
    }

    private void ReadTailAdjustment(int remainder, long offset, long startBlock, Span<byte> buffer)
    {
        Span<byte> temp = stackalloc byte[Aes128CtrTransform2.BlockSizeBytes];
        ReadAndTransform(offset, startBlock, temp);

        temp[..remainder].CopyTo(buffer);
    }

    protected override void Dispose(bool disposing)
    {
        _transform.Dispose();

        base.Dispose(disposing);
    }
}