using LibHac.Fs;

namespace LibHacToolNX.Tools.Writers.Impl;

/// <summary>
/// Describes an XCI archive.
/// </summary>
public class XciDescription
{
    /// <summary>
    /// The package id.
    /// </summary>
    public required ulong PackageId { get; init; }

    /// <summary>
    /// The flags.
    /// </summary>
    public GameCardAttribute Flags { get; set; }

    /// <summary>
    /// The game card size.
    /// </summary>
    public GameCardSizeInternal GameCardSize { get; set; }

    /// <summary>
    /// The header version.
    /// </summary>
    public byte Version { get; set; }

    /// <summary>
    /// The ROM area start page.
    /// </summary>
    public int RomAreaStartPage { get; set; }

    /// <summary>
    /// The valid data end page.
    /// </summary>
    public int ValidDataEndPage { get; set; }

    /// <summary>
    /// The root partition offset.
    /// </summary>
    public long RootPartitionOffset { get; set; }

    /// <summary>
    /// The root partition header size.
    /// </summary>
    public int RootPartitionHeaderSize { get; set; }
}