// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// Holds the current log level for the language server - the initial level is specified by the
/// server arguments, but clients can update the log level at runtime.
/// </summary>
internal sealed class LogConfiguration
{
    private volatile LogLevel _currentLogLevel;

    public LogLevel LogLevel => _currentLogLevel;

    public LogConfiguration(LogLevel initialLogLevel)
    {
        _currentLogLevel = initialLogLevel;
    }

    public void UpdateLogLevel(LogLevel level)
    {
        _currentLogLevel = level;
    }
}
