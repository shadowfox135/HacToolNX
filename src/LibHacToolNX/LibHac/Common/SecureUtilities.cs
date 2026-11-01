using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LibHac.Common;

public static class SecureUtilities
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void XorArrays(Span<byte> transformData, ReadOnlySpan<byte> xorData)
    {
        for (var i = 0; i < transformData.Length; i++)
        {
            transformData[i] ^= xorData[i];
        }
    }

    public static void XorArrays(Span<byte> output, ReadOnlySpan<byte> input1, ReadOnlySpan<byte> input2)
    {
        int length = Math.Min(input1.Length, input2.Length);

        int sisdStart = 0;
        if (Vector.IsHardwareAccelerated)
        {
            int lengthVec = length / Vector<byte>.Count;

            Span<Vector<byte>> outputVec = MemoryMarshal.Cast<byte, Vector<byte>>(output);
            ReadOnlySpan<Vector<byte>> input1Vec = MemoryMarshal.Cast<byte, Vector<byte>>(input1);
            ReadOnlySpan<Vector<byte>> input2Vec = MemoryMarshal.Cast<byte, Vector<byte>>(input2);

            sisdStart = lengthVec * Vector<byte>.Count;

            for (int i = 0; i < lengthVec; i++)
            {
                outputVec[i] = input1Vec[i] ^ input2Vec[i];
            }
        }

        for (int i = sisdStart; i < length; i++)
        {
            output[i] = (byte)(input1[i] ^ input2[i]);
        }
    }
}