using LibHac.Common;
using LibHac.Common.Keys;
using LibHac.Crypto;
using LibHac.Fs;
using System;

namespace LibHac.Tools.FsSystem.NcaUtils;

/// <summary>
/// A mechanism to read an NCA archive encryption key.
/// </summary>
/// <param name="keySet">The keyset used to access the key.</param>
public class NcaEncryptionKeyReader(SecureKeySet keySet)
{
    private static readonly string[] _kakNames = ["application", "ocean", "system"];

    /// <summary>
    /// Reads the key.
    /// </summary>
    /// <param name="fsHeader">The <see cref="SecureNcaFsHeader2"/> which contains the key.</param>
    /// <returns>The <see cref="NcaEncryptionKey"/>.</returns>
    /// <exception cref="ArgumentException">The header is not encrypted.</exception>
    public NcaEncryptionKey Read(SecureNcaFsHeader2 fsHeader)
    {
        if (fsHeader.EncryptionType == NcaEncryptionType.None)
        {
            throw new ArgumentException("The header does not use encryption.", nameof(fsHeader));
        }

        var keyType = GetKeyTypeForEncryptionType(fsHeader.EncryptionType);

        byte[] key;
        if (fsHeader.Parent.HasRightsId)
        {
            key = GetDecryptedTitleKey(fsHeader);
        }
        else
        {
            key = GetDecryptedKey(fsHeader, keyType);
        }

        return new NcaEncryptionKey
        {
            Key = key,
            KeyType = keyType
        };
    }

    private byte[] GetDecryptedTitleKey(SecureNcaFsHeader2 fsHeader)
    {
        var rightsId = new RightsId(fsHeader.Parent.RightsId);
        if (keySet.ExternalKeySet.Get(rightsId, out var accessKey).IsFailure())
        {
            throw new MissingKeyException("Missing NCA title key.", rightsId.ToString(), KeyType.Title);
        }

        int keyRevision = Utilities.GetMasterKeyRevision(fsHeader.Parent.KeyGeneration);

        byte[] titleKek = keySet.TitleKeks[keyRevision].DataRo.ToArray();
        if (titleKek.IsZeros())
        {
            throw new MissingKeyException("Unable to decrypt title key.", $"titlekek_{keyRevision:x2}", KeyType.Common);
        }

        byte[] decryptedKey = new byte[Aes.KeySize128];
        Aes.DecryptEcb128(accessKey.Value, decryptedKey, titleKek);

        return decryptedKey;
    }

    private byte[] GetDecryptedKey(SecureNcaFsHeader2 fsHeader, NcaKeyType keyType)
    {
        var keyRevision = Utilities.GetMasterKeyRevision(fsHeader.Parent.KeyGeneration);

        var keyAreaKey = keySet.KeyAreaKeys[keyRevision][fsHeader.Parent.KeyAreaKeyIndex].Data;
        if (keyAreaKey.IsZeros())
        {
            throw new MissingKeyException("Unable to decrypt the NCA section.",
                $"key_area_key_{_kakNames[fsHeader.Parent.KeyAreaKeyIndex]}_{keyRevision:x2}", KeyType.Common);
        }

        var encryptionKey = fsHeader.Parent.GetEncryptionKey(keyType, Aes128EcbTransform.BlockSizeBytes).ToArray();

        var transform = new Aes128EcbTransform(keyAreaKey.ToArray(), false);
        transform.Transform(encryptionKey);

        return encryptionKey;
    }

    private static NcaKeyType GetKeyTypeForEncryptionType(NcaEncryptionType encryptionType)
    {
        // These appear to be based on the kind of storage being constructed within the old SecureNca file.
        return encryptionType switch
        {
            NcaEncryptionType.AesCtr => NcaKeyType.AesCtr,
            NcaEncryptionType.AesCtrEx => NcaKeyType.AesCtrEx,
            _ => throw new NotSupportedException($"The encryption type '{encryptionType}' is not supported.")
        };
    }
}