using LibHac.Tools.FsSystem;
using LibHac.Tools.FsSystem.NcaUtils;
using static System.Collections.Specialized.BitVector32;

namespace LibHac.Tests.Tools;

[TestFixture]
[Category("Manual")]
public partial class NcaTests : GameDecrypterTests
{
    private const string OneFile = "336693c4e55c44f46b3f2e425b26a409";

    private const string PatchFile = "5146edd5f9d0579a488ea31d0610e859"; // This is the patch file for the one file.

    [Test]
    public void ReadsTheOneFile()
    {
        var keySet = CreateEncryptedKeySet();
        using var fs = File.OpenRead(Path.Combine(EncryptedRootPath, $"{OneFile}.nca"));

        var succeeded = TryProcessNca2(keySet, SecureNca2.Create(keySet, fs));
        Assert.That(succeeded, "At least one part of the archive failed to be loaded, check the debug output for more information.");
    }

    [Test]
    public void ReadsThePatchedSection()
    {
        var keySet = CreateEncryptedKeySet();
        using var oneFs = File.OpenRead(Path.Combine(EncryptedRootPath, $"{OneFile}.nca"));
        using var patchFs = File.OpenRead(Path.Combine(EncryptedRootPath, $"{PatchFile}.nca"));

        var nca = SecureNca2.Create(keySet, oneFs);
        var patch = SecureNca2.Create(keySet, patchFs);

        var storage = patch.OpenStorageWithPatch(nca, NcaSectionType.Data, IntegrityCheckLevel.ErrorOnInvalid);

        Span<byte> buffer = stackalloc byte[0x400];
        storage.Read(0, buffer).ThrowIfFailure();
    }

    [Test]
    public void ReadsTheDecryptedPatchedSection()
    {
        var keySet = CreateEncryptedKeySet();
        using var oneFs = File.OpenRead(Path.Combine(DecryptedRootPath, $"{OneFile}.d.nca"));
        using var patchFs = File.OpenRead(Path.Combine(DecryptedRootPath, $"{PatchFile}.d.nca"));

        var nca = SecureNca2.Create(keySet, oneFs);
        var patch = SecureNca2.Create(keySet, patchFs);

        var storage = patch.OpenStorageWithPatch(nca, NcaSectionType.Data, IntegrityCheckLevel.ErrorOnInvalid);

        Span<byte> buffer = stackalloc byte[0x400];
        storage.Read(0, buffer).ThrowIfFailure();
    }

    [Test]
    public void ReadsTheOneDecryptedFile()
    {
        var keySet = CreateEncryptedKeySet();
        using var fs = File.OpenRead(Path.Combine(DecryptedRootPath, $"{OneFile}.d.nca"));

        var succeeded = TryProcessNca2(keySet, SecureNca2.Create(keySet, fs));
        Assert.That(succeeded, "At least one part of the archive failed to be loaded, check the debug output for more information.");
    }

    [Test]
    public void ReadsTheOneFilePatched()
    {
        var keySet = CreateEncryptedKeySet();
        using var oneFs = File.OpenRead(Path.Combine(EncryptedRootPath, $"{OneFile}.nca"));
        using var patchFs = File.OpenRead(Path.Combine(EncryptedRootPath, $"{PatchFile}.nca"));

        var nca = SecureNca2.Create(keySet, oneFs);
        var patch = SecureNca2.Create(keySet, patchFs);

        var fs = patch.OpenFileSystemWithPatch(nca, NcaSectionType.Data, IntegrityCheckLevel.ErrorOnInvalid);

        var succeeded = TryProcessFileSystem2(keySet, fs);
        Assert.That(succeeded, "At least one part of the archive failed to be loaded, check the debug output for more information.");
    }

    [Test]
    public void ReadsTheOneDecryptedFilePatched()
    {
        var keySet = CreateEncryptedKeySet();
        using var oneFs = File.OpenRead(Path.Combine(DecryptedRootPath, $"{OneFile}.d.nca"));
        using var patchFs = File.OpenRead(Path.Combine(DecryptedRootPath, $"{PatchFile}.d.nca"));

        var nca = SecureNca2.Create(keySet, oneFs);
        var patch = SecureNca2.Create(keySet, patchFs);

        var fs = patch.OpenFileSystemWithPatch(nca, NcaSectionType.Data, IntegrityCheckLevel.ErrorOnInvalid);

        var succeeded = TryProcessFileSystem2(keySet, fs);
        Assert.That(succeeded, "At least one part of the archive failed to be loaded, check the debug output for more information.");
    }
}