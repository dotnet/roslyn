// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Razor.Logging;

internal abstract partial class AbstractLoggerFactory
{
    private sealed class LazyLogger(Lazy<ILoggerProvider, LoggerProviderMetadata> lazyProvider, string categoryName)
    {
        private readonly LoggerProviderMetadata _metadata = lazyProvider.Metadata;
        private readonly Lazy<ILogger> _lazyLogger = new(() => lazyProvider.Value.CreateLogger(categoryName));

        public ILogger Instance => _lazyLogger.Value;

        public bool IsEnabled(LogLevel logLevel)
        {
            // If the ILoggerProvider's metadata has a minimum log level, we can use that
            // rather than forcing the ILoggerProvider to be created.
            if (_metadata.MinimumLogLevel is LogLevel minimumLogLevel &&
                !logLevel.IsAtLeast(minimumLogLevel))
            {
                return false;
            }

            return Instance.IsEnabled(logLevel);
        }
    }

    private class AggregateLogger(ImmutableArray<LazyLogger> lazyLoggers) : ILogger
    {
        private ImmutableArray<LazyLogger> _lazyLoggers = lazyLoggers;

        public bool IsEnabled(LogLevel logLevel)
        {
            foreach (var lazyLogger in _lazyLoggers)
            {
                if (lazyLogger.IsEnabled(logLevel))
                {
                    return true;
                }
            }

            return false;
        }

        public void Log(LogLevel logLevel, string message, Exception? exception)
        {
            foreach (var lazyLogger in _lazyLoggers)
            {
                if (lazyLogger.IsEnabled(logLevel))
                {
                    lazyLogger.Instance.Log(logLevel, message, exception);
                }
            }
        }

        internal void AddLogger(LazyLogger lazyLogger)
        {
            ImmutableInterlocked.Update(ref _lazyLoggers, (set, l) => set.Add(l), lazyLogger);
        }
    }
}
