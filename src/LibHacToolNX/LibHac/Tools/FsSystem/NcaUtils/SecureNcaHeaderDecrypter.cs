using LibHac.Common.Keys;
using LibHac.Crypto;
using LibHac.Util;
using System;
using System.IO;
using static LibHac.Tools.FsSystem.NcaUtils.NativeTypes;

namespace LibHac.Tools.FsSystem.NcaUtils;

/// <summary>
/// A mechanism capable of decrypting the data from an NCA header.
/// </summary>
public class SecureNcaHeaderDecrypter
{
    private readonly Aes128XtsTransform2 _transform;

    /// <summary>
    /// Initializes an instance of the class.
    /// </summary>
    /// <param name="keySet">The key set.</param>
    public SecureNcaHeaderDecrypter(SecureKeySet keySet)
    {
        _transform = new Aes128XtsTransform2(keySet, true);
    }

    private bool IsDecrypted(Span<byte> bytes)
    {
        // Check the magic value
        if (bytes[0x200] != 'N' || bytes[0x201] != 'C' || bytes[0x202] != 'A')
            return false;

        // Check the version in the magic value
        if (!StringUtils.IsDigit(bytes[0x203]))
            return false;

        // Is the distribution type valid?
        if (bytes[0x204] > (int)DistributionType.GameCard)
            return false;

        // Is the content type valid?
        if (bytes[0x205] > (int)NcaContentType.PublicData)
            return false;

        return true;
    }

    public void Decrypt(Span<byte> bytes)
    {
        if (IsDecrypted(bytes))
        {
            return;
        }

        _transform.Transform(bytes[..HeaderSectorSize], 0);
        _transform.Transform(bytes.Slice(HeaderSectorSize, HeaderSectorSize), 1);

        if (!IsDecrypted(bytes))
        {
            throw new InvalidDataException("Unable to decrypt NCA header. The file is not an NCA file or the header key is incorrect.");
        }

        var version = bytes[0x203] - '0';
        if (version != 0)
        {
            switch (version)
            {
                case 3:
                    DecryptVersion3(bytes);
                    break;

                case 2:
                    DecryptVersion2(bytes);
                    break;

                default:
                    throw new NotSupportedException($"NCA version {version} is not supported.");
            }
        }
    }

    private void DecryptVersion3(Span<byte> bytes)
    {
        for (var sector = 2; sector < HeaderSize / HeaderSectorSize; sector++)
        {
            _transform.Transform(bytes.Slice(sector * HeaderSectorSize, HeaderSectorSize), (ulong)sector);
        }
    }

    private void DecryptVersion2(Span<byte> bytes)
    {
        for (var i = 0x400; i < HeaderSize; i += HeaderSectorSize)
        {
            _transform.Transform(bytes.Slice(i, HeaderSectorSize), 0);
        }
    }
}