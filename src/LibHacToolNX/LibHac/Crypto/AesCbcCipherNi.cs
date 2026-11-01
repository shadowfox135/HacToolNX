using LibHac.Common;
using LibHac.Crypto.Impl;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace LibHac.Crypto;

public class AesCbcEncryptorNi : ICipherWithIv
{
    private AesCbcModeNi _baseCipher;

    public ref Buffer16 Iv => ref Unsafe.As<Vector128<byte>, Buffer16>(ref _baseCipher.Iv);

    public AesCbcEncryptorNi(ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv)
    {
        _baseCipher = new AesCbcModeNi();
        _baseCipher.Initialize(key, iv, false);
    }

    public int Transform(ReadOnlySpan<byte> input, Span<byte> output)
    {
        return _baseCipher.Encrypt(input, output);
    }
}