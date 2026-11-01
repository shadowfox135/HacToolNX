using LibHac.Common;
using LibHac.Crypto;
using System;
using System.Linq;
using static LibHac.Tools.FsSystem.NcaUtils.NativeTypes;

namespace LibHac.Tools.FsSystem.NcaUtils;

/// <summary>
/// Describes an NCA content archive header.
/// </summary>
public class SecureNcaHeader2 : NcaHeader
{
    private static readonly byte[] Nca0FixedBodyKeySha256Hash =
    [
        0x9A, 0xBB, 0xD2, 0x11, 0x86, 0x00, 0x21, 0x9D, 0x7A, 0xDC, 0x5B, 0x43, 0x95, 0xF8, 0x4E, 0xFD,
        0xFF, 0x6B, 0x25, 0xEF, 0x9F, 0x96, 0x85, 0x28, 0x18, 0x9E, 0x76, 0xB0, 0x92, 0xF0, 0x6A, 0xCB
    ];

    /// <summary>
    /// Creates an instance of the class.
    /// </summary>
    /// <param name="data">The raw header data.</param>
    public SecureNcaHeader2(Memory<byte> data)
        : base(data) { }

    /// <summary>
    /// The signature 1 value.
    /// </summary>
    public Span<byte> Signature1
    {
        get => Buffer.Span[..SignatureSize];
        set
        {
            if (value.Length != SignatureSize)
            {
                throw new ArgumentException("The value is the wrong size.", nameof(value));
            }

            value.CopyTo(Buffer.Span[..SignatureSize]);
        }
    }

    /// <summary>
    /// The signature 2 value.
    /// </summary>
    public Span<byte> Signature2
    {
        get => Buffer.Span.Slice(SignatureSize, SignatureSize);
        set
        {
            if (value.Length != SignatureSize)
            {
                throw new ArgumentException("The value is the wrong size.", nameof(value));
            }

            value.CopyTo(Buffer.Span.Slice(SignatureSize, SignatureSize));
        }
    }

    /// <summary>
    /// The key generation.
    /// </summary>
    public byte KeyGeneration => Math.Max(KeyGeneration1, KeyGeneration2);

    /// <summary>
    /// The key 1 generation.
    /// </summary>
    public byte KeyGeneration1
    {
        get => Header.KeyGeneration1;
        set => Header.KeyGeneration1 = value;
    }

    /// <summary>
    /// The key area key index.
    /// </summary>
    public byte KeyAreaKeyIndex
    {
        get => Header.KeyAreaKeyIndex;
        set => Header.KeyAreaKeyIndex = value;
    }

    /// <summary>
    /// The key 2 generation.
    /// </summary>
    public byte KeyGeneration2
    {
        get => Header.KeyGeneration2;
        set => Header.KeyGeneration2 = value;
    }

    /// <summary>
    /// The encryption key area.
    /// </summary>
    public Span<byte> EncryptionKeyArea
    {
        get => Buffer.Span.Slice(KeyAreaOffset, KeyAreaSize);
        set
        {
            if (value.Length != KeyAreaSize)
            {
                throw new ArgumentException("The value is the wrong size.", nameof(value));
            }

            value.CopyTo(Buffer.Span.Slice(KeyAreaOffset, KeyAreaSize));
        }
    }

    /// <summary>
    /// The signature key generation.
    /// </summary>
    public byte SignatureKeyGeneration
    {
        get => Header.SignatureKeyGeneration;
        set => Header.SignatureKeyGeneration = value;
    }

    /// <summary>
    /// The format version.
    /// </summary>
    public override NcaVersion FormatVersion
    {
        get
        {
            var version = base.FormatVersion;
            if (version != NcaVersion.Unknown)
            {
                return version;
            }

            if (!EncryptionKeyArea.Slice(0x80, 0x10).IsZeros())
            {
                // The end of the key area will only be non-zero if it's RSA-OAEP encrypted
                return NcaVersion.Nca0RsaOaep;
            }

            // Key areas using fixed, unencrypted keys always use the same keys.
            // Check for these keys by comparing the key area with the known hash of the fixed body keys.
            Span<byte> hash = stackalloc byte[Sha256.DigestSize];
            Sha256.GenerateSha256Hash(EncryptionKeyArea[..Sha256.DigestSize], hash);

            if (hash.SequenceEqual(Nca0FixedBodyKeySha256Hash))
            {
                return NcaVersion.Nca0FixedKey;
            }

            // Otherwise the key area is encrypted the same as modern NCAs.
            return NcaVersion.Nca0;
        }
    }

    /// <summary>
    /// Gets the NCA FS header.
    /// </summary>
    /// <param name="index">The zero-based index of the FS header.</param>
    /// <returns>The header.</returns>
    public new SecureNcaFsHeader2 GetFsHeader(int index)
    {
        var offset = FsHeadersOffset + (index * FsHeaderSize);
        var data = Buffer.Slice(offset, FsHeaderSize);

        return new SecureNcaFsHeader2(this, data);
    }

    public Memory<byte> GetEncryptionKey(NcaKeyType keyType, int keySize)
    {
        var offset = KeyAreaOffset + ((int)keyType * Aes.KeySize128);
        return Buffer.Slice(offset, keySize);
    }
}