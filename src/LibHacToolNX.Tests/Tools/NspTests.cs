using LibHac.Fs;
using LibHac.FsSystem;
using Path = System.IO.Path;

namespace LibHac.Tests.Tools;

[TestFixture]
[Category("Manual")]
public partial class NspTests : GameDecrypterTests
{
    /// <summary>
    /// The main file to use.
    /// </summary>
    private const string OneFile = "Baba Is You [01002CD00A51C000][v0][Base].nsp";

    /// <summary>
    /// The patch file to use (must be associated with the one file).
    /// </summary>
    private const string PatchFile = "Baba Is You [01002CD00A51C800][v720896][Update].nsp";

    [Test]
    public void ReadsTheOneFile()
    {
        var keySet = CreateEncryptedKeySet();
        using var fs = File.OpenRead(Path.Combine(EncryptedRootPath, OneFile)).AsStorage2();

        var pfs = PartitionFileSystem2.Create(fs);
        var succeeded = TryProcessFileSystem2(keySet, pfs);

        Assert.That(succeeded, "At least one file failed to be loaded, check the debug output for more information.");
    }

    // [Test]
    // public void ReadsThePatchFile()
    // {
    //     var keySet = CreateEncryptedKeySet();
    //     
    //     using var fs = File.OpenRead(Path.Combine(EncryptedRootPath, PatchFile)).AsStorage2(false);
    //     var pfs = PartitionFileSystem2.Create(fs);
    //
    //     var succeeded = TryProcessFileSystem2(keySet, pfs);
    //     Assert.That(succeeded, "At least one part of the archive failed load, check the debug output for more information.");
    // }

    // [Test]
    // public void PatchesTheFileTheOldWay()
    // {
    //     var keySet = CreateEncryptedKeySet();
    //     using var fs1 = File.OpenRead(Path.Combine(EncryptedRootPath, OneFile)).AsStorage();
    //     
    //     var pfs1 = new PartitionFileSystem();
    //     pfs1.Initialize(fs1).ThrowIfFailure();
    //     
    //     using var fs2 = File.OpenRead(Path.Combine(EncryptedRootPath, PatchFile)).AsStorage();
    //     var pfs2 = new PartitionFileSystem();
    //     pfs2.Initialize(fs2).ThrowIfFailure();
    //     
    //     var succeeded = TryProcessFileSystem()
    //     Assert.True(false, "At least one file failed to be loaded, check the debug output for more information.");
    // }
}