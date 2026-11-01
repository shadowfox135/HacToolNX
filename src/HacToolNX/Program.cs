using HacToolNX.Commands;
using LibHac.Common.Keys;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace HacToolNX;

internal class Program
{
    /// <summary>
    /// The cancellation source which is signaled if/when the application is requested to terminate.
    /// </summary>
    private static CancellationTokenSource CancellationSource { get; } = new();

    internal static async Task Main(string[] args)
    {
        Console.CancelKeyPress += OnCancelKeyPressed;

        try
        {
            var program = new Program();
            await program.RunAsync(args);
        }
        catch (OperationCanceledException)
        {
            await Console.Out.WriteLineAsync("Aborted!");
            Environment.ExitCode = 1;
        }
        catch (MissingKeyException ex)
        {
            await Console.Out.WriteLineAsync($"ERR: The required {ex.Type} key '{ex.Name}' was not found.");
            Environment.ExitCode = 2;
        }
        catch (Exception ex)
        {
            await Console.Out.WriteLineAsync($"ERR: {ex}");
            Environment.ExitCode = 1;
        }
    }

    private async Task RunAsync(string[] args)
    {
        var context = ApplicationContext.Parse(args);

        context.CancellationToken = CancellationSource.Token;

        if (context.CommandName == null)
        {
            await ShowApplicationHelpAsync();
        }
        else
        {
            try
            {
                await ExecuteCommandAsync(context);
            }
            catch (CommandNotSupportedException)
            {
                await Console.Error.WriteLineAsync($"ERR: The command '{context.CommandName}' is not supported. For more information on available commands, see help information.");
                Environment.ExitCode = 1;
            }
        }
    }

    private async Task ExecuteCommandAsync(ApplicationContext context)
    {
        ICommand command = await CreateCommandAsync(context);

        Type? optionsType = CommandOptions.FindOptionsType(command.GetType());
        if (optionsType == null)
        {
            throw new NotSupportedException("The command is invalid.");
        }

        var opts = ParseOptions(optionsType, context.Arguments);
        if (opts.ShowHelp)
        {
            await ShowCommandHelpAsync(command, optionsType);
        }
        else
        {
            try
            {
                await command.ExecuteAsync(context, opts);
            }
            catch (CommandParameterException ex)
            {
                await Console.Error.WriteLineAsync($"ERR: {ex.Message} For more information on available command options, see help information.");
                Environment.ExitCode = 1;
            }
        }
    }

    private static async Task ShowApplicationHelpAsync()
    {
        await Console.Out.WriteLineAsync("Usage: hactoolnx.exe [command] [options]");
        await Console.Out.WriteLineAsync();

        var descriptors = DefaultCommandFactory.GetCommandTypes().Select(o => new
        {
            Type = o,
            Metadata = o.GetCustomAttribute<CommandAttribute>()!
        }).OrderBy(o => o.Metadata.Name).ToArray();

        var maxCommandNameLength = descriptors.Max(o => o.Metadata.Name.Length);

        await Console.Out.WriteLineAsync("Commands:");

        foreach (var descriptor in descriptors)
        {
            await Console.Out.WriteLineAsync(
                $"  {descriptor.Metadata.Name}{new string(' ', maxCommandNameLength - descriptor.Metadata.Name.Length)}     {descriptor.Metadata.Description}");
        }

        await Console.Out.WriteLineAsync();
        await ShowCommandOptionsAsync(typeof(CommandOptions), maxCommandNameLength);
    }

    private static async Task ShowCommandOptionsAsync(Type optionsType, int alignmentPadding = -1)
    {
        var options = optionsType.GetProperties()
            .Select(o =>
            {
                var optionAttribute = o.GetCustomAttribute<CommandOptionAttribute>()!;

                return new
                {
                    Attribute = optionAttribute,
                    OptionString = optionAttribute.Alias != null ? $"-{optionAttribute.Alias}, --{optionAttribute.Name}" : $"--{optionAttribute.Name}"
                };
            })
            .OrderByDescending(o => o.Attribute.Required)
            .ThenBy(o => o.Attribute.Global)
            .ThenBy(o => o.Attribute.Name).ToArray();

        await Console.Out.WriteLineAsync("Options:");

        var maxOptionNameLength = options.Max(o => o.OptionString.Length);
        if (maxOptionNameLength < alignmentPadding)
        {
            maxOptionNameLength = alignmentPadding;
        }

        foreach (var option in options)
        {
            string padding = new(' ', maxOptionNameLength - option.OptionString.Length);

            await Console.Out.WriteLineAsync(
                $"  {option.OptionString}{padding}     {option.Attribute.Description}");
        }
    }

    private static async Task ShowCommandHelpAsync(ICommand command, Type optionsType)
    {
        var attr = command.GetType().GetCustomAttribute<CommandAttribute>()!;

        await Console.Out.WriteLineAsync($"Usage: hactoolnx.exe {attr.Name} [options]");
        await Console.Out.WriteLineAsync();
        await Console.Out.WriteLineAsync("Description:");
        await Console.Out.WriteLineAsync(attr.Description);
        await Console.Out.WriteLineAsync();

        await ShowCommandOptionsAsync(optionsType);
    }

    private static CommandOptions ParseOptions(Type optionsType, ApplicationArgument[]? args)
    {
        var method = typeof(Program).GetMethod(nameof(ParseOptionsImpl), BindingFlags.Static | BindingFlags.NonPublic)!
            .MakeGenericMethod(optionsType);

        return (CommandOptions)method.Invoke(null, [args!])!;
    }

    private static TOptions ParseOptionsImpl<TOptions>(ApplicationArgument[] args) where TOptions : CommandOptions, new()
    {
        return CommandOptions.Parse<TOptions>(args);
    }

    private async Task<ICommand> CreateCommandAsync(ApplicationContext context)
    {
        if (string.IsNullOrWhiteSpace(context.CommandName))
        {
            throw new CommandNotSupportedException("The command name must be provided.");
        }

        var factory = new DefaultCommandFactory();
        return await factory.CreateAsync(context.CommandName, context.CancellationToken);
    }

    private static void OnCancelKeyPressed(object? sender, ConsoleCancelEventArgs e)
    {
        CancellationSource.Cancel();
        e.Cancel = true; // Notify the runtime that cancellation has been requested.
    }
}