using System;

namespace LibHac.Tools.FsSystem.NcaUtils;

/// <summary>
/// Describes an encryption key.
/// </summary>
public readonly struct NcaEncryptionKey
{
    /// <summary>
    /// The key.
    /// </summary>
    public required Memory<byte> Key { get; init; }

    /// <summary>
    /// The type of key.
    /// </summary>
    public required NcaKeyType KeyType { get; init; }
}