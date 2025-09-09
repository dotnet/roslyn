// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.LanguageServer.Logging;

internal sealed class LspLogMessageLoggerProvider(ILoggerFactory fallbackLoggerFactory, ServerConfiguration serverConfiguration) : ILoggerProvider, ISupportExternalScope
{
    private readonly ConcurrentDictionary<string, LspLogMessageLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);
    private IExternalScopeProvider? _externalScopeProvider;

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, new LspLogMessageLogger(categoryName, fallbackLoggerFactory, serverConfiguration, _externalScopeProvider));
    }

    public void Dispose()
    {
        _loggers.Clear();
        fallbackLoggerFactory.Dispose();
    }

    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        _externalScopeProvider = scopeProvider;
    }
}
