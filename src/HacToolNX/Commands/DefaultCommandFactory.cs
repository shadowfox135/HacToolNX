using HacToolNX.Commands.Decrypt;
using HacToolNX.Commands.Unpack;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace HacToolNX.Commands;

/// <summary>
/// The default factory for mapping the command name to the type.
/// </summary>
internal class DefaultCommandFactory : ICommandFactory
{
    /// <summary>
    /// The types of classes that represent command operations.
    /// </summary>
    /// <remarks>To get a new command recognized, it must be registered here.</remarks>
    private static readonly Type[] _commandTypes =
    [
        typeof(DecryptCommand),
        typeof(UnpackCommand)
    ];

    /// <summary>
    /// Retrieves the command types registered.
    /// </summary>
    /// <returns>The array of command types.</returns>
    public static Type[] GetCommandTypes()
    {
        return _commandTypes;
    }

    private readonly Dictionary<string, Func<ICommand>> factoryRegistry;

    public DefaultCommandFactory() : this(CreateRegistry()) { }

    private DefaultCommandFactory(Dictionary<string, Func<ICommand>> factoryRegistry)
    {
        this.factoryRegistry = factoryRegistry;
    }

    public Task<ICommand> CreateAsync(string commandName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandName);

        if (!factoryRegistry.TryGetValue(commandName, out var factory))
        {
            throw new CommandNotSupportedException($"The command '{commandName}' is not recognized.");
        }

        ICommand result = factory();
        if (result == null)
        {
            throw new InvalidOperationException("The factory returned null.");
        }

        return Task.FromResult(result);
    }

    private static Dictionary<string, Func<ICommand>> CreateRegistry()
    {
        Dictionary<string, Func<ICommand>> result = new();

        foreach (var commandType in _commandTypes)
        {
            var descriptor = commandType.GetCustomAttribute<CommandAttribute>();
            if (descriptor == null)
            {
                continue;
            }

            result.Add(descriptor.Name, () =>
                (ICommand)(Activator.CreateInstance(commandType) ??
                           throw new NotSupportedException("The command could not be created, please ensure a default constructor is present.")));
        }

        return result;
    }
}