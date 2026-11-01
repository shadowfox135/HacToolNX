using System.Threading.Tasks;

namespace HacToolNX.Commands;

/// <summary>
/// A command.
/// </summary>
internal interface ICommand
{
    /// <summary>
    /// Gets the command metadata.
    /// </summary>
    CommandAttribute Metadata { get; }

    /// <summary>
    /// Executes the command.
    /// </summary>
    /// <param name="context">The application execution context.</param>
    /// <param name="options">The options for the command.</param>
    /// <returns>The task to await.</returns>
    Task ExecuteAsync(ApplicationContext context, object options);
}