using LibHac.Common;
using LibHac.Common.Keys;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.Tools.FsSystem;
using LibHac.Tools.FsSystem.NcaUtils;
using System.Diagnostics;
using Path = System.IO.Path;

namespace LibHac.Tests.Tools;

[Category("Manual")]
public partial class DecrypterTests
{
    /// <summary>
    /// Defines the full path to the 'system' folder.
    /// </summary>
    private static string SystemPath { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".switch");

    protected static SecureKeySet CreateEncryptedKeySet()
    {
        var prodKeysFile = GetFileIfExists(SystemPath, "prod.keys");
        if (prodKeysFile == null)
        {
            Assert.Fail("The prod.keys file must be present to run this test.");
        }

        var titleKeysFile = GetFileIfExists(SystemPath, "title.keys");
        var consoleKeysFile = GetFileIfExists(SystemPath, "console.keys");

        var keySet = SecureKeySet.CreateSecureKeySet();
        ExternalKeyReader.ReadKeyFile(keySet, prodKeysFile, titleKeysFile, consoleKeysFile, (IProgressReport)null);

        return keySet;
    }

    private static string? GetFileIfExists(string path, string fileName)
    {
        var fullPath = Path.Combine(path, fileName);
        if (File.Exists(fullPath))
        {
            return fullPath;
        }

        return null;
    }

    /// <summary>
    /// Attempts to process an Nca file.
    /// </summary>
    /// <param name="keySet">The keyset.</param>
    /// <param name="nca">The NCA file to process.</param>
    /// <returns>true if the Nca was processed successfully, otherwise false.</returns>
    protected static bool TryProcessNca2(SecureKeySet keySet, SecureNca2 nca)
    {
        return TryProcessNca2(keySet, nca, DebugWriter.Default);
    }

    private static bool TryProcessNca2(SecureKeySet keySet, SecureNca2 nca, IWriter writer)
    {
        bool succeeded = true;

        foreach (var section in nca.Sections)
        {
            try
            {
                writer.WriteLine($"Section={section.Key}, Length={section.Value.SectionSize}, Offset={section.Value.SectionStartOffset}, HeaderVersion={nca.Header.FormatVersion}, Format={section.Value.FsHeader.FormatType}, FileVersion={section.Value.FsHeader.Version}");

                if (section.Value.FsHeader.IsPatchSection())
                {
                    // The section contains patch details that will not load correctly without a file being patched.
                    writer.WriteLine($"Section={section.Key}, Error=Patch file detected!");
                    continue;
                }

                try
                {
                    writer.Indent();

                    var fs = nca.OpenFileSystem(section.Key, IntegrityCheckLevel.ErrorOnInvalid);
                    succeeded &= TryProcessFileSystem2(keySet, fs, writer);
                }
                finally
                {
                    writer.Outdent();
                }
            }
            catch (Exception ex)
            {
                writer.WriteLine($"Section={section.Key}, Error={ex.Message}");
                succeeded = false;
            }
        }

        return succeeded;
    }

    /// <summary>
    /// Attempts to open all files within the file system.
    /// </summary>
    /// <remarks>Be advised this method will recursively load any additional NCA files it finds.</remarks>
    /// <param name="keySet">The keyset.</param>
    /// <param name="pfs">The file system whose files to open.</param>
    /// <returns>true if all files were able to open successfully, otherwise false.</returns>
    protected static bool TryProcessFileSystem2(SecureKeySet keySet, IFileSystem pfs)
    {
        return TryProcessFileSystem2(keySet, pfs, DebugWriter.Default);
    }

    private static bool TryProcessFileSystem2(SecureKeySet keySet, IFileSystem pfs, IWriter writer)
    {
        bool succeeded = true;

        foreach (var file in pfs.EnumerateEntries("/", "*", SearchOptions.RecurseSubdirectories))
        {
            try
            {
                writer.WriteLine($"Entry={file.FullPath}, Length={file.Size}");

                if (string.Equals(".nca", Path.GetExtension(file.Name), StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        writer.Indent();

                        // Process the child NCA within the parent archive.
                        using var fileRef = new UniqueRef<IFile>();
                        pfs.OpenFile(ref fileRef.Ref, file.FullPath.ToU8Span(), OpenMode.Read).ThrowIfFailure();

                        using var fs = fileRef.Get.AsStream();

                        succeeded &= TryProcessNca2(
                            keySet,
                            SecureNca2.Create(keySet, fs),
                            writer);
                    }
                    finally
                    {
                        writer.Outdent();
                    }
                }
            }
            catch (Exception ex)
            {
                writer.WriteLine($"Entry={file.FullPath}, Error={ex.Message}");
                succeeded = false;
            }
        }

        return succeeded;
    }
}

internal interface IWriter : IDisposable
{
    void WriteLine(string message);

    void Indent();
    void Outdent();

    void Flush();
}

internal abstract class Writer : IWriter
{
    private int indent;

    ~Writer()
    {
        Dispose(false);
    }

    public void Indent()
    {
        indent++;
    }

    public void Outdent()
    {
        if (indent == 0)
        {
            return;
        }

        indent--;
    }

    public virtual void Flush()
    {
        // This method intentionally left blank.
    }

    public void WriteLine(string message)
    {
        WriteLineImpl($"{string.Concat(Enumerable.Repeat("|-- ", indent))}{message}");
    }

    protected abstract void WriteLineImpl(string message);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {

    }
}

internal class DebugWriter : Writer
{
    public static DebugWriter Default { get; } = new();

    protected override void WriteLineImpl(string message)
    {
        Debug.WriteLine(message);
    }
}