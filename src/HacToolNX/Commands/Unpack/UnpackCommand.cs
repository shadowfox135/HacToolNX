using LibHac.Tools.FsSystem;
using LibHacToolNX.Tools.Extraction;
using LibHacToolNX.Tools.Extraction.Impl;
using System;
using System.IO;
using System.Threading.Tasks;

namespace HacToolNX.Commands.Unpack;

/// <summary>
/// An <see cref="ICommand"/> which unpacks the contents of an archive to the local file system.
/// </summary>
[Command("unpack", "Unpacks the contents of an archive to the file system.")]
internal class UnpackCommand : AbstractDecryptCommand<UnpackCommandOptions>
{
    public override async Task ExecuteAsync(ApplicationContext context, UnpackCommandOptions options)
    {
        if (options.Source == null)
        {
            throw new CommandParameterException("The source file is required.");
        }

        var source = new FileInfo(options.Source);
        if (source is not { Exists: true })
        {
            throw new CommandParameterException($"The source file '{options.Source}' does not exist.");
        }

        if (string.IsNullOrWhiteSpace(options.Destination))
        {
            throw new CommandParameterException("The destination directory is required.");
        }

        var destination = new DirectoryInfo(Path.Combine(options.Destination, source.Name));
        if (destination is { Exists: true } && !options.Continue)
        {
            throw new CommandParameterException($"The destination directory '{options.Destination}' already exists, and continue is disabled.");
        }

        if (!destination.Exists)
        {
            destination.Create();
        }

        if (string.IsNullOrWhiteSpace(options.KeySet))
        {
            throw new CommandParameterException("The keyset directory is required.");
        }

        var keySetLocation = new DirectoryInfo(options.KeySet);
        if (keySetLocation is { Exists: false })
        {
            throw new CommandParameterException($"The keyset directory '{options.KeySet}' does not exist.");
        }

        var extractOptions = new ExtractionOptions
        {
            Destination = destination,
            KeySet = CreateSecureKeySet(keySetLocation),
            Recursive = options.Recursive,
            Continue = options.Continue,
            IntegrityCheckLevel = options.SkipIntegrityChecks ? IntegrityCheckLevel.None : IntegrityCheckLevel.ErrorOnInvalid
        };

        var extractor = CreateExtractor(source, context);
        await extractor.ExtractAsync(source, extractOptions, context.CancellationToken);
    }

    private IExtractor CreateExtractor(FileInfo source, ApplicationContext context)
    {
        return source.Extension.ToLowerInvariant() switch
        {
            ".nca" => new NcaExtractor(context.Logger),
            ".nsp" => new NspExtractor(context.Logger),
            ".xci" => new XciExtractor(context.Logger),
            _ => throw new NotSupportedException($"The file extension '{source.Extension}' is not supported.")
        };
    }
}