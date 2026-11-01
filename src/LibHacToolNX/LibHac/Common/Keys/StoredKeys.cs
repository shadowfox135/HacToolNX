using LibHac.Crypto;

namespace LibHac.Common.Keys;

/// <summary>
/// Holds keys that are stored directly in Horizon programs.
/// </summary>
public struct StoredKeys
{
    public AesKey XciHeaderKey;
}