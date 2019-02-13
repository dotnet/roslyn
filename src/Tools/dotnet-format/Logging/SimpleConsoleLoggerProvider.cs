// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;
using System.CommandLine;

namespace Microsoft.CodeAnalysis.Tools.Logging
{
    internal class SimpleConsoleLoggerProvider : ILoggerProvider
    {
        private readonly IConsole _console;
        private readonly LogLevel _logLevel;

        public SimpleConsoleLoggerProvider(IConsole console, LogLevel logLevel)
        {
            _console = console;
            _logLevel = logLevel;
        }

        public ILogger CreateLogger(string name)
        {
            return new SimpleConsoleLogger(_console, _logLevel);
        }

        public void Dispose()
        {
        }
    }
}
