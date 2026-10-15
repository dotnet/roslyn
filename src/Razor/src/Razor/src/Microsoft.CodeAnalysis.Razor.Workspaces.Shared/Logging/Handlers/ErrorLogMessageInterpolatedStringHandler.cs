// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis.Razor.Logging;

[InterpolatedStringHandler]
internal ref struct ErrorLogMessageInterpolatedStringHandler
{
    private readonly LogMessageInterpolatedStringHandler _handler;

    public ErrorLogMessageInterpolatedStringHandler(int literalLength, int _, ILogger logger, out bool isEnabled)
    {
        _handler = new LogMessageInterpolatedStringHandler(literalLength, _, logger, LogLevel.Error, out isEnabled);
    }

    public bool IsEnabled => _handler.IsEnabled;

    public void AppendLiteral(string s)
        => _handler.AppendLiteral(s);

    public void AppendFormatted<T>(T t)
        => _handler.AppendFormatted(t);

    public void AppendFormatted<T>(T t, string format)
        => _handler.AppendFormatted(t, format);

    public override string ToString()
        => _handler.ToString();
}
