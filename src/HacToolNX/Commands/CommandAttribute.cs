using System;

namespace HacToolNX.Commands;

/// <summary>
/// Describes a class as a command.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
internal class CommandAttribute : Attribute
{
    /// <summary>
    /// The name of the command.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The description of what the command does.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Initializes a new instance of the class.
    /// </summary>
    /// <param name="name">The name of the command.</param>
    /// <param name="description">The description of what the command does.</param>
    public CommandAttribute(string name, string? description = null)
    {
        Name = name;
        Description = description;
    }
}