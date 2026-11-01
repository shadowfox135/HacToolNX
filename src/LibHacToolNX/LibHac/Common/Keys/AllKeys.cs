namespace LibHac.Common.Keys;

public struct AllKeys
{
    public TsecSecrets TsecSecrets;
    public RootKeys RootKeysDev;
    public RootKeys RootKeysProd;
    public KeySeeds KeySeeds;
    public StoredKeys StoredKeysDev;
    public StoredKeys StoredKeysProd;
    public DerivedKeys DerivedKeysDev;
    public DerivedKeys DerivedKeysProd;
    public DeviceKeys DeviceKeys;
    public DerivedDeviceKeys DerivedDeviceKeysDev;
    public DerivedDeviceKeys DerivedDeviceKeysProd;
    public RsaSigningKeys RsaSigningKeysDev;
    public RsaSigningKeys RsaSigningKeysProd;
    public RsaKeys RsaKeys;
    public DeviceRsaKeys DeviceRsaKeys;
}