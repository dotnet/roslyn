// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Razor.Logging;

internal sealed class LoggerProviderMetadata
{
    public static LoggerProviderMetadata Empty { get; } = new();

    public LogLevel? MinimumLogLevel { get; }

    private LoggerProviderMetadata()
    {
    }

    public LoggerProviderMetadata(IDictionary<string, object> data)
        : this()
    {
        MinimumLogLevel = data.TryGetValue(nameof(MinimumLogLevel), out var minimumLogLevel)
            ? (LogLevel?)minimumLogLevel
            : null;
    }
}
