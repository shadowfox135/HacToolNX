using System;

namespace LibHac.Tools.FsSystem.NcaUtils;

/// <summary>
/// Provides a view over a memory block containing an NCA FS header.
/// </summary>
public class SecureNcaFsHeader2 : NcaFsHeader
{
    /// <summary>
    /// Creates an instance of the class.
    /// </summary>
    /// <param name="parent">The parent header.</param>
    /// <param name="data">The memory block containing the header data.</param>
    public SecureNcaFsHeader2(SecureNcaHeader2 parent, Memory<byte> data)
        : base(parent, data) { }

    /// <summary>
    /// The parent header.
    /// </summary>
    public new SecureNcaHeader2 Parent => (SecureNcaHeader2)base.Parent;

    /// <summary>
    /// The counter.
    /// </summary>
    public ulong Counter
    {
        get => Header.UpperCounter;
        set => Header.UpperCounter = value;
    }

    /// <summary>
    /// The type of counter.
    /// </summary>
    public int CounterType
    {
        get => Header.CounterType;
        set => Header.CounterType = value;
    }

    /// <summary>
    /// The version of the counter.
    /// </summary>
    public int CounterVersion
    {
        get => Header.CounterVersion;
        set => Header.CounterVersion = value;
    }

    /// <summary>
    /// The type of encryption.
    /// </summary>
    public new NcaEncryptionType EncryptionType
    {
        get => (NcaEncryptionType)base.EncryptionType;
        set => base.EncryptionType = (byte)value;
    }
}