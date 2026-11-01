using LibHac.Ncm;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace LibHacToolNX.Tools.Writers.Impl;

/// <summary>
/// Describes packaged content information.
/// </summary>
public class PackagedContentInfo
{
    private Memory<byte> Buffer { get; }

    private ref PackagedContentInfoStruct Data => ref Unsafe.As<byte, PackagedContentInfoStruct>(ref Buffer.Span[0]);

    public PackagedContentInfo(Memory<byte> buffer)
    {
        Buffer = buffer;
    }

    public Span<byte> Hash
    {
        get => Data.Digest;
        set
        {
            value.CopyTo(Data.Digest);
        }
    }

    public ContentId NcaId
    {
        get => Data.Info.ContentId;
        set => Data.Info.ContentId = value;
    }

    public long Size
    {
        get => Data.Info.SizeLow | ((long)Data.Info.SizeHigh << 32);
        set
        {
            Data.Info.SizeLow = (uint)value;
            Data.Info.SizeHigh = (byte)(value >> 32);
        }
    }

    public ContentMetaType ContentType
    {
        get => Data.Info.ContentType;
        set => Data.Info.ContentType = value;
    }
}

/// <summary>
/// A mechanism used to write CNMT files.
/// </summary>
public class CnmtWriter : Writer
{
    private Memory<byte> Buffer { get; }

    private ref PackagedContentMetaHeaderStruct Header => ref Unsafe.As<byte, PackagedContentMetaHeaderStruct>(ref Buffer.Span[0]);

    public CnmtWriter(Memory<byte> buffer)
    {
        Buffer = buffer;
    }

    public int ContentEntryCount => Header.ContentCount;

    public int ContentMetaEntryCount => Header.ContentMetaCount;

    public PackagedContentInfo GetPackagedContentInfo(int index)
    {
        var size = Unsafe.SizeOf<PackagedContentInfoStruct>();

        var offset = Unsafe.SizeOf<PackagedContentMetaHeaderStruct>() + Header.ExtendedHeaderSize + (size * index);
        return new PackagedContentInfo(Buffer.Slice(offset, size));
    }

    public override async Task WriteAsync(Stream outStream, CancellationToken cancellationToken = default)
    {
        await outStream.WriteAsync(Buffer, cancellationToken);
    }
}