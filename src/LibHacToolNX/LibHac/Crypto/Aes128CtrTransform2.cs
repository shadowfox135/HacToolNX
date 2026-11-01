using LibHac.Util;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace LibHac.Crypto;

/// <summary>
/// An implementation of the AES-CTR transform.
/// </summary>
/// <remarks>See NIST Special Publication 800-38A for more information.</remarks>
public class Aes128CtrTransform2 : SecureCryptoTransform
{
    /// <summary>
    /// Defines the block size (in bytes).
    /// </summary>
    public const int BlockSizeBytes = 0x10;

    private readonly byte[] _key;
    private readonly bool _encrypt;
    private readonly AesEngine _aes;

    /// <summary>
    /// Initializes an instance of the class.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="encrypt">true to encrypt, otherwise false to decrypt.</param>
    /// <exception cref="ArgumentException"><paramref name="key"/>is the wrong length. Both values must be exactly 16 bytes.</exception>
    public Aes128CtrTransform2(byte[] key, bool encrypt)
    {
        if (key.Length != BlockSizeBytes)
        {
            throw new ArgumentException($"{nameof(key)} must be {BlockSizeBytes} bytes long");
        }

        _key = key;
        _encrypt = encrypt;
        _aes = new AesEngine();
    }

    /// <summary>
    /// Transforms the data.
    /// </summary>
    /// <param name="counter">The starting counter to use.</param>
    /// <param name="data">The data to transform.</param>
    /// <returns>The number of bytes transformed.</returns>
    public int Transform(Span<ulong> counter, Span<byte> data)
    {
        if (data.Length % BlockSizeBytes != 0)
        {
            throw new ArgumentException($"The buffer must be a multiple of {BlockSizeBytes}, partial blocks are not supported.");
        }

        var blockCount = BitUtil.DivideUp(data.Length, BlockSizeBytes);

        ulong high = counter[0];
        ulong low = counter[1];

        try
        {
            // Switch the counter endian-ness to comply with NIST SP 800-38A.
            counter[0] = BinaryPrimitives.ReverseEndianness(high);
            counter[1] = BinaryPrimitives.ReverseEndianness(low);

            byte[] counterBytes = MemoryMarshal.AsBytes(counter).ToArray();

            var cipher = new SicBlockCipher(_aes);
            cipher.Init(_encrypt, new ParametersWithIV(new KeyParameter(_key), counterBytes));

            for (var i = 0; i < blockCount; i++)
            {
                Span<byte> block = data.Slice(i * BlockSizeBytes, BlockSizeBytes);

                cipher.ProcessBlock(block, block);
                low++;
            }

            return data.Length;
        }
        finally
        {
            // Switch the counter endian-ness back for normal operating values.
            counter[0] = high;
            counter[1] = low;
        }
    }
}