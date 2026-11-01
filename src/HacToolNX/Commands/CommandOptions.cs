using System;
using System.Collections.Generic;
using System.Reflection;

namespace HacToolNX.Commands;

/// <summary>
/// Describes the options for a command.
/// </summary>
internal abstract class CommandOptions
{
    /// <summary>
    /// Identifies whether help information should be shown.
    /// </summary>
    [CommandOption("help", Description = "Shows the help information.", Global = true)]
    public bool ShowHelp { get; set; }

    /// <summary>
    /// Finds the options type for the command type.
    /// </summary>
    /// <param name="commandType"></param>
    /// <returns></returns>
    public static Type? FindOptionsType(Type commandType)
    {
        for (Type? current = commandType; current != null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(Command<>))
            {
                return current.GetGenericArguments()[0];
            }
        }

        return null;
    }

    /// <summary>
    /// Parses the options from the arguments.
    /// </summary>
    /// <param name="args">The arguments to parse.</param>
    /// <typeparam name="T">The type of options to parse.</typeparam>
    /// <returns>The parsed options.</returns>
    public static T Parse<T>(ApplicationArgument[]? args) where T : CommandOptions, new()
    {
        var map = BuildMap<T>();
        var result = new T();

        if (args != null)
        {
            foreach (var arg in args)
            {
                if (!map.TryGetValue(arg.Key, out var binding))
                {
                    throw new CommandParameterException($"The option '{arg.Key}' is not recognized.");
                }

                if (binding.Property.PropertyType == typeof(bool))
                {
                    binding.Property.SetValue(result, true);
                }
                else
                {
                    try
                    {
                        var converted = Convert.ChangeType(arg.Value, binding.Property.PropertyType);
                        binding.Property.SetValue(result, converted);
                    }
                    catch (Exception ex) when (ex is FormatException or InvalidCastException)
                    {
                        throw new CommandParameterException(
                            $"The value '{arg.Value}' is invalid for parameter '{arg.Key}'.");
                    }
                }
            }
        }

        return result;
    }

    private sealed record OptionBinding(PropertyInfo Property, CommandOptionAttribute Attribute);

    private static Dictionary<string, OptionBinding> BuildMap<T>()
    {
        var dict = new Dictionary<string, OptionBinding>(StringComparer.OrdinalIgnoreCase);

        foreach (var prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var attr = prop.GetCustomAttribute<CommandOptionAttribute>();
            if (attr is null)
                continue;

            OptionBinding binding = new(prop, attr);
            string?[] keys = [attr.Name, attr.Alias];

            foreach (var key in keys)
            {
                if (string.IsNullOrWhiteSpace(key))
                    continue;

                if (!dict.TryAdd(key, binding))
                {
                    throw new InvalidOperationException(
                        $"Duplicate key detected on '{prop.Name}' for type {typeof(T).FullName}");
                }
            }
        }

        return dict;
    }
}