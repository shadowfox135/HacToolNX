using System;

namespace HacToolNX.Commands;

/// <summary>
/// Describes a property as an option for a command.
/// </summary>
/// <remarks>This attribute is typically used on the options class of a command.</remarks>
[AttributeUsage(AttributeTargets.Property)]
internal class CommandOptionAttribute : Attribute
{
    /// <summary>
    /// The name of the option.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The description of the option.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Optional. An alias that may be used in lieu of the name.
    /// </summary>
    public string? Alias { get; init; }

    /// <summary>
    /// Optional. Identifies whether an option is required.
    /// </summary>
    public bool Required => Alias != null;

    /// <summary>
    /// Optional. The default value to use.
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Identifies an option as global.
    /// </summary>
    /// <remarks>Setting this value will cause the option to show up toward the end of the help information.</remarks>
    public bool Global { get; set; }

    /// <summary>
    /// Initializes an instance of the class.
    /// </summary>
    /// <param name="name">The name of the option.</param>
    public CommandOptionAttribute(string name)
    {
        Name = name;
    }
}