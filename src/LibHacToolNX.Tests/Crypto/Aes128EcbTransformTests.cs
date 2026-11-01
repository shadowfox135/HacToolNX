using LibHac.Crypto;

namespace LibHac.Tests.Crypto;

public class Aes128EcbTransformTests
{
    [Test]
    public void MustEncryptTheData()
    {
        byte[] key =
        [
            1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16
        ];

        byte[] data =
        [
            1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16,
            17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32
        ];

        var transform = new Aes128EcbTransform(key, true);
        var result = transform.Transform(data);

        Assert.That(result, Is.EqualTo(32));
        Assert.That(data, Is.EqualTo([52, 195, 59, 127, 20, 253, 83, 220, 234, 37, 224, 26, 2, 225, 103, 39, 215, 33, 160, 241, 148, 35, 24, 34, 243, 152, 112, 109, 209, 255, 242, 183]));
    }

    [Test]
    public void MustDecryptTheData()
    {
        byte[] key =
        [
            1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16
        ];

        byte[] data =
        [
            52, 195, 59, 127, 20, 253, 83, 220, 234, 37, 224, 26, 2, 225, 103, 39, 215, 33, 160, 241, 148, 35, 24,
            34, 243, 152, 112, 109, 209, 255, 242, 183
        ];

        var transform = new Aes128EcbTransform(key, false);
        var result = transform.Transform(data);

        Assert.That(result, Is.EqualTo(32));
        Assert.That(data, Is.EqualTo([1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32]));
    }
}