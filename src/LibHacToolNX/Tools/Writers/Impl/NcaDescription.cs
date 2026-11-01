using LibHac.Tools.FsSystem.NcaUtils;

namespace LibHacToolNX.Tools.Writers.Impl;

/// <summary>
/// Describes an NCA archive.
/// </summary>
public class NcaDescription
{
    /// <summary>
    /// The magic.
    /// </summary>
    public required uint Magic { get; set; }

    /// <summary>
    /// The distribution type.
    /// </summary>
    public required DistributionType DistributionType { get; set; }

    /// <summary>
    /// The content type.
    /// </summary>
    public required NcaContentType ContentType { get; set; }

    /// <summary>
    /// The title id.
    /// </summary>
    public required ulong TitleId { get; set; }

    /// <summary>
    /// The content index.
    /// </summary>
    public required int ContentIndex { get; set; }

    /// <summary>
    /// The SDK version.
    /// </summary>
    public required uint SdkVersion { get; set; }
}