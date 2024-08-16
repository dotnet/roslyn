// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.CodeAnalysis.Telemetry;

[InterpolatedStringHandler]
internal readonly struct TelemetryLoggingInterpolatedStringHandler
{
    private readonly StringBuilder _stringBuilder;

    public TelemetryLoggingInterpolatedStringHandler(int literalLength, int _)
    {
        _stringBuilder = new StringBuilder(capacity: literalLength);
    }

    public void AppendLiteral(string value) => _stringBuilder.Append(value);

    public void AppendFormatted<T>(T value) => _stringBuilder.Append(value?.ToString());

    public string GetFormattedText() => _stringBuilder.ToString();
}
