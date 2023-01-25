// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.LanguageServer.Logging;
internal class AggregateLoggerProvider : ILoggerProvider
{
    private readonly ILoggerFactory _fallbackLoggerFactory;
    private readonly ConcurrentDictionary<string, AggregateLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);

    public AggregateLoggerProvider(ILoggerFactory fallbackLoggerFactory)
    {
        _fallbackLoggerFactory = fallbackLoggerFactory;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, new AggregateLogger(categoryName, _fallbackLoggerFactory));
    }

    public void Dispose()
    {
        _loggers.Clear();
        _fallbackLoggerFactory.Dispose();
    }
}
