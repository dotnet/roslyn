// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.CodeAnalysis.Razor.Logging;

[InterpolatedStringHandler]
internal ref struct LogMessageInterpolatedStringHandler
{
    private PooledObject<StringBuilder> _builder;
    private readonly bool _isEnabled;

    public LogMessageInterpolatedStringHandler(int literalLength, int _, ILogger logger, LogLevel logLevel, out bool isEnabled)
    {
        _isEnabled = isEnabled = logger.IsEnabled(logLevel);
        if (isEnabled)
        {
            _builder = StringBuilderPool.GetPooledObject();
            _builder.Object.EnsureCapacity(literalLength);
        }
    }

    public bool IsEnabled => _isEnabled;

    public void AppendLiteral(string s)
    {
        _builder.Object.Append(s);
    }

    public void AppendFormatted<T>(T t)
    {
        _builder.Object.Append(GetMessage(t));
    }

    public void AppendFormatted<T>(T t, string format)
    {
        _builder.Object.AppendFormat(format, t);
    }

    public override string ToString()
    {
        var result = _builder.Object.ToString();
        _builder.Dispose();
        return result;
    }

    private static string GetMessage(object? value)
        => value switch
        {
            LspRange range => range.ToDisplayString(),
            Position position => position.ToDisplayString(),
            ISumType sumType => GetMessage(sumType.Value),

            null => "[null]",
            _ => value.ToString() ?? "[null]"
        };
}
