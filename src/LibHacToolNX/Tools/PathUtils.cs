using LibHac.Tools.Fs;
using System.IO;

namespace LibHacToolNX.Tools;

/// <summary>
/// Contains utility methods for path operations.
/// </summary>
public static class PathUtils
{
    /// <summary>
    /// Creates a random temp directory.
    /// </summary>
    /// <returns>The new directory.</returns>
    public static DirectoryInfo CreateRandomTempDirectory()
    {
        return new DirectoryInfo(Path.GetTempPath()).CreateRandomSubdirectory();
    }

    /// <summary>
    /// Creates a random subdirectory.
    /// </summary>
    /// <param name="directory">The parent directory where the subdirectory should be created.</param>
    /// <returns>The new subdirectory.</returns>
    public static DirectoryInfo CreateRandomSubdirectory(this DirectoryInfo directory)
    {
        DirectoryInfo result;

        while (true)
        {
            result = new DirectoryInfo(Path.Combine(directory.FullName, Path.GetRandomFileName()));
            if (!result.Exists)
            {
                result.Create();
                break;
            }
        }

        return result;
    }

    /// <summary>
    /// Converts the path of the entry to a local file path.
    /// </summary>
    /// <param name="entry">The directory entry.</param>
    /// <returns>The path of the file on the local file system.</returns>
    public static string ToLocalPath(this DirectoryEntryEx entry)
    {
        return entry.FullPath[1..].Replace('/', Path.DirectorySeparatorChar);
    }
}