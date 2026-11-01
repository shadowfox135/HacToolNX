using LibHac.Tools.FsSystem.NcaUtils;

namespace LibHacToolNX.Tools.Writers.Impl;

/// <summary>
/// Describes a section within an NCA archive.
/// </summary>
public readonly struct NcaSectionDescription
{
    /// <summary>
    /// The version.
    /// </summary>
    public required short Version { get; init; }

    /// <summary>
    /// The type of section format.
    /// </summary>
    public required NcaFormatType FormatType { get; init; }

    /// <summary>
    /// The type of section hash.
    /// </summary>
    public required NcaHashType HashType { get; init; }

    /// <summary>
    /// The patch info.
    /// </summary>
    public required NcaFsPatchInfo? PatchInfo { get; init; }

    /// <summary>
    /// The checksum info.
    /// </summary>
    public required byte[]? Checksum { get; init; }

    /// <summary>
    /// The section start offset.
    /// </summary>
    public required long SectionStartOffset { get; init; }
}