using LibHac.Common.Keys;
using LibHac.Crypto;
using LibHac.Fs;
using System;
using System.Collections.Generic;
using System.IO;
using static LibHac.Tools.FsSystem.NcaUtils.NativeTypes;

namespace LibHac.Tools.FsSystem.NcaUtils;

/// <summary>
/// Provides a mechanism to interact with secure content archive (NCA) files.
/// </summary>
public class SecureNca2 : Nca2<SecureNcaFsHeader2>
{
    /// <summary>
    /// Gets the header.
    /// </summary>
    public new SecureNcaHeader2 Header => (SecureNcaHeader2)base.Header;

    private SecureNca2(IStorage baseStorage, SecureNcaHeader2 header, Dictionary<NcaSectionType, SectionDescription> sections)
        : base(baseStorage, header, sections)
    { }

    /// <summary>
    /// Creates an <see cref="SecureNca2"/>.
    /// </summary>
    /// <param name="keySet">The <see cref="SecureKeySet"/> to use.</param>
    /// <param name="stream">The <see cref="Stream"/> to use.</param>
    /// <returns>The new <see cref="SecureNca2"/> file.</returns>
    public static SecureNca2 Create(SecureKeySet keySet, Stream stream)
    {
        var storage = StreamStorage2.Create(stream);

        try
        {
            return Create(keySet, storage);
        }
        catch (Exception)
        {
            storage.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Creates an <see cref="SecureNca2"/>.
    /// </summary>
    /// <param name="keySet">The <see cref="SecureKeySet"/> to use.</param>
    /// <param name="storage">The <see cref="IStorage"/> to use.</param>
    /// <returns>The new <see cref="SecureNca2"/> file.</returns>
    public static SecureNca2 Create(SecureKeySet keySet, IStorage storage)
    {
        storage.GetSize(out var size).ThrowIfFailure();

        if (size < HeaderSize)
        {
            throw new NotSupportedException("The stream contains less bytes than expected.");
        }

        byte[] buffer = new byte[HeaderSize];
        storage.Read(0, buffer);

        var decrypter = new SecureNcaHeaderDecrypter(keySet);
        decrypter.Decrypt(buffer);

        var header = new SecureNcaHeader2(buffer);
        var entries = ReadSections(header, i =>
        {
            var fsHeader = header.GetFsHeader(i);
            var hash = header.GetFsHeaderHash(i);

            NcaEncryptionKey? encryptionKey;
            if (fsHeader.EncryptionType == NcaEncryptionType.None)
            {
                encryptionKey = null;
            }
            else
            {
                var keyReader = new NcaEncryptionKeyReader(keySet);
                encryptionKey = keyReader.Read(fsHeader);
            }

            return new SecureSectionDescription
            {
                SectionIndex = i,
                FsHeader = fsHeader,
                SectionStartOffset = header.GetSectionStartOffset(i),
                SectionSize = header.GetSectionSize(i),
                EncryptionKey = encryptionKey,
                HashValidity = CryptoUtil.CheckSha256Hash(fsHeader.Data.Span, hash)
            };
        });

        return new SecureNca2(storage, header, entries);
    }

    /// <summary>
    /// Describes a secure section.
    /// </summary>
    public class SecureSectionDescription : SectionDescription
    {
        /// <summary>
        /// The encryption key (if available).
        /// </summary>
        public NcaEncryptionKey? EncryptionKey { get; init; }
    }

    protected override IStorage OpenRawStorage(SectionDescription description)
    {
        var baseStorage = base.OpenRawStorage(description);
        if (description is not SecureSectionDescription secureDescription)
        {
            throw new ArgumentException("The description must be a secure description.", nameof(description));
        }

        try
        {
            return description.FsHeader.EncryptionType switch
            {
                NcaEncryptionType.None => baseStorage,
                NcaEncryptionType.AesCtr => CreateAesCtrStorage(baseStorage, secureDescription),
                NcaEncryptionType.AesCtrEx => CreateAesCtrExStorage(baseStorage, secureDescription),
                _ => throw new NotSupportedException($"The encryption type '{description.FsHeader.EncryptionType}' is not supported.")
            };
        }
        catch (Exception)
        {
            baseStorage.Dispose();
            throw;
        }
    }

    private IStorage CreateAesCtrStorage(IStorage baseStorage, SecureSectionDescription description)
    {
        if (description.EncryptionKey == null)
        {
            throw new InvalidOperationException("The decryption key for the section has not been supplied on the header.");
        }

        return Aes128CtrStorage2.Create(baseStorage, new Aes128CtrStorage2.StorageDescription
        {
            Counter = description.FsHeader.Counter,
            SectionStartOffset = description.SectionStartOffset,
            EncryptionKey = description.EncryptionKey.Value
        });
    }

    private IStorage CreateAesCtrExStorage(IStorage baseStorage, SecureSectionDescription description)
    {
        if (description.EncryptionKey == null)
        {
            throw new InvalidOperationException("The decryption key for the section has not been supplied on the header.");
        }

        var patchInfo = description.FsHeader.GetPatchInfo();
        if (patchInfo is null or { EncryptionTreeSize: 0 })
        {
            throw new NotSupportedException("The section does not contain the required patch data information.");
        }

        return Aes128CtrExStorage2.CreateEx(baseStorage, new Aes128CtrExStorage2.StorageDescriptionEx
        {
            Counter = description.FsHeader.Counter,
            SectionStartOffset = description.SectionStartOffset,
            SectionEndOffset = patchInfo.EncryptionTreeOffset,
            EncryptionTreeHeader = patchInfo.GetEncryptionTreeHeader(),
            EncryptionTreeSize = patchInfo.EncryptionTreeSize,
            EncryptionKey = description.EncryptionKey.Value
        });
    }
}