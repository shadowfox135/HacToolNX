using LibHac.Tools.Fs;
using System.Diagnostics;

namespace LibHac.Tests.Tools;

[TestFixture]
[Category("Manual")]
public partial class XciTests : GameDecrypterTests
{
    private const string OneFile = "The Legend of Zelda_ Link's Awakening [01006BB00C6F0000][v0] [NKA][C][T].xci";

    /// <summary>
    /// Defines the partition types available.
    /// </summary>
    private static readonly XciPartitionType[] _partitionTypes =
        typeof(XciPartitionType).GetEnumValues().Cast<XciPartitionType>().ToArray();

    [Test]
    public void ReadsTheOneFile()
    {
        var keySet = CreateEncryptedKeySet();
        using var rootFs = File.OpenRead(Path.Combine(EncryptedRootPath, OneFile));

        var succeeded = true;
        var xci = Xci2.Create(rootFs);

        foreach (var partitionType in _partitionTypes)
        {
            if (!xci.HasPartition(partitionType))
                continue;

            try
            {
                var pfs = xci.OpenPartition(partitionType);

                Debug.WriteLine($"====================================================================================");
                Debug.WriteLine($"Opened partition: {partitionType}");
                Debug.WriteLine($"====================================================================================");

                succeeded &= TryProcessFileSystem2(keySet, pfs);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Partition={partitionType}, Error={ex.Message}");
                succeeded = false;
            }
        }

        Assert.That(succeeded, "At least one part of the archive failed to be loaded, check the debug output for more information.");
    }
}