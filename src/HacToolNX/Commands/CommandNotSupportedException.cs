using System;

namespace HacToolNX.Commands;

internal class CommandNotSupportedException(string message) : NotSupportedException(message);