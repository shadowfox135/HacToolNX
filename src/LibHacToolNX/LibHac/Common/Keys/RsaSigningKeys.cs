using LibHac.Common.FixedArrays;
using LibHac.Crypto;

namespace LibHac.Common.Keys;

public struct RsaSigningKeys
{
    public Array2<RsaKey> NcaHeaderSigningKeys;
    public Array2<RsaKey> AcidSigningKeys;
    public RsaKey Package2SigningKey;
}