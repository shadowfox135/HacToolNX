using System.Threading;
using System.Threading.Tasks;

namespace HacToolNX.Commands;

/// <summary>
/// A mechanism which can create a command.
/// </summary>
internal interface ICommandFactory
{
    /// <summary>
    /// Creates the command.
    /// </summary>
    /// <param name="commandName">The name of the command.</param>
    /// <param name="cancellationToken">The cancellation token to monitor for cancellation requests.</param>
    /// <returns>The command.</returns>
    Task<ICommand> CreateAsync(string commandName, CancellationToken cancellationToken = default);
}