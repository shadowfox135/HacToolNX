using LibHac.Common;
using LibHac.Crypto;
using LibHac.FsSystem;
using System;
using System.IO;
using System.Linq;

namespace LibHacToolNX.Tools.Writers.Impl;

/// <summary>
/// Describes a SHA256 partition file system entry.
/// </summary>
public class Sha256PartitionFileEntry : PartitionFileEntry
{
    /// <summary>
    /// The offset of the hash to generate.
    /// </summary>
    public int HashTargetOffset { get; init; } = 0;

    /// <summary>
    /// The size of the hash to generate.
    /// </summary>
    public int HashTargetSize { get; init; } = 512;
}

/// <summary>
/// A writer for creating SHA256 partition file systems.
/// </summary>
public class Sha256PartitionFileSystemWriter : PartitionFileSystemWriter<Sha256PartitionFileSystemFormat.PartitionEntry, Sha256PartitionFileEntry>
{
    protected override ReadOnlySpan<byte> HeaderSignature => "HFS0"u8;

    protected override uint FileDataAlignmentSize => 0x200;

    public override void AddFile(string path, FileInfo file)
    {
        ArgumentNullException.ThrowIfNull(path);

        AddFile(path, new Sha256PartitionFileEntry
        {
            File = file
        });
    }

    protected override void ProcessEntry(WriteContext context, string fileName, Sha256PartitionFileEntry description)
    {
        var name = fileName[1..].ToU8Span().Value.ToArray();
        var size = description.File.Length;

        ref var entry = ref context.GetEntry(context.CurrentIndex);
        entry.NameOffset = context.NameTable.Count;
        entry.Offset = context.Offset;
        entry.Size = size;
        entry.HashTargetOffset = description.HashTargetOffset;
        entry.HashTargetSize = description.HashTargetSize; // Had to be 1024 for the size of the partition header rather than 512.

        var hashBytes = GenerateHashForFile(description.File, entry.HashTargetOffset, entry.HashTargetSize);
        hashBytes.CopyTo(entry.Hash.Items);

        // The names are null character terminated.
        context.NameTable.AddRange(name);
        context.NameTable.Add(0);

        context.Offset += size;
    }

    private byte[] GenerateHashForFile(FileInfo file, long startOffset, int hashTargetSize)
    {
        using var dataBuffer = new RentedArray2<byte>(hashTargetSize);
        using var tempFs = file.OpenRead();

        tempFs.Seek(startOffset, SeekOrigin.Begin);
        tempFs.ReadExactly(dataBuffer.Span);

        var hashBuffer = new byte[Sha256.DigestSize];
        Sha256.GenerateSha256Hash(dataBuffer.Span, hashBuffer);

        return hashBuffer.ToArray();
    }
}