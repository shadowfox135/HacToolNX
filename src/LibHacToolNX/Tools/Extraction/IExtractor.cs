using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LibHacToolNX.Tools.Extraction;

/// <summary>
/// A mechanism which can extract the contents of an archive.
/// </summary>
public interface IExtractor
{
    /// <summary>
    /// Extracts the contents of the archive.
    /// </summary>
    /// <param name="source">The file to extract.</param>
    /// <param name="options">The options to use while extracting the contents.</param>
    /// <param name="cancellationToken">A cancellation token to monitor for cancellation requests.</param>
    /// <returns>The task to await.</returns>
    Task ExtractAsync(FileInfo source, ExtractionOptions options, CancellationToken cancellationToken = default);
}