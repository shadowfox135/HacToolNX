using LibHac.Util;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using System;

namespace LibHac.Crypto;

/// <summary>
/// An implementation of the AES-ECB transform.
/// </summary>
public class Aes128EcbTransform : SecureCryptoTransform
{
    /// <summary>
    /// Defines the block size (in bytes).
    /// </summary>
    public const int BlockSizeBytes = 0x10;

    private readonly AesEngine _cipher;

    /// <summary>
    /// Initializes an instance of the class.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="encrypt">true to encrypt, otherwise false to decrypt.</param>
    /// <exception cref="ArgumentException"><paramref name="key"/>is the wrong length. Both values must be exactly 16 bytes.</exception>
    public Aes128EcbTransform(byte[] key, bool encrypt)
    {
        if (key.Length != BlockSizeBytes)
        {
            throw new ArgumentException($"{nameof(key)} must be {BlockSizeBytes} bytes long");
        }

        _cipher = new AesEngine();
        _cipher.Init(encrypt, new KeyParameter(key));
    }

    /// <summary>
    /// Transforms a block.
    /// </summary>
    /// <param name="buffer"> The input for which to compute the transform.</param>
    /// <returns>The number of bytes transformed.</returns>
    public int Transform(Span<byte> buffer)
    {
        if (buffer.Length % BlockSizeBytes != 0)
        {
            throw new ArgumentException($"The buffer must be a multiple of {BlockSizeBytes}, partial blocks are not supported.");
        }

        var blocks = BitUtil.DivideUp(buffer.Length, BlockSizeBytes);
        var total = 0;

        for (var i = 0; i < blocks; i++)
        {
            var slice = buffer.Slice(i * BlockSizeBytes, BlockSizeBytes);
            total += _cipher.ProcessBlock(slice, slice);
        }

        return total;
    }
}