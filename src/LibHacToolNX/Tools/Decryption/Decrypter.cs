using LibHac.Common;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.Tools.FsSystem;
using LibHacToolNX.Logging;
using LibHacToolNX.Tools.Decryption.Impl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Path = System.IO.Path;

namespace LibHacToolNX.Tools.Decryption;

/// <summary>
/// An abstract decrypter.
/// </summary>
/// <remarks>This implementation has utility methods for assisting with decrypting common file formats.</remarks>
public abstract class Decrypter : IDecrypter
{
    /// <summary>
    /// Gets the logger.
    /// </summary>
    protected ILogger Logger { get; }

    /// <summary>
    /// Initializes an instance of the class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    protected Decrypter(ILogger logger)
    {
        Logger = logger;
    }

    public abstract Task DecryptAsync(FileInfo source, Stream outputStream, DecryptionOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts the contents.
    /// </summary>
    /// <param name="context">The context providing contextual details of the extraction process.</param>
    /// <returns>The list of extracted files.</returns>
    /// <exception cref="ArgumentException">The context must provide a file system.</exception>
    protected async Task<List<ExtractedFileInfo>> ExtractContentsAsync(DecryptionContext context)
    {
        if (context.FileSystem == null)
        {
            throw new ArgumentException("The context must provide a file system to extract.", nameof(context));
        }

        List<ExtractedFileInfo> result = new();

        foreach (var file in context.FileSystem.EnumerateEntries())
        {
            context.RequestAborted.ThrowIfCancellationRequested();

            Logger.WriteLine(file.FullPath);

            using var fileRef = new UniqueRef<IFile>();
            context.FileSystem.OpenFile(ref fileRef.Ref, file.FullPath.ToU8Span(), OpenMode.Read).ThrowIfFailure();

            await using var fs = fileRef.Get.AsStream();
            var outPath = new FileInfo(Path.Combine(context.TempDirectory.FullName, file.ToLocalPath()));

            if (string.Equals(Path.GetExtension(file.Name).ToLowerInvariant(), ".nca"))
            {
                var outDirectory = context.TempDirectory.CreateSubdirectory(Path.GetRandomFileName());

                try
                {
                    await using var outStream = outPath.Open(FileMode.Create, FileAccess.Write, FileShare.None);

                    var nca = new NcaDecrypter(Logger);
                    await nca.DecryptAsync(fs, outStream, new DecryptionOptions
                    {
                        KeySet = context.Options.KeySet,
                        TempDirectory = outDirectory,
                        IntegrityCheckLevel = context.Options.IntegrityCheckLevel,
                        UseAlternateFormat = context.Options.UseAlternateFormat
                    }, context.RequestAborted);

                    result.Add(new ExtractedFileInfo
                    {
                        SourcePath = file.FullPath,
                        TemporaryPath = outPath
                    });
                }
                finally
                {
                    if (outDirectory.Exists)
                    {
                        outDirectory.Delete(true);
                    }
                }
            }
            else
            {
                await using var outStream = outPath.OpenWrite();

                await fs.CopyToAsync(outStream, context.RequestAborted);
                await outStream.FlushAsync(context.RequestAborted);

                result.Add(new ExtractedFileInfo
                {
                    SourcePath = file.FullPath,
                    TemporaryPath = outPath
                });
            }
        }

        return result;
    }

    /// <summary>
    /// Describes an extracted file.
    /// </summary>
    protected class ExtractedFileInfo
    {
        /// <summary>
        /// The original path within the archive.
        /// </summary>
        public required string SourcePath { get; init; }

        /// <summary>
        /// The temporary path where the file is being held.
        /// </summary>
        public required FileInfo TemporaryPath { get; init; }
    }
}