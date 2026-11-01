using LibHac.Common.FixedArrays;
using LibHac.Crypto;

namespace LibHac.Common.Keys;

public struct DerivedKeys
{
    public Array32<AesKey> MasterKeks;
    public Array32<AesKey> MasterKeys;
    public Array32<AesKey> Package1MacKeys;
    public Array32<AesKey> Package1Keys;
    public Array32<AesKey> Package2Keys;
    public Array32<Array3<AesKey>> KeyAreaKeys;
    public Array32<AesKey> TitleKeks;
    public AesXtsKey HeaderKey;
    public AesKey ETicketRsaKek;
    public AesKey SslRsaKek;
}