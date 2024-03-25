// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Writing;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Logging
{
    internal sealed class LsifFormatLogger : ILogger
    {
        private readonly TextWriter _writer;
        private readonly object _writerGate = new object();

        public LsifFormatLogger(TextWriter writer)
        {
            _writer = writer;
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            throw new NotImplementedException();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= LogLevel.Information;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);

            var severity = logLevel switch { LogLevel.Information => "Info", LogLevel.Warning => "Warning", LogLevel.Error => "Error", LogLevel.Critical => "Critical", _ => throw ExceptionUtilities.UnexpectedValue(logLevel) };

            var command = new CommandWithParameters("log",
                new LogCommandParameters(severity, message, exception?.Message, exception?.GetType().ToString(), exception?.StackTrace));
            var serializedCommand = JsonConvert.SerializeObject(command, LineModeLsifJsonWriter.SerializerSettings);

            lock (_writerGate)
            {
                _writer.Write(serializedCommand);
            }
        }

        private sealed record CommandWithParameters(string Command, object Parameters);
        private sealed record LogCommandParameters(
            string Severity,
            string Message,
            string? ExceptionMessage,
            string? ExceptionType,
            string? CallStack);
    }
}
