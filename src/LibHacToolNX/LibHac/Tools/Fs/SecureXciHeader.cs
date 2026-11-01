using LibHac.Fs;
using System;
using System.Linq;
using static LibHac.Tools.Fs.NativeTypes;
using static LibHac.Tools.Fs.SecureNativeTypes;

namespace LibHac.Tools.Fs;

/// <summary>
/// Describes a secure XCI card header.
/// </summary>
public class SecureXciHeader2 : XciHeader2
{
    /// <summary>
    /// Creates a new header.
    /// </summary>
    /// <remarks>Be advised, this method expects the header to begin at offset 0. Please ensure any required storage slicing occurs before this method is executed.</remarks>
    /// <param name="storage">The storage containing the header.</param>
    /// <returns>The header.</returns>
    public static new SecureXciHeader2 Create(IStorage storage)
    {
        var buffer = new byte[HeaderSize];
        storage.Read(0, buffer).ThrowIfFailure();

        return new SecureXciHeader2(buffer);
    }

    /// <summary>
    /// Creates an instance of the class.
    /// </summary>
    /// <param name="buffer">The buffer containing the header data.</param>
    public SecureXciHeader2(Memory<byte> buffer)
        : base(buffer) { }

    /// <summary>
    /// The AES-CBC IV.
    /// </summary>
    /// <remarks>Be aware this value is stored as reversed, but this property will expose the correct value.</remarks>
    public Span<byte> AesCbcIv
    {
        get
        {
            Span<byte> actual = new byte[AesCbcIvSize];
            Buffer.Slice(AesCbcIvOffset, AesCbcIvSize).Span.CopyTo(actual);
            actual.Reverse();

            return actual;
        }
        set
        {
            if (value.Length != AesCbcIvSize)
            {
                throw new ArgumentException("value is the wrong size.", nameof(value));
            }

            Span<byte> actual = stackalloc byte[AesCbcIvSize];
            value.CopyTo(actual);
            actual.Reverse();

            actual.CopyTo(Buffer.Slice(AesCbcIvOffset, AesCbcIvSize).Span);
        }
    }

    /// <summary>
    /// The sel sec value.
    /// </summary>
    public SelSec SelSec
    {
        get => (SelSec)Header.SelSec;
        set => Header.SelSec = (int)value;
    }

    /// <summary>
    /// The sel T1 key value.
    /// </summary>
    public int SelT1Key
    {
        get => Header.SelT1Key;
        set => Header.SelT1Key = value;
    }

    /// <summary>
    /// The sel key value.
    /// </summary>
    public int SelKey
    {
        get => Header.SelKey;
        set => Header.SelKey = value;
    }
}