using LibHac.Boot;
using LibHac.Common.FixedArrays;
using LibHac.Crypto;

namespace LibHac.Common.Keys;

public struct DeviceKeys
{
    public AesKey SecureBootKey;
    public AesKey TsecKey;
    public AesKey SdCardEncryptionSeed;
    public Array32<EncryptedKeyBlob> EncryptedKeyBlobs;
}