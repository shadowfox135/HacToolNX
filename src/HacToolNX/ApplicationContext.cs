using HacToolNX.Logging;
using LibHacToolNX.Logging;

namespace HacToolNX;

/// <summary>
/// A context providing contextual information for the application.
/// </summary>
internal class ApplicationContext
{
    /// <summary>
    /// The cancellation token to monitor for cancellation requests.
    /// </summary>
    public CancellationToken CancellationToken { get; set; }

    /// <summary>
    /// The name of the command to execute.
    /// </summary>
    public string? CommandName { get; set; }

    /// <summary>
    /// The arguments for the command.
    /// </summary>
    public ApplicationArgument[]? Arguments { get; set; }

    /// <summary>
    /// The logger.
    /// </summary>
    public required ILogger Logger { get; set; }

    /// <summary>
    /// Parses the application execution context settings from the command line arguments.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    /// <returns>The context.</returns>
    public static ApplicationContext Parse(string[] args)
    {
        var result = new ApplicationContext
        {
            Logger = ConsoleLogger.Default
        };

        if (args.Length > 0)
        {
            result.CommandName = args[0].ToLowerInvariant();

            if (args.Length > 1)
            {
                var commandArgs = new List<ApplicationArgument>();

                ApplicationArgument? current = null;

                for (var index = 1; index < args.Length; index++)
                {
                    var arg = args[index];

                    if (arg.StartsWith('-'))
                    {
                        if (current != null)
                        {
                            commandArgs.Add(current);
                        }

                        if (arg.StartsWith(ApplicationArgument.OptionPrefix))
                        {
                            current = new ApplicationArgument { Key = arg.Substring(2) };
                        }
                        else if (arg.StartsWith(ApplicationArgument.AliasPrefix))
                        {
                            current = new ApplicationArgument { Key = arg.Substring(1) };
                        }
                    }
                    else if (current != null)
                    {
                        current.Value = arg;
                        commandArgs.Add(current);

                        current = null;
                    }
                }

                if (current != null)
                {
                    commandArgs.Add(current);
                }

                result.Arguments = commandArgs.ToArray();
            }
        }

        return result;
    }
}