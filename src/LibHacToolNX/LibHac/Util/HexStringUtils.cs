using System;
using System.Buffers;

namespace LibHac.Util;

internal static class HexStringUtils
{
    public static bool IsHexDigit(byte c)
    {
        return char.IsAsciiHexDigit((char)c);
    }

    public static bool TryFromHexString(ReadOnlySpan<char> chars, Span<byte> outputBytes)
    {
        if ((uint)chars.Length % 2 != 0)
            return false;

        uint bytesLength = (uint)chars.Length / 2;

        if ((uint)outputBytes.Length >= bytesLength)
        {
            Span<byte> bytes = outputBytes.Slice(0, (int)bytesLength);
            return Convert.FromHexString(chars, bytes, out _, out _)
                == OperationStatus.Done;
        }

        return false;
    }
}