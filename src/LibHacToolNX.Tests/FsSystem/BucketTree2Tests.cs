using LibHac.Common;
using LibHac.Fs;
using LibHac.FsSystem;
using LibHac.Tools.FsSystem;

namespace LibHac.Tests.FsSystem;

public partial class BucketTree2Tests
{
    /// <summary>
    /// | -- Relocation Tree (0-32768) -- | -- Encryption Tree (32768-65536) -- |
    /// </summary>
    private const string OneFile = "../../../TestData/BucketTree.bin";

    private static readonly byte[] _relocationTreeHeaderBytes = [66, 75, 84, 82, 1, 0, 0, 0, 99, 28, 0, 0, 0, 0, 0, 0];
    private const int RelocationTreeSize = 163840;

    private static readonly byte[] _encryptionTreeHeaderBytes = [66, 75, 84, 82, 1, 0, 0, 0, 72, 2, 0, 0, 0, 0, 0, 0];
    private const int EncryptionTreeSize = 32768;

    private static byte[] ReadTargetFile()
    {
        return File.ReadAllBytes(OneFile);
    }

    [Test]
    [Category("Manual")]
    public void RelocationTreeWorks()
    {
        Memory<byte> bytes = ReadTargetFile();

        var treeHeader2 = new BucketTreeHeader();
        _relocationTreeHeaderBytes.CopyTo(SpanHelpers.AsByteSpan(ref treeHeader2));

        Assert.That(treeHeader2.EntryCount, Is.EqualTo(7267));
        Assert.That(treeHeader2.HeaderSignature, Is.EqualTo(1381256002u));
        Assert.That(treeHeader2.Reserved, Is.EqualTo(0));
        Assert.That(treeHeader2.Version, Is.EqualTo(1u));

        using var ms = MemoryStorage2.Create(bytes.Slice(0, RelocationTreeSize).ToArray());

        var target = BucketTree2<IndirectStorage2.Entry>.Create(ms, new BucketTreeDefinition
        {
            Header = treeHeader2,
            Length = RelocationTreeSize
        });

        Assert.That(target.Count, Is.EqualTo(treeHeader2.EntryCount));
    }

    [Test]
    [Category("Manual")]
    public void EncryptionTreeWorks()
    {
        Memory<byte> bytes = ReadTargetFile();

        var treeHeader2 = new BucketTreeHeader();
        _encryptionTreeHeaderBytes.CopyTo(SpanHelpers.AsByteSpan(ref treeHeader2));

        Assert.That(treeHeader2.EntryCount, Is.EqualTo(584));
        Assert.That(treeHeader2.HeaderSignature, Is.EqualTo(1381256002u));
        Assert.That(treeHeader2.Reserved, Is.EqualTo(0));
        Assert.That(treeHeader2.Version, Is.EqualTo(1u));

        using var ms = MemoryStorage2.Create(bytes.Slice(RelocationTreeSize, EncryptionTreeSize).ToArray());

        var target = BucketTree2<Aes128CtrExStorage2.Entry>.Create(ms,
            new BucketTreeDefinition
            {
                Header = treeHeader2,
                Length = EncryptionTreeSize
            });

        Assert.That(target.Count, Is.EqualTo(treeHeader2.EntryCount));

        // Find the first one.
        var result = target.Find(0);
        Assert.That(result.Value.Offset, Is.EqualTo(0));
        Assert.That(result.Value.Reserved, Is.EqualTo(0));
        Assert.That(result.Value.Generation, Is.EqualTo(5));

        // Find a middle one.
        result = target.Find(76163616);
        Assert.That(result.Value.Offset, Is.EqualTo(23013360));
        Assert.That(result.Value.Reserved, Is.EqualTo(0));
        Assert.That(result.Value.Generation, Is.EqualTo(0));

        // Find one whose offset is between two offsets.
        result = target.Find(76163620);
        Assert.That(result.Value.Offset, Is.EqualTo(23013360));
        Assert.That(result.Value.Reserved, Is.EqualTo(0));
        Assert.That(result.Value.Generation, Is.EqualTo(0));

        // Find the last one (which happens to be the relocation tree)
        result = target.Find(2693054464);
        Assert.That(result.Value.Offset, Is.EqualTo(2663994944));
        Assert.That(result.EndOffset, Is.EqualTo(2693218304));
        Assert.That(result.Value.Reserved, Is.EqualTo(0));
        Assert.That(result.Value.Generation, Is.EqualTo(5));
    }
}