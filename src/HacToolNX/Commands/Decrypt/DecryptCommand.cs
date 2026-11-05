using LibHac.Tools.FsSystem;
using LibHacToolNX.Tools;
using LibHacToolNX.Tools.Decryption;
using LibHacToolNX.Tools.Decryption.Impl;
using IDecrypter = LibHacToolNX.Tools.Decryption.IDecrypter;
using Path = System.IO.Path;

namespace HacToolNX.Commands.Decrypt;

/// <summary>
/// An <see cref="ICommand"/> which unpacks the contents of an archive to the local file system.
/// </summary>
[Command("decrypt", "Decrypts an archive.")]
internal class DecryptCommand : AbstractDecryptCommand<DecryptCommandOptions>
{
    public override async Task ExecuteAsync(ApplicationContext context, DecryptCommandOptions options)
    {
        if (options.Source == null)
        {
            throw new CommandParameterException("The source file is required.");
        }

        var source = new FileInfo(options.Source);
        if (!source.Exists)
        {
            throw new CommandParameterException($"The source file '{source.FullName}' does not exist.");
        }

        if (options.Destination == null)
        {
            throw new CommandParameterException("The destination is required.");
        }

        var destination = new DirectoryInfo(options.Destination);
        if (!destination.Exists)
        {
            throw new CommandParameterException($"The destination '{destination.FullName}' does not exist.");
        }

        var outputFile = new FileInfo(Path.Combine(destination.FullName,
            $"{Path.GetFileNameWithoutExtension(source.Name)}{GetOutputFileExtension(source, options)}"));

        if (!options.Overwrite && outputFile.Exists)
        {
            throw new CommandParameterException($"The destination file '{outputFile.FullName}' already exists.");
        }

        if (string.IsNullOrWhiteSpace(options.KeySet))
        {
            throw new CommandParameterException("The keyset directory is required.");
        }

        if (string.IsNullOrWhiteSpace(options.KeySet))
        {
            throw new CommandParameterException("The keyset directory is required.");
        }

        var keySetLocation = new DirectoryInfo(options.KeySet);
        if (!keySetLocation.Exists)
        {
            throw new CommandParameterException($"The keyset directory '{options.KeySet}' does not exist.");
        }

        var tempDirectory = PathUtils.CreateRandomTempDirectory();

        try
        {
            await using var outStream = outputFile.Open(FileMode.Create, FileAccess.Write, FileShare.None);

            var keySet = CreateSecureKeySet(keySetLocation);
            var decrypter = CreateDecrypter(source, options, context);

            await decrypter.DecryptAsync(source, outStream, new DecryptionOptions
            {
                KeySet = keySet,
                TempDirectory = tempDirectory,
                IntegrityCheckLevel = options.SkipIntegrityChecks ? IntegrityCheckLevel.None : IntegrityCheckLevel.ErrorOnInvalid,
                UseAlternateFormat = options.UseAlternateFormat
            }, context.CancellationToken);
        }
        catch (TaskCanceledException)
        {
            if (outputFile.Exists)
            {
                outputFile.Delete();
            }

            throw;
        }
        finally
        {
            if (tempDirectory.Exists)
            {
                tempDirectory.Delete(true);
            }
        }
    }

    private IDecrypter CreateDecrypter(FileInfo source, DecryptCommandOptions commandOptions, ApplicationContext context)
    {
        if (commandOptions.Firmware)
        {
            return source.Extension.ToLowerInvariant() switch
            {
                ".zip" => new ZipFileFirmwareDecrypter(context.Logger),
                _ => throw new NotSupportedException($"The file extension '{source.Extension}' is not supported for firmware.")
            };
        }

        return source.Extension.ToLowerInvariant() switch
        {
            ".nca" => new NcaDecrypter(context.Logger),
            ".nsp" => new NspDecrypter(context.Logger),
            ".xci" => new XciDecrypter(context.Logger),
            _ => throw new NotSupportedException($"The file extension '{source.Extension}' is not supported.")
        };
    }

    private string GetOutputFileExtension(FileInfo source, DecryptCommandOptions commandOptions)
    {
        return (source.Extension.ToLowerInvariant(), commandOptions.UseAlternateFormat) switch
        {
            (".zip", _) => ".zip",
            (".nca", false) => ".d.nca",
            (".nca", true) => ".dnca",
            (".xci", false) => ".d.xci",
            (".xci", true) => ".dxci",
            (".nsp", false) => ".d.nsp",
            (".nsp", true) => ".dnsp",
            _ => throw new NotSupportedException($"The file extension '{source.Extension}' is not supported.")
        };
    }
}