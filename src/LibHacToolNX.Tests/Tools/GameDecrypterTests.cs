namespace LibHac.Tests.Tools;

public abstract class GameDecrypterTests : DecrypterTests
{
    /// <summary>
    /// Defines the root path of any encrypted files.
    /// </summary>
    protected static string EncryptedRootPath { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Emulation", "backup");

    /// <summary>
    /// Defines the root path of any decrypted files.
    /// </summary>
    protected static string DecryptedRootPath { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Emulation", "roms");
}