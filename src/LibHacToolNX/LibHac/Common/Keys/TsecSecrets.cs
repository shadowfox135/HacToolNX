using LibHac.Common.FixedArrays;
using LibHac.Crypto;

namespace LibHac.Common.Keys;

public struct TsecSecrets
{
    public Array64<AesKey> Secrets;
}