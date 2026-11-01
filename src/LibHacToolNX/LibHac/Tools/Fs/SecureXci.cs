using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.FsSystem;
using LibHac.Tools.FsSystem;
using System;
using System.IO;
using System.Linq;
using static LibHac.Tools.Fs.NativeTypes;

namespace LibHac.Tools.Fs;

/// <summary>
/// Provides a mechanism to interact with secure executable cartridge image (XCI) files.
/// </summary>
public class SecureXci2 : Xci2
{
    /// <summary>
    /// Gets the header.
    /// </summary>
    public new SecureXciHeader2 Header => (SecureXciHeader2)base.Header;

    /// <summary>
    /// Creates an instance of the class.
    /// </summary>
    /// <param name="baseStorage">The storage to use.</param>
    /// <param name="header">The header.</param>
    /// <param name="rootFileSystem">The root file system.</param>
    protected SecureXci2(IStorage baseStorage, SecureXciHeader2 header, IFileSystem rootFileSystem)
        : base(baseStorage, header, rootFileSystem) { }

    /// <summary>
    /// Creates an <see cref="Xci2"/>.
    /// </summary>
    /// <param name="stream">The <see cref="Stream"/> to use.</param>
    /// <returns>The new <see cref="Xci2"/> file.</returns>
    public new static SecureXci2 Create(Stream stream)
    {
        var storage = StreamStorage2.Create(stream);

        try
        {
            return Create(storage);
        }
        catch (Exception)
        {
            storage.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Creates an <see cref="Xci2"/>.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> to use.</param>
    /// <returns>The new <see cref="Xci2"/> file.</returns>
    public new static SecureXci2 Create(IStorage storage)
    {
        var header = SecureXciHeader2.Create(storage);
        if (!header.Magic.Span.SequenceEqual(HeaderSignature))
        {
            throw new ArgumentException("The storage does not contain the expected header.", nameof(storage));
        }

        storage.GetSize(out var storageSize).ThrowIfFailure();

        try
        {
            var rootFs = Sha256PartitionFileSystem2.Create(
                storage.Slice2(header.RootPartitionOffset, storageSize - header.RootPartitionOffset),
                IntegrityCheckLevel.ErrorOnInvalid);

            return new SecureXci2(storage, header, rootFs);
        }
        catch (Exception)
        {
            storage.Dispose();
            throw;
        }
    }
}