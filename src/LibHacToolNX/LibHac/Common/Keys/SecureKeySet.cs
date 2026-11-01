using LibHac.Boot;
using LibHac.Common;
using LibHac.Common.FixedArrays;
using LibHac.Common.Keys;
using LibHac.Crypto;
using LibHac.Util;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Type = LibHac.Common.Keys.KeyInfo.KeyType;

namespace LibHac.Common.Keys;

/// <summary>
/// Represents a set of secure keys. 
/// </summary>
public class SecureKeySet : KeySet
{
    public enum Mode
    {
        Dev,
        Prod
    }

    /// <summary>
    /// Returns a new <see cref="SecureKeySet"/> containing any keys that have been compiled into the library.
    /// </summary>
    /// <returns>The created <see cref="SecureKeySet"/>.</returns>
    public static SecureKeySet CreateSecureKeySet()
    {
        return new SecureKeySet();
    }

    /// <summary>
    /// The number of keyblobs that were used for &lt; 6.2.0 crypto
    /// </summary>
    internal const int UsedKeyBlobCount = 6;
    internal const int SdCardKeyIdCount = 3;
    internal const int KeyRevisionCount = 0x20;
    internal const int TsecSecretCount = 0x40;
    internal const int MarikoAesClassKeyCount = 0xC;

    /// <summary>
    /// The number of slots reserved for current and future TSEC FW revisions.
    /// 8 was semi-arbitrarily chosen because there are only 3 FW revisions used for &gt;= 6.2.0 crypto as of Jan 2023,
    /// and it's unlikely that many more will be issued.
    /// </summary>
    internal const int TsecKeyRevisionCount = 8;

    private AllKeys _keys;
    private Mode _mode = Mode.Prod;

    public ref AllKeys KeyStruct => ref _keys;
    public Mode CurrentMode => _mode;

    private ref TsecSecrets Secrets => ref _keys.TsecSecrets;
    private ref RootKeys RootKeys => ref _mode == Mode.Dev ? ref _keys.RootKeysDev : ref _keys.RootKeysProd;
    private ref StoredKeys StoredKeys => ref _mode == Mode.Dev ? ref _keys.StoredKeysDev : ref _keys.StoredKeysProd;
    private ref DerivedKeys DerivedKeys => ref _mode == Mode.Dev ? ref _keys.DerivedKeysDev : ref _keys.DerivedKeysProd;
    private ref DerivedDeviceKeys DerivedDeviceKeys => ref _mode == Mode.Dev ? ref _keys.DerivedDeviceKeysDev : ref _keys.DerivedDeviceKeysProd;
    private ref RsaSigningKeys RsaSigningKeys => ref _mode == Mode.Dev ? ref _keys.RsaSigningKeysDev : ref _keys.RsaSigningKeysProd;
    private ref RsaKeys RsaKeys => ref _keys.RsaKeys;
    private ref DeviceRsaKeys DeviceRsaKeys => ref _keys.DeviceRsaKeys;

    private ref RsaSigningKeyParameters RsaSigningKeyParams => ref _mode == Mode.Dev
        ? ref _rsaSigningKeyParamsDev
        : ref _rsaSigningKeyParamsProd;

    public Span<AesKey> MarikoAesClassKeys => RootKeys.MarikoAesClassKeys.Items;
    public ref AesKey MarikoKek => ref RootKeys.MarikoKek;
    public ref AesKey MarikoBek => ref RootKeys.MarikoBek;
    public Span<KeyBlob> KeyBlobs => RootKeys.KeyBlobs.Items;
    public Span<AesKey> KeyBlobKeySources => _keys.KeySeeds.KeyBlobKeySources.Items;
    public ref AesKey KeyBlobMacKeySource => ref _keys.KeySeeds.KeyBlobMacKeySource;

    public Span<AesKey> TsecSecrets => Secrets.Secrets.Items;
    public Span<AesKey> TsecRootKeks => RootKeys.TsecRootKeks.Items;
    public Span<AesKey> Package1MacKeks => RootKeys.Package1MacKeks.Items;
    public Span<AesKey> Package1Keks => RootKeys.Package1Keks.Items;
    public Span<AesKey> TsecAuthSignatures => _keys.KeySeeds.TsecAuthSignatures.Items;
    public Span<AesKey> TsecRootKeys => RootKeys.TsecRootKeys.Items;
    public Span<AesKey> MasterKekSources => _keys.KeySeeds.MasterKekSources.Items;
    public Span<AesKey> GcTitleKeyKeks => RootKeys.GcTitleKeyKeks.Items;

    public Span<AesKey> MarikoMasterKekSources => _mode == Mode.Dev
        ? _keys.KeySeeds.MarikoMasterKekSourcesDev.Items
        : _keys.KeySeeds.MarikoMasterKekSources.Items;

    public Span<AesKey> MasterKeks => DerivedKeys.MasterKeks.Items;
    public ref AesKey MasterKeySource => ref _keys.KeySeeds.MasterKeySource;
    public Span<AesKey> MasterKeys => DerivedKeys.MasterKeys.Items;
    public Span<AesKey> Package1MacKeys => DerivedKeys.Package1MacKeys.Items;
    public Span<AesKey> Package1Keys => DerivedKeys.Package1Keys.Items;
    public Span<AesKey> Package2Keys => DerivedKeys.Package2Keys.Items;
    public ref AesKey Package2KeySource => ref _keys.KeySeeds.Package2KeySource;
    public ref AesKey PerConsoleKeySource => ref _keys.KeySeeds.PerConsoleKeySource;
    public ref AesKey RetailSpecificAesKeySource => ref _keys.KeySeeds.RetailSpecificAesKeySource;
    public ref AesKey BisKekSource => ref _keys.KeySeeds.BisKekSource;
    public Span<AesXtsKey> BisKeySources => _keys.KeySeeds.BisKeySources.Items;
    public ref AesKey AesKekGenerationSource => ref _keys.KeySeeds.AesKekGenerationSource;
    public ref AesKey AesKeyGenerationSource => ref _keys.KeySeeds.AesKeyGenerationSource;
    public ref AesKey KeyAreaKeyApplicationSource => ref _keys.KeySeeds.KeyAreaKeyApplicationSource;
    public ref AesKey KeyAreaKeyOceanSource => ref _keys.KeySeeds.KeyAreaKeyOceanSource;
    public ref AesKey KeyAreaKeySystemSource => ref _keys.KeySeeds.KeyAreaKeySystemSource;
    public ref AesKey TitleKekSource => ref _keys.KeySeeds.TitleKekSource;
    public ref AesKey HeaderKekSource => ref _keys.KeySeeds.HeaderKekSource;
    public ref AesKey SdCardKekSource => ref _keys.KeySeeds.SdCardKekSource;
    public Span<AesXtsKey> SdCardKeySources => _keys.KeySeeds.SdCardKeySources.Items;
    public ref AesKey DeviceUniqueSaveMacKekSource => ref _keys.KeySeeds.DeviceUniqueSaveMacKekSource;
    public Span<AesKey> DeviceUniqueSaveMacKeySources => _keys.KeySeeds.DeviceUniqueSaveMacKeySources.Items;
    public ref AesKey SeedUniqueSaveMacKekSource => ref _keys.KeySeeds.SeedUniqueSaveMacKekSource;
    public ref AesKey SeedUniqueSaveMacKeySource => ref _keys.KeySeeds.SeedUniqueSaveMacKeySource;
    public ref AesXtsKey HeaderKeySource => ref _keys.KeySeeds.HeaderKeySource;
    public ref AesXtsKey HeaderKey => ref DerivedKeys.HeaderKey;
    public Span<AesKey> TitleKeks => DerivedKeys.TitleKeks.Items;
    public Span<Array3<AesKey>> KeyAreaKeys => DerivedKeys.KeyAreaKeys.Items;
    public ref AesKey XciHeaderKey => ref StoredKeys.XciHeaderKey;
    public ref AesKey ETicketRsaKek => ref DerivedKeys.ETicketRsaKek;
    public ref AesKey SslRsaKek => ref DerivedKeys.SslRsaKek;

    public ref AesKey SecureBootKey => ref _keys.DeviceKeys.SecureBootKey;
    public ref AesKey TsecKey => ref _keys.DeviceKeys.TsecKey;
    public ref AesKey SdCardEncryptionSeed => ref _keys.DeviceKeys.SdCardEncryptionSeed;
    public Span<EncryptedKeyBlob> EncryptedKeyBlobs => _keys.DeviceKeys.EncryptedKeyBlobs.Items;

    public Span<AesKey> KeyBlobKeys => DerivedDeviceKeys.KeyBlobKeys.Items;
    public Span<AesKey> KeyBlobMacKeys => DerivedDeviceKeys.KeyBlobMacKeys.Items;
    public ref AesKey DeviceKey => ref DerivedDeviceKeys.DeviceKey;
    public Span<AesXtsKey> BisKeys => DerivedDeviceKeys.BisKeys.Items;
    public Span<AesKey> DeviceUniqueSaveMacKeys => DerivedDeviceKeys.DeviceUniqueSaveMacKeys.Items;
    public ref AesKey SeedUniqueSaveMacKey => ref DerivedDeviceKeys.SeedUniqueSaveMacKey;

    // Todo: Make a separate type? Not actually an AES-XTS key, but it's still the same shape.
    public Span<AesXtsKey> SdCardEncryptionKeys => DerivedDeviceKeys.SdCardEncryptionKeys.Items;

    public Span<RsaKey> NcaHeaderSigningKeys => RsaSigningKeys.NcaHeaderSigningKeys.Items;
    public Span<RsaKey> AcidSigningKeys => RsaSigningKeys.AcidSigningKeys.Items;
    public ref RsaKey Package2SigningKey => ref RsaSigningKeys.Package2SigningKey;
    public ref RsaFullKey BetaNca0KeyAreaKey => ref RsaKeys.BetaNca0KeyAreaKey;
    public ref RsaKeyPair ETicketRsaKey => ref DeviceRsaKeys.ETicketRsaKey;

    private RsaSigningKeyParameters _rsaSigningKeyParamsDev;
    private RsaSigningKeyParameters _rsaSigningKeyParamsProd;
    private RsaKeyParameters _rsaKeyParams;


    public Span<RSAParameters> NcaHeaderSigningKeyParams
    {
        get
        {
            ref Optional<Array2<RSAParameters>> keys = ref RsaSigningKeyParams.NcaHeaderSigningKeys;

            if (!keys.HasValue)
            {
                keys.Set(new Array2<RSAParameters>());
                keys.Value[0] = CreateRsaParameters(in NcaHeaderSigningKeys[0]);
                keys.Value[1] = CreateRsaParameters(in NcaHeaderSigningKeys[1]);
            }

            // Todo: Remove local variable after Roslyn issue #67697 is fixed
            ref Array2<RSAParameters> array = ref keys.Value;
            return array.Items;
        }
    }

    public Span<RSAParameters> AcidSigningKeyParams
    {
        get
        {
            ref Optional<Array2<RSAParameters>> keys = ref RsaSigningKeyParams.AcidSigningKeys;

            if (!keys.HasValue)
            {
                keys.Set(new Array2<RSAParameters>());
                keys.Value[0] = CreateRsaParameters(in AcidSigningKeys[0]);
                keys.Value[1] = CreateRsaParameters(in AcidSigningKeys[1]);
            }

            // Todo: Remove local variable after Roslyn issue #67697 is fixed
            ref Array2<RSAParameters> array = ref keys.Value;
            return array.Items;
        }
    }

    public ref RSAParameters Package2SigningKeyParams
    {
        get
        {
            ref Optional<RSAParameters> keys = ref RsaSigningKeyParams.Package2SigningKey;

            if (!keys.HasValue)
            {
                keys.Set(new RSAParameters());
                keys.Value = CreateRsaParameters(in Package2SigningKey);
            }

            return ref keys.Value;
        }
    }

    public ref RSAParameters BetaNca0KeyAreaKeyParams
    {
        get
        {
            ref Optional<RSAParameters> keys = ref _rsaKeyParams.BetaNca0KeyAreaKey;

            if (!keys.HasValue)
            {
                keys.Set(CreateRsaParameters(in BetaNca0KeyAreaKey));
            }

            return ref keys.Value;
        }
    }

    public ref RSAParameters ETicketRsaKeyParams
    {
        get
        {
            ref Optional<RSAParameters> keys = ref _rsaKeyParams.ETicketRsaKey;

            if (!keys.HasValue && !ETicketRsaKey.PublicExponent.ItemsRo.IsZeros())
            {
                RSAParameters rsaParams = Rsa.RecoverParameters(ETicketRsaKey.Modulus, ETicketRsaKey.PublicExponent, ETicketRsaKey.PrivateExponent);
                keys.Set(rsaParams);
            }

            return ref keys.Value;
        }
    }

    public void SetSdSeed(ReadOnlySpan<byte> sdSeed)
    {
        if (sdSeed.Length != 0x10)
            throw new ArgumentException("Sd card encryption seed must be 16 bytes long.");

        sdSeed.CopyTo(SdCardEncryptionSeed);
        DeriveSdCardKeys();
    }

    public void SetMode(Mode mode) => _mode = mode;

    public void DeriveKeys(IProgressReport logger = null)
    {
        Mode originalMode = CurrentMode;

        SetMode(Mode.Prod);
        KeyDerivation.DeriveAllKeys(this, logger);

        SetMode(Mode.Dev);
        KeyDerivation.DeriveAllKeys(this, logger);

        SetMode(originalMode);
    }

    public void DeriveSdCardKeys() => KeyDerivation.DeriveSdCardKeys(this);

    private static RSAParameters CreateRsaParameters(in RsaKey key)
    {
        return new RSAParameters
        {
            Exponent = key.PublicExponent.ItemsRo.ToArray(),
            Modulus = key.Modulus.ItemsRo.ToArray()
        };
    }

    private static RSAParameters CreateRsaParameters(in RsaFullKey key)
    {
        return new RSAParameters
        {
            D = key.PrivateExponent.ItemsRo.ToArray(),
            DP = key.Dp.ItemsRo.ToArray(),
            DQ = key.Dq.ItemsRo.ToArray(),
            Exponent = key.PublicExponent.ItemsRo.ToArray(),
            InverseQ = key.InverseQ.ItemsRo.ToArray(),
            Modulus = key.Modulus.ItemsRo.ToArray(),
            P = key.P.ItemsRo.ToArray(),
            Q = key.Q.ItemsRo.ToArray()
        };
    }

    private struct RsaSigningKeyParameters
    {
        public Optional<Array2<RSAParameters>> NcaHeaderSigningKeys;
        public Optional<Array2<RSAParameters>> AcidSigningKeys;
        public Optional<RSAParameters> Package2SigningKey;
    }

    private struct RsaKeyParameters
    {
        public Optional<RSAParameters> BetaNca0KeyAreaKey;
        public Optional<RSAParameters> ETicketRsaKey;
    }

    /// <summary>
    /// Creates a <see cref="List{T}"/> of the <see cref="KeyInfo"/> of all keys that are loadable by default.
    /// </summary>
    /// <returns>The created list.</returns>
    public static List<KeyInfo> CreateKeyList()
    {
        // Update this value if more keys are added
        var keys = new List<KeyInfo>(70);

        // Keys with a group value of -1 are keys that will be read but not written.
        // This is for compatibility since some keys had other names in the past.

        // TSEC secrets aren't public yet, so the TSEC root keys will be treated as
        // root keys even though they're derived.

        keys.Add(new KeyInfo(10, Type.DeviceRoot, "secure_boot_key", (set, _) => set.SecureBootKey));
        keys.Add(new KeyInfo(11, Type.DeviceRoot, "tsec_key", (set, _) => set.TsecKey));
        keys.Add(new KeyInfo(12, Type.DeviceDrvd, "device_key", (set, _) => set.DeviceKey));

        keys.Add(new KeyInfo(15, Type.CommonRootSame, "tsec_secret", 0, TsecSecretCount, (set, i) => set.TsecSecrets[i]));

        keys.Add(new KeyInfo(20, Type.CommonRoot, "tsec_root_kek", 0, TsecKeyRevisionCount, (set, i) => set.TsecRootKeks[i]));
        keys.Add(new KeyInfo(22, Type.CommonRoot, "package1_mac_kek", 0, TsecKeyRevisionCount, (set, i) => set.Package1MacKeks[i]));
        keys.Add(new KeyInfo(24, Type.CommonRoot, "package1_kek", 0, TsecKeyRevisionCount, (set, i) => set.Package1Keks[i]));

        keys.Add(new KeyInfo(30, Type.CommonSeed, "tsec_auth_signature", 0, TsecKeyRevisionCount, (set, i) => set.TsecAuthSignatures[i]));

        keys.Add(new KeyInfo(40, Type.CommonRoot, "tsec_root_key", 0, TsecKeyRevisionCount, (set, i) => set.TsecRootKeys[i]));

        keys.Add(new KeyInfo(50, Type.CommonSeed, "keyblob_mac_key_source", (set, _) => set.KeyBlobMacKeySource));
        keys.Add(new KeyInfo(51, Type.CommonSeed, "keyblob_key_source", 0, UsedKeyBlobCount, (set, i) => set.KeyBlobKeySources[i]));

        keys.Add(new KeyInfo(55, Type.DeviceDrvd, "keyblob_key", 0, UsedKeyBlobCount, (set, i) => set.KeyBlobKeys[i]));

        keys.Add(new KeyInfo(60, Type.DeviceDrvd, "keyblob_mac_key", 0, UsedKeyBlobCount, (set, i) => set.KeyBlobMacKeys[i]));

        keys.Add(new KeyInfo(70, Type.DeviceRoot, "encrypted_keyblob", 0, 6, (set, i) =>
        {
            // Todo: Remove local variable after Roslyn issue #67697 is fixed
            ref EncryptedKeyBlob keySetKeyBlob = ref set.EncryptedKeyBlobs[i];
            return keySetKeyBlob.Bytes;
        }));

        keys.Add(new KeyInfo(80, Type.CommonRoot, "keyblob", 0, 6, (set, i) =>
        {
            // Todo: Remove local variable after Roslyn issue #67697 is fixed
            ref KeyBlob keySetKeyBlob = ref set.KeyBlobs[i];
            return keySetKeyBlob.Bytes;
        }));

        keys.Add(new KeyInfo(90, Type.CommonSeed, "master_kek_source", UsedKeyBlobCount, KeyRevisionCount, (set, i) => set.MasterKekSources[i]));

        keys.Add(new KeyInfo(100, Type.CommonRoot, "mariko_bek", (set, _) => set.MarikoBek));
        keys.Add(new KeyInfo(101, Type.CommonRoot, "mariko_kek", (set, _) => set.MarikoKek));

        keys.Add(new KeyInfo(110, Type.CommonRoot, "mariko_aes_class_key", 0, MarikoAesClassKeyCount, (set, i) => set.MarikoAesClassKeys[i]));
        keys.Add(new KeyInfo(120, Type.CommonSeedDiff, "mariko_master_kek_source", 0, KeyRevisionCount, (set, i) => set.MarikoMasterKekSources[i]));
        keys.Add(new KeyInfo(130, Type.CommonDrvd, "master_kek", 0, KeyRevisionCount, (set, i) => set.MasterKeks[i]));
        keys.Add(new KeyInfo(140, Type.CommonSeed, "master_key_source", (set, _) => set.MasterKeySource));
        keys.Add(new KeyInfo(150, Type.CommonDrvd, "master_key", 0, KeyRevisionCount, (set, i) => set.MasterKeys[i]));

        keys.Add(new KeyInfo(160, Type.CommonDrvd, "package1_key", 0, KeyRevisionCount, (set, i) => set.Package1Keys[i]));
        keys.Add(new KeyInfo(170, Type.CommonDrvd, "package1_mac_key", UsedKeyBlobCount, KeyRevisionCount, (set, i) => set.Package1MacKeys[i]));
        keys.Add(new KeyInfo(180, Type.CommonSeed, "package2_key_source", (set, _) => set.Package2KeySource));
        keys.Add(new KeyInfo(190, Type.CommonDrvd, "package2_key", 0, KeyRevisionCount, (set, i) => set.Package2Keys[i]));

        keys.Add(new KeyInfo(200, Type.CommonSeed, "bis_kek_source", (set, _) => set.BisKekSource));
        keys.Add(new KeyInfo(201, Type.CommonSeed, "bis_key_source", 0, 4, (set, i) => set.BisKeySources[i]));

        keys.Add(new KeyInfo(205, Type.DeviceDrvd, "bis_key", 0, 4, (set, i) => set.BisKeys[i]));

        keys.Add(new KeyInfo(210, Type.CommonSeed, "per_console_key_source", (set, _) => set.PerConsoleKeySource));
        keys.Add(new KeyInfo(211, Type.CommonSeed, "retail_specific_aes_key_source", (set, _) => set.RetailSpecificAesKeySource));
        keys.Add(new KeyInfo(212, Type.CommonSeed, "aes_kek_generation_source", (set, _) => set.AesKekGenerationSource));
        keys.Add(new KeyInfo(213, Type.CommonSeed, "aes_key_generation_source", (set, _) => set.AesKeyGenerationSource));
        keys.Add(new KeyInfo(214, Type.CommonSeed, "titlekek_source", (set, _) => set.TitleKekSource));

        keys.Add(new KeyInfo(220, Type.CommonDrvd, "titlekek", 0, KeyRevisionCount, (set, i) => set.TitleKeks[i]));

        keys.Add(new KeyInfo(230, Type.CommonSeed, "header_kek_source", (set, _) => set.HeaderKekSource));
        keys.Add(new KeyInfo(231, Type.CommonSeed, "header_key_source", (set, _) => set.HeaderKeySource));
        keys.Add(new KeyInfo(232, Type.CommonDrvd, "header_key", (set, _) => set.HeaderKey));

        keys.Add(new KeyInfo(240, Type.CommonSeed, "key_area_key_application_source", (set, _) => set.KeyAreaKeyApplicationSource));
        keys.Add(new KeyInfo(241, Type.CommonSeed, "key_area_key_ocean_source", (set, _) => set.KeyAreaKeyOceanSource));
        keys.Add(new KeyInfo(242, Type.CommonSeed, "key_area_key_system_source", (set, _) => set.KeyAreaKeySystemSource));

        keys.Add(new KeyInfo(250, Type.CommonSeed, "save_mac_kek_source", (set, _) => set.DeviceUniqueSaveMacKekSource));
        keys.Add(new KeyInfo(251, Type.CommonSeed, "save_mac_key_source", 0, 2, (set, i) => set.DeviceUniqueSaveMacKeySources[i]));
        keys.Add(new KeyInfo(252, Type.DeviceDrvd, "save_mac_key", 0, 2, (set, i) => set.DeviceUniqueSaveMacKeys[i]));
        keys.Add(new KeyInfo(-01, Type.CommonSeed, "save_mac_key_source", (set, _) => set.DeviceUniqueSaveMacKeySources[0]));

        keys.Add(new KeyInfo(253, Type.CommonSeed, "save_mac_sd_card_kek_source", (set, _) => set.SeedUniqueSaveMacKekSource));
        keys.Add(new KeyInfo(254, Type.CommonSeed, "save_mac_sd_card_key_source", (set, _) => set.SeedUniqueSaveMacKeySource));
        keys.Add(new KeyInfo(255, Type.DeviceDrvd, "save_mac_sd_card_key", (set, _) => set.SeedUniqueSaveMacKey));

        keys.Add(new KeyInfo(260, Type.DeviceRoot, "sd_seed", (set, _) => set.SdCardEncryptionSeed));

        keys.Add(new KeyInfo(261, Type.CommonSeed, "sd_card_kek_source", (set, _) => set.SdCardKekSource));
        keys.Add(new KeyInfo(262, Type.CommonSeed, "sd_card_save_key_source", (set, _) => set.SdCardKeySources[0]));
        keys.Add(new KeyInfo(263, Type.CommonSeed, "sd_card_nca_key_source", (set, _) => set.SdCardKeySources[1]));
        keys.Add(new KeyInfo(264, Type.CommonSeed, "sd_card_custom_storage_key_source", (set, _) => set.SdCardKeySources[2]));

        keys.Add(new KeyInfo(270, Type.CommonRoot, "xci_header_key", (set, _) => set.XciHeaderKey));
        keys.Add(new KeyInfo(271, Type.CommonRoot, "xci_t1_titlekey_kek", 0, 0x10, (set, i) => set.GcTitleKeyKeks[i]));

        keys.Add(new KeyInfo(280, Type.CommonRoot, "eticket_rsa_kek", (set, _) => set.ETicketRsaKek));
        keys.Add(new KeyInfo(281, Type.CommonRoot, "ssl_rsa_kek", (set, _) => set.SslRsaKek));
        keys.Add(new KeyInfo(282, Type.DeviceDrvd, "eticket_rsa_keypair", (set, _) => SpanHelpers.AsByteSpan(ref set.ETicketRsaKey)));

        keys.Add(new KeyInfo(290, Type.CommonDrvd, "key_area_key_application", 0, KeyRevisionCount, (set, i) => set.KeyAreaKeys[i][0]));
        keys.Add(new KeyInfo(300, Type.CommonDrvd, "key_area_key_ocean", 0, KeyRevisionCount, (set, i) => set.KeyAreaKeys[i][1]));
        keys.Add(new KeyInfo(310, Type.CommonDrvd, "key_area_key_system", 0, KeyRevisionCount, (set, i) => set.KeyAreaKeys[i][2]));

        return keys;
    }
}