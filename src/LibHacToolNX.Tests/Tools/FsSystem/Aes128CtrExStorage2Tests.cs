using LibHac.Fs;
using LibHac.FsSystem;
using LibHac.Tools.FsSystem;
using LibHac.Tools.FsSystem.NcaUtils;
using System.Runtime.InteropServices;
using Path = System.IO.Path;

namespace LibHac.Tests.Tools.FsSystem;

public partial class Aes128CtrExStorage2Tests : DecrypterTests
{
    private const string OneFile = "Data.raw.bin";

    private static readonly byte[] Key = [183, 28, 230, 37, 190, 112, 101, 76, 69, 128, 223, 240, 186, 160, 25, 78];

    [Test]
    public void ReadsTheHeadBytesFromFirstBlock()
    {
        var startOffset = 0;
        var readLength = 5;

        var result = ReadBytesFromOneFileTheNewWay(startOffset, readLength);

        Assert.That(result.Length, Is.EqualTo(readLength));
        Assert.That(result, Is.EqualTo(
        [
            13, 10, 54, 116, 105
        ]));
    }

    [Test]
    public void ReadsTheMiddleBytesFromFirstBlock()
    {
        var startOffset = 16;
        var readLength = 32;

        var result = ReadBytesFromOneFileTheNewWay(startOffset, readLength);

        Assert.That(result.Length, Is.EqualTo(readLength));
        Assert.That(result, Is.EqualTo(
        [
            61, 50, 48, 57, 13, 10, 55, 110, 97, 109, 101, 61, 116, 101, 120, 116,
            95, 121, 111, 117, 13, 10, 55, 111, 98, 106, 101, 99, 116, 61, 111, 98
        ]));
    }

    [Test]
    public void ReadsTheTailBytesFromFirstBlock()
    {
        const int BlockEndOffset = 789952;

        var readLength = 32;
        var startOffset = BlockEndOffset - readLength;

        var result = ReadBytesFromOneFileTheNewWay(startOffset, readLength);

        Assert.That(result.Length, Is.EqualTo(readLength));
        Assert.That(result, Is.EqualTo(
        [
            0, 128, 63, 255, 255, 255, 2, 77, 65, 73, 78, 160, 0, 0, 0, 120,
            1, 157, 210, 219, 14, 195, 32, 8, 0, 80, 145, 206, 110, 245, 255, 191
        ]));
    }

    [Test]
    public void ReadsAcrossTwoBlocks()
    {
        const int BlockEndOffset = 789952;

        var readLength = 32;
        var startOffset = BlockEndOffset - (readLength / 2);

        var result = ReadBytesFromOneFileTheNewWay(startOffset, readLength);

        Assert.That(result.Length, Is.EqualTo(readLength));
        Assert.That(result, Is.EqualTo(
        [
            1, 157, 210, 219, 14, 195, 32, 8, 0, 80, 145, 206, 110, 245, 255, 191,
            215, 105, 9, 19, 144, 209, 166, 240, 98, 212, 19, 240, 146, 210, 147, 104
        ]));
    }

    [Test]
    public void ReadsTheLastBlock()
    {
        const int BlockOffset = 98857984;

        var result = ReadBytesFromOneFileTheNewWay(BlockOffset);

        Assert.That(result.Length, Is.EqualTo(2630144));
        Assert.That(result[..32].ToArray(), Is.EqualTo(
        [
            65, 176, 8, 6, 198, 195, 8, 140, 34, 68, 28, 10, 16, 20, 71, 4,
            120, 206, 144, 128, 115, 206, 137, 35, 50, 206, 24, 49, 68, 198, 25, 32
        ]));
    }

    private byte[] ReadBytesFromOneFileTheNewWay(long startOffset = 0, long? length = null)
    {
        const int SectionStartOffset = 0x4000;
        const int EncryptionTreeSize = 0x8000;

        using var fs = File.OpenRead(Path.Combine(@"..\..\..\TestData", OneFile)).AsStorage2();
        fs.GetSize(out var fsSize).ThrowIfFailure();

        var storage = Aes128CtrExStorage2.CreateEx(fs, new Aes128CtrExStorage2.StorageDescriptionEx
        {
            Counter = 8589934602,
            EncryptionKey = new NcaEncryptionKey
            {
                Key = Key,
                KeyType = NcaKeyType.AesCtr
            },
            SectionStartOffset = SectionStartOffset,
            SectionEndOffset = fsSize - EncryptionTreeSize,
            EncryptionTreeHeader = MemoryMarshal.Cast<byte, BucketTreeHeader>(new byte[] { 66, 75, 84, 82, 1, 0, 0, 0, 161, 0, 0, 0, 0, 0, 0, 0 })[0],
            EncryptionTreeSize = EncryptionTreeSize
        });

        byte[] buffer;
        if (length != null)
        {
            buffer = new byte[length.Value];
        }
        else
        {
            storage.GetSize(out var storageSize).ThrowIfFailure();
            buffer = new byte[storageSize - startOffset];
        }

        storage.Read(startOffset, buffer);

        return buffer;
    }
}