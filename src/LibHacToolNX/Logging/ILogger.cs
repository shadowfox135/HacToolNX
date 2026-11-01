namespace LibHacToolNX.Logging;

/// <summary>
/// A mechanism to write log entries.
/// </summary>
/// <remarks>The implementations of this interface are expected to store state of their current indentation level.</remarks>
public interface ILogger
{
    /// <summary>
    /// Indents the log.
    /// </summary>
    void Indent();

    /// <summary>
    /// Outdents the log.
    /// </summary>
    void Outdent();

    /// <summary>
    /// Writes the message.
    /// </summary>
    /// <remarks>For null values, the message will be ignored.</remarks>
    /// <param name="message">The message to write.</param>
    void WriteLine(string? message);
}