using LibHac.Common;
using LibHac.FsSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace LibHacToolNX.Tools.Writers.Impl;

/// <summary>
/// A writer for creating partition file systems.
/// </summary>
public class PartitionFileSystemWriter : PartitionFileSystemWriter<PartitionFileSystemFormat.PartitionEntry, PartitionFileEntry>
{
    protected override ReadOnlySpan<byte> HeaderSignature => "PFS0"u8;

    protected override uint FileDataAlignmentSize => 0x20;

    public override void AddFile(string path, FileInfo file)
    {
        ArgumentNullException.ThrowIfNull(path);

        AddFile(path, new PartitionFileEntry
        {
            File = file
        });
    }

    protected override void ProcessEntry(WriteContext context, string fileName, PartitionFileEntry description)
    {
        var name = fileName[1..].ToU8Span().Value.ToArray();
        var size = description.File.Length;

        ref var entry = ref context.GetEntry(context.CurrentIndex);
        entry.NameOffset = context.NameTable.Count;
        entry.Offset = context.Offset;
        entry.Size = size;

        // The names are null character terminated.
        context.NameTable.AddRange(name);
        context.NameTable.Add(0);

        context.Offset += size;
    }
}

/// <summary>
/// Describes a partition file entry.
/// </summary>
public class PartitionFileEntry
{
    /// <summary>
    /// The file containing the contents.
    /// </summary>
    public required FileInfo File { get; init; }
}

/// <summary>
/// A base writer for creating partition file systems.
/// </summary>
/// <typeparam name="TEntry">The type of entry.</typeparam>
public abstract class PartitionFileSystemWriter<TEntry, TFileEntry> : Writer
    where TEntry : unmanaged
    where TFileEntry : PartitionFileEntry
{
    protected Dictionary<string, TFileEntry> Files { get; } = new();

    /// <summary>
    /// The header signature.
    /// </summary>
    protected abstract ReadOnlySpan<byte> HeaderSignature { get; }

    /// <summary>
    /// The file data alignment size.
    /// </summary>
    protected abstract uint FileDataAlignmentSize { get; }

    /// <summary>
    /// Adds a file.
    /// </summary>
    /// <param name="path">The target path of the file.</param>
    /// <param name="file">The file containing the contents.</param>
    public abstract void AddFile(string path, FileInfo file);

    /// <summary>
    /// Adds a file.
    /// </summary>
    /// <param name="path">The target path of the file.</param>
    /// <param name="entry">The partition file entry details.</param>
    public void AddFile(string path, TFileEntry entry)
    {
        ArgumentNullException.ThrowIfNull(path);

        Files.Add(path, entry);
    }

    /// <summary>
    /// Processes the entry.
    /// </summary>
    /// <param name="context">The context providing contextual information for the entry being written.</param>
    /// <param name="fileName">The name of the file.</param>
    /// <param name="description">The partition file entry description.</param>
    protected abstract void ProcessEntry(WriteContext context, string fileName, TFileEntry description);

    public int OutputHeaderSize { get; private set; }

    public override async Task WriteAsync(Stream outStream, CancellationToken cancellationToken = default)
    {
        var files = Files.ToArray();

        var entrySize = Unsafe.SizeOf<TEntry>();
        var headerSize = Unsafe.SizeOf<PartitionFileSystemFormat.PartitionFileSystemHeaderImpl>();

        var context = new WriteContext
        {
            HeaderBytes = new byte[headerSize],
            EntryBytes = new byte[files.Length * entrySize]
        };

        HeaderSignature.CopyTo(context.HeaderBytes);
        context.Header.EntryCount = files.Length;

        for (context.CurrentIndex = 0; context.CurrentIndex < files.Length; context.CurrentIndex++)
        {
            var file = files[context.CurrentIndex];
            ProcessEntry(context, file.Key, file.Value);
        }

        int totalSize = headerSize + (context.Header.EntryCount * entrySize) + context.NameTable.Count;
        int padding = (int)(FileDataAlignmentSize - (totalSize % FileDataAlignmentSize));
        OutputHeaderSize = totalSize + padding;

        context.Header.NameTableSize = context.NameTable.Count + padding;

        await outStream.WriteAsync(context.HeaderBytes, cancellationToken);
        await outStream.WriteAsync(context.EntryBytes, cancellationToken);

        // Write the name table.
        await outStream.WriteAsync(context.NameTable.ToArray(), cancellationToken);
        if (padding > 0)
        {
            await outStream.WriteAsync(new byte[padding], cancellationToken);
        }

        foreach (var file in files)
        {
            await using var fs = file.Value.File.OpenRead();
            await fs.CopyToAsync(outStream, cancellationToken);
        }

        await outStream.FlushAsync(cancellationToken);
    }

    /// <summary>
    /// Provides contextual information during the write process.
    /// </summary>
    protected sealed class WriteContext
    {
        /// <summary>
        /// The raw header.
        /// </summary>
        public required byte[] HeaderBytes { get; set; }

        /// <summary>
        /// The header.
        /// </summary>
        public ref PartitionFileSystemFormat.PartitionFileSystemHeaderImpl Header =>
            ref MemoryMarshal.Cast<byte, PartitionFileSystemFormat.PartitionFileSystemHeaderImpl>(HeaderBytes.AsSpan())[0];

        /// <summary>
        /// The raw entries.
        /// </summary>
        public required byte[] EntryBytes { get; set; }

        /// <summary>
        /// The zero-based index of the current entry being processed.
        /// </summary>
        public int CurrentIndex { get; set; }

        /// <summary>
        /// The name table.
        /// </summary>
        public List<byte> NameTable { get; } = [];

        /// <summary>
        /// The current offset within the file.
        /// </summary>
        public long Offset { get; set; }

        public ref TEntry GetEntry(int index)
        {
            return ref MemoryMarshal.Cast<byte, TEntry>(EntryBytes.AsSpan())[index];
        }
    }
}