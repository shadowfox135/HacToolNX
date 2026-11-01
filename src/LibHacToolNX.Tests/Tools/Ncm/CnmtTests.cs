using LibHac.Common;
using LibHac.Fs.Fsa;
using LibHac.Tools.FsSystem;
using LibHac.Tools.FsSystem.NcaUtils;
using LibHac.Tools.Ncm;
using LibHacToolNX.Tools.Writers.Impl;
using System.Diagnostics;

namespace LibHac.Tests.Tools.Ncm;

public class CnmtTests : DecrypterTests
{
    [Test]
    public void OpensTheNca()
    {
        var files = Directory.GetFiles("C:\\Temp\\Pokemon - Brilliant Diamond.xci\\secure", "*.cnmt.nca");

        foreach (var file in files)
        {
            Debug.WriteLine($"File: {file}");

            using var inStream = File.OpenRead(file);

            var keySet = CreateEncryptedKeySet();
            var nca = SecureNca2.Create(keySet, inStream.AsStorage());

            var fs = nca.OpenFileSystem(NcaSectionType.Data, IntegrityCheckLevel.ErrorOnInvalid);

            foreach (var entry in fs.EnumerateEntries())
            {
                Debug.WriteLine($"  - Entry: {entry.FullPath}");

                var cnmtFile = new UniqueRef<IFile>();
                fs.OpenFile(ref cnmtFile.Ref, entry.FullPath.ToU8Span(), LibHac.Fs.OpenMode.Read).ThrowIfFailure();

                var cnmt = new Cnmt(cnmtFile.Get.AsStream());
            }
        }
    }

    [Test]
    public void Works()
    {
        byte[] bytes = File.ReadAllBytes(@"../../../TestData/Patch_01002cd00a51c800.cnmt");

        using var ms = new MemoryStream(bytes);
        var cnmt = new Cnmt(ms);

        Assert.That(cnmt.ApplicationTitleId, Is.EqualTo(72106866075877376u));
        Assert.That(cnmt.ContentEntries[0].Size, Is.EqualTo(110777856));

        CnmtWriter target = new(bytes);
        PackagedContentInfo info = target.GetPackagedContentInfo(0);

        Assert.That(info.Size, Is.EqualTo(110777856));

        info.Size = 0;
        Assert.That(info.Size, Is.EqualTo(0));

        info.Size = 1;
        Assert.That(info.Size, Is.EqualTo(1));

        info.Size = 1099511627775;
        Assert.That(info.Size, Is.EqualTo(1099511627775));
    }
}