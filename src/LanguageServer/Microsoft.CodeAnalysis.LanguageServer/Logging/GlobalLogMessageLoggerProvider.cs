// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.LanguageServer.Logging;

internal sealed class GlobalLogMessageLoggerProvider(
    ILoggerFactory fallbackLoggerFactory,
    LanguageServerConnectionManager connectionManager,
    LogConfiguration fallbackLogConfiguration) : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, GlobalLogMessageLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);

    public ILogger CreateLogger(string categoryName)
        => _loggers.GetOrAdd(categoryName, new GlobalLogMessageLogger(categoryName, fallbackLoggerFactory, connectionManager, fallbackLogConfiguration));

    public void Dispose()
    {
        _loggers.Clear();
        fallbackLoggerFactory.Dispose();
    }
}