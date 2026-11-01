using LibHac.Common.Keys;
using System.IO;

namespace HacToolNX.Commands;

/// <summary>
/// A command which exposes decryption capabilities.
/// </summary>
/// <typeparam name="TOptions">The type of <see cref="AbstractDecryptCommandOptions"/> used by the command.</typeparam>
internal abstract class AbstractDecryptCommand<TOptions> : Command<TOptions>
    where TOptions : AbstractDecryptCommandOptions
{
    protected static SecureKeySet CreateSecureKeySet(DirectoryInfo keySetLocation)
    {
        string? GetKeyFullPath(string fileName)
        {
            var fullPath = Path.Combine(keySetLocation.FullName, fileName);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"The file '{fullPath}' does not exist.", fullPath);
            }

            return fullPath;
        }

        var result = new SecureKeySet();
        ExternalKeyReader.ReadKeyFile(result,
            prodKeysFilename: GetKeyFullPath("prod.keys"),
            devKeysFilename: GetKeyFullPath("dev.keys"),
            titleKeysFilename: GetKeyFullPath("title.keys"));

        return result;
    }
}