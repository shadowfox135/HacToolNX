using System.Reflection;
using System.Threading.Tasks;

namespace HacToolNX.Commands;

/// <summary>
/// A command.
/// </summary>
/// <typeparam name="TOptions">The type of <see cref="CommandOptions"/> used by the command.</typeparam>
internal abstract class Command<TOptions> : ICommand
    where TOptions : CommandOptions
{
    /// <summary>
    /// The metadata for the command.
    /// </summary>
    public CommandAttribute Metadata => GetType().GetCustomAttribute<CommandAttribute>()!;

    /// <summary>
    /// Executes the command.
    /// </summary>
    /// <param name="context">The application execution context.</param>
    /// <param name="options">The options for the command.</param>
    /// <returns>The task to await.</returns>
    public abstract Task ExecuteAsync(ApplicationContext context, TOptions options);

    async Task ICommand.ExecuteAsync(ApplicationContext context, object options)
    {
        await ExecuteAsync(context, (TOptions)options);
    }
}