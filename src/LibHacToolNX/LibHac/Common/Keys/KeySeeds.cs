using LibHac.Common.FixedArrays;
using LibHac.Crypto;

namespace LibHac.Common.Keys;

public struct KeySeeds
{
    public Array8<AesKey> TsecAuthSignatures;
    public Array32<AesKey> KeyBlobKeySources;
    public AesKey KeyBlobMacKeySource;
    public Array32<AesKey> MasterKekSources;
    public Array32<AesKey> MarikoMasterKekSources;
    public Array32<AesKey> MarikoMasterKekSourcesDev;
    public AesKey MasterKeySource;
    public AesKey Package2KeySource;
    public AesKey PerConsoleKeySource;
    public AesKey RetailSpecificAesKeySource;
    public AesKey BisKekSource;
    public Array4<AesXtsKey> BisKeySources;
    public AesKey AesKekGenerationSource;
    public AesKey AesKeyGenerationSource;
    public AesKey KeyAreaKeyApplicationSource;
    public AesKey KeyAreaKeyOceanSource;
    public AesKey KeyAreaKeySystemSource;
    public AesKey TitleKekSource;
    public AesKey HeaderKekSource;
    public AesKey SdCardKekSource;
    public Array3<AesXtsKey> SdCardKeySources;
    public AesKey DeviceUniqueSaveMacKekSource;
    public Array2<AesKey> DeviceUniqueSaveMacKeySources;
    public AesKey SeedUniqueSaveMacKekSource;
    public AesKey SeedUniqueSaveMacKeySource;
    public AesXtsKey HeaderKeySource;
}