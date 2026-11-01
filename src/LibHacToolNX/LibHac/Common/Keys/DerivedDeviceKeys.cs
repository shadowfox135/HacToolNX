using LibHac.Common.FixedArrays;
using LibHac.Crypto;

namespace LibHac.Common.Keys;

public struct DerivedDeviceKeys
{
    public Array32<AesKey> KeyBlobKeys;
    public Array32<AesKey> KeyBlobMacKeys;
    public AesKey DeviceKey;
    public Array4<AesXtsKey> BisKeys;
    public Array2<AesKey> DeviceUniqueSaveMacKeys;
    public AesKey SeedUniqueSaveMacKey;
    public Array3<AesXtsKey> SdCardEncryptionKeys;
}