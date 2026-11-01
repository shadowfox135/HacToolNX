using System;

namespace HacToolNX.Commands;

/// <summary>
/// Thrown when command parameters were not supplied.
/// </summary>
internal class CommandParameterException(string message) : Exception(message);