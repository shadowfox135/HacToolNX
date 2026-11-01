using LibHac.Boot;
using LibHac.Common.FixedArrays;
using LibHac.Crypto;

namespace LibHac.Common.Keys;

public struct RootKeys
{
    // Mariko keys. The AES class keys are currently unused.
    public AesKey MarikoKek;
    public AesKey MarikoBek;
    public Array12<AesKey> MarikoAesClassKeys;

    // The key blobs are technically derived from the encrypted key blobs and their keys,
    // however those keys are device-unique. The decrypted key blobs are basically the common root
    // keys used by pre-6.2.0 Erista.
    public Array32<KeyBlob> KeyBlobs;

    // Used by TSEC in >= 6.2.0 Erista firmware
    public Array8<AesKey> TsecRootKeks;
    public Array8<AesKey> Package1MacKeks;
    public Array8<AesKey> Package1Keks;

    // Derived by TSEC. This is the first public root key for >= 6.2.0 Erista
    public Array8<AesKey> TsecRootKeys;

    // Used to decrypt the title keys found in an XCI's initial data
    public Array16<AesKey> GcTitleKeyKeks;
}