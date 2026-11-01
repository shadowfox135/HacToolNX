// Copyright (c) 2010 Gareth Lennox (garethl@dwakn.com)
// All rights reserved.

// Redistribution and use in source and binary forms, with or without modification,
// are permitted provided that the following conditions are met:

//     * Redistributions of source code must retain the above copyright notice,
//       this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice,
//       this list of conditions and the following disclaimer in the documentation
//       and/or other materials provided with the distribution.
//     * Neither the name of Gareth Lennox nor the names of its
//       contributors may be used to endorse or promote products derived from this
//       software without specific prior written permission.

// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
// THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using LibHac.Common;
using LibHac.Common.Keys;
using LibHac.Util;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.Buffers;
using System.Runtime.InteropServices;

namespace LibHac.Crypto;

/// <summary>
/// A mechanism which supports 128-bit AES-XTS transformations.
/// </summary>
public class Aes128XtsTransform2 : SecureCryptoTransform
{
    /// <summary>
    /// Defines the block size (in bytes).
    /// </summary>
    private const int BlockSizeBytes = 0x10;

    private readonly byte[] _cc = new byte[16];
    private readonly byte[] _pp = new byte[16];
    private readonly byte[] _t = new byte[16];
    private readonly bool _decrypting;

    private readonly BufferedBlockCipher _cipher1;
    private readonly BufferedBlockCipher _cipher2;

    /// <summary>
    /// Initializes an instance of the class.
    /// </summary>
    /// <param name="keySet">The key set</param>
    /// <param name="decrypting">true if decrypting data, otherwise false to encrypt.</param>
    public Aes128XtsTransform2(SecureKeySet keySet, bool decrypting)
    {
        _cipher1 = new BufferedBlockCipher(new AesEngine());
        _cipher1.Init(!decrypting, new KeyParameter(keySet.HeaderKey.SubKeys[0].DataRo));

        _cipher2 = new BufferedBlockCipher(new AesEngine());
        _cipher2.Init(true, new KeyParameter(keySet.HeaderKey.SubKeys[1].DataRo));

        _decrypting = decrypting;
    }

    /// <summary>
    /// Transforms a block.
    /// </summary>
    /// <param name="buffer">The input for which to compute the transform.</param>
    /// <param name="sector">The sector number of the block</param>
    /// <returns>The number of bytes transformed.</returns>
    public int Transform(Span<byte> buffer, ulong sector)
    {
        if (buffer.Length % BlockSizeBytes != 0)
        {
            throw new ArgumentException($"The buffer must be a multiple of {BlockSizeBytes}, partial blocks are not supported.");
        }

        int lim;
        var count = buffer.Length;
        var offset = 0;

        /* get number of blocks */
        var m = count >> 4;
        var mo = count & 15;
        var alignedCount = Alignment.AlignUp(count, BlockSizeBytes);

        /* for i = 0 to m-2 do */
        if (mo == 0)
        {
            lim = m;
        }
        else
        {
            lim = m - 1;
        }

        var tweak = ArrayPool<byte>.Shared.Rent(alignedCount);

        try
        {
            FillArrayFromSector(tweak, sector);

            /* encrypt the tweak */
            _cipher2.ProcessBytes(tweak, tweak);

            FillTweakBuffer(tweak.AsSpan(0, alignedCount));

            if (lim > 0)
            {
                var span = buffer.Slice(offset, lim * 16);

                SecureUtilities.XorArrays(span, tweak);
                _cipher1.ProcessBytes(span, span);
                SecureUtilities.XorArrays(span, tweak);
            }

            if (mo > 0)
            {
                Buffer.BlockCopy(tweak, lim * 16, _t, 0, 16);

                if (_decrypting)
                {
                    Buffer.BlockCopy(tweak, lim * 16 + 16, _cc, 0, 16);

                    /* CC = tweak encrypt block m-1 */
                    TweakCrypt(buffer, _pp, _cc);

                    /* Cm = first ptlen % 16 bytes of CC */
                    int i;
                    for (i = 0; i < mo; i++)
                    {
                        _cc[i] = buffer[16 + i + offset];
                        buffer[16 + i + offset] = _pp[i];
                    }

                    for (; i < 16; i++)
                    {
                        _cc[i] = _pp[i];
                    }

                    /* Cm-1 = Tweak encrypt PP */
                    TweakCrypt(_cc, buffer, _t);
                }
                else
                {
                    /* CC = tweak encrypt block m-1 */
                    TweakCrypt(buffer, _cc, _t);

                    /* Cm = first ptlen % 16 bytes of CC */
                    int i;
                    for (i = 0; i < mo; i++)
                    {
                        _pp[i] = buffer[16 + i + offset];
                        buffer[16 + i + offset] = _cc[i];
                    }

                    for (; i < 16; i++)
                    {
                        _pp[i] = _cc[i];
                    }

                    /* Cm-1 = Tweak encrypt PP */
                    TweakCrypt(_pp, buffer, _t);
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(tweak);
        }

        return count;
    }

    private static void FillTweakBuffer(Span<byte> buffer)
    {
        var bufL = MemoryMarshal.Cast<byte, ulong>(buffer);

        ulong a = bufL[1];
        ulong b = bufL[0];

        for (int i = 2; i < bufL.Length; i += 2)
        {
            ulong tt = unchecked((ulong)((long)a >> 63) & 0x87);

            a = (a << 1) | (b >> 63);
            b = (b << 1) ^ tt;

            bufL[i + 1] = a;
            bufL[i] = b;
        }
    }

    /// <summary>
    /// Fills a byte array from a sector number (little endian)
    /// </summary>
    /// <param name="value">The destination</param>
    /// <param name="sector">The sector number</param>
    private static void FillArrayFromSector(byte[] value, ulong sector)
    {
        for (var i = 0xF; i >= 0; i--)
        {
            value[i] = unchecked((byte)sector);
            sector >>= 8;
        }
    }

    /// <summary>
    /// Performs the Xts TweakCrypt operation
    /// </summary>
    private void TweakCrypt(Span<byte> inputBuffer, Span<byte> outputBuffer, Span<byte> t)
    {
        for (var x = 0; x < 16; x++)
        {
            outputBuffer[x] = (byte)(inputBuffer[x] ^ t[x]);
        }

        _cipher1.ProcessBytes(outputBuffer.Slice(0, 16), outputBuffer);

        for (var x = 0; x < 16; x++)
        {
            outputBuffer[x] = (byte)(outputBuffer[x] ^ t[x]);
        }

        MultiplyByX(t);
    }

    /// <summary>
    /// Multiply by x
    /// </summary>
    /// <param name="i">The value to multiply by x (LFSR shift)</param>
    private static void MultiplyByX(Span<byte> i)
    {
        byte t = 0, tt = 0;

        for (var x = 0; x < 16; x++)
        {
            tt = (byte)(i[x] >> 7);
            i[x] = (byte)(((i[x] << 1) | t) & 0xFF);

            t = tt;
        }

        if (tt > 0)
        {
            i[0] ^= 0x87;
        }
    }
}