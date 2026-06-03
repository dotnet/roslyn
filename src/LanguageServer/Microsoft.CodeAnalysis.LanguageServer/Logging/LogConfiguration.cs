// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal sealed class LogConfiguration
{
    private int _currentLogLevel;

    public LogConfiguration(LogLevel initialLogLevel)
    {
        _currentLogLevel = (int)(initialLogLevel);
    }

    public void UpdateLogLevel(LogLevel level)
    {
        Interlocked.Exchange(ref _currentLogLevel, (int)level);
    }

    public LogLevel GetLogLevel()
    {
        return (LogLevel)_currentLogLevel;
    }
}
