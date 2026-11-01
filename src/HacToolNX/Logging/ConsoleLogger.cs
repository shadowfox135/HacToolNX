using LibHacToolNX.Logging;
using System;
using System.IO;
using System.Linq;

namespace HacToolNX.Logging;

/// <summary>
/// A mechanism used to write log messages to a destination.
/// </summary>
/// <remarks>This class handles indent and outdent capabilities as the higherarchy of the file tree is processed.</remarks>
/// <param name="writer">The text writer which should receive the logs.</param>
internal class ConsoleLogger(TextWriter writer) : ILogger
{
    private int indent;

    /// <summary>
    /// The default log writer. 
    /// </summary>
    /// <remarks>By default, this will output to the console.</remarks>
    public static ConsoleLogger Default { get; } = new(Console.Out);

    public void Indent()
    {
        indent++;
    }

    public void Outdent()
    {
        if (indent == 0)
        {
            return;
        }

        indent--;
    }

    public void WriteLine(string? message)
    {
        if (message == null)
            return;

        writer.WriteLine(indent == 0 ? message : $"{string.Concat(Enumerable.Repeat("|-- ", indent))}{message}");
    }
}