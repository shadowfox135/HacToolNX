using LibHac.Common;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.Tools.FsSystem;
using LibHac.Tools.FsSystem.NcaUtils;
using System.Diagnostics;
using Path = System.IO.Path;

namespace LibHac.Tests.Tools.FsSystem.NcaUtils;

[TestFixture]
[Category("Manual")]
public class SecureNca2Tests : DecrypterTests
{
    /// <summary>
    /// Defines the source root path.
    /// </summary>
    protected static string SourceRootPath { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Emulation", "firmware", "firmware_19.0.1");

    private static readonly string OneFile = "35835b221a62ca73763455914bf0e9aa.nca";

    [Test]
    public void MustReadOneFile()
    {
        var keySet = CreateEncryptedKeySet();
        var file = new FileInfo(Path.Combine(SourceRootPath, OneFile));

        using var fs = file.OpenRead();

        var nca = SecureNca2.Create(keySet, fs);
        ProcessNca2(nca);
    }

    [Test]
    public void MustReadTheFiles()
    {
        var stopwatch = Stopwatch.StartNew();

        var succeeded = true;
        var keySet = CreateEncryptedKeySet();

        var directory = new DirectoryInfo(SourceRootPath);
        foreach (var file in directory.EnumerateFiles("*.nca", SearchOption.TopDirectoryOnly))
        {
            Debug.WriteLine("======================================================");

            using var fs = file.OpenRead();

            try
            {
                var nca = SecureNca2.Create(keySet, fs);

                Assert.That(nca, Is.Not.Null);
                Debug.WriteLine($"{file.Name}, ContentType={nca.Header.ContentType}, Entries={nca.Sections.Count}");

                ProcessNca2(nca);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{file.Name} failed due to: {ex}");
                succeeded = false;
            }
        }

        Debug.WriteLine($"Elapsed: {stopwatch.Elapsed}");
        Assert.That(succeeded, "At least one file failed to be loaded, check the debug output for more information.");
    }

    private static void ProcessNca2(SecureNca2 nca)
    {
        foreach (var section in nca.Sections)
        {
            Debug.WriteLine("------------------------------------------------------");
            Debug.WriteLine($"Section={section.Key}, Length={section.Value.SectionSize}");

            var fileSystem = nca.OpenFileSystem(section.Key, IntegrityCheckLevel.ErrorOnInvalid);

            foreach (var entry in fileSystem.EnumerateEntries("/", "*", SearchOptions.RecurseSubdirectories))
            {
                if (entry.Type == DirectoryEntryType.Directory)
                {
                    continue;
                }

                using var fileRef = new UniqueRef<IFile>();
                fileSystem.OpenFile(ref fileRef.Ref, entry.FullPath.ToU8Span(), OpenMode.Read).ThrowIfFailure();

                Debug.WriteLine($"Entry={entry.FullPath}, Length={entry.Size}");
            }
        }
    }
}