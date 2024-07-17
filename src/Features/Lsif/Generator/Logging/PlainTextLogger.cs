// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Logging
{
    internal sealed class PlainTextLogger : ILogger
    {
        private readonly TextWriter _writer;
        private readonly object _gate = new object();

        public PlainTextLogger(TextWriter writer)
        {
            _writer = writer;
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            throw new NotImplementedException();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            // Not all formatters will actually include the exception even if we pass it through, so include it here.
            var message = formatter(state, exception);
            var exceptionString = exception?.ToString();

            lock (_gate)
            {
                _writer.WriteLine(message);

                if (exceptionString != null)
                    _writer.WriteLine(exceptionString);
            }
        }
    }
}
