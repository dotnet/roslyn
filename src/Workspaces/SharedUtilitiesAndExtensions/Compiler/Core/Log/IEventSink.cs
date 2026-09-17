// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace Microsoft.CodeAnalysis.Internal.Log;

/// <summary>
/// A destination for events reported by <see cref="RoslynTelemetry"/>.
/// </summary>
internal interface IEventSink
{
    /// <summary>
    /// Whether this sink will record anything for <paramref name="functionId"/>.
    /// </summary>
    bool IsEnabled(FunctionId functionId);

    void Log(FunctionId functionId, LogMessage logMessage);

    /// <summary>
    /// Records the start of a scope. <paramref name="uniquePairId"/> pairs this call with the
    /// <see cref="LogBlockEnd"/> that closes it.
    /// </summary>
    void LogBlockStart(FunctionId functionId, LogMessage logMessage, int uniquePairId, CancellationToken cancellationToken);

    /// <summary>
    /// Records the end of the scope opened by <see cref="LogBlockStart"/> with the same
    /// <paramref name="uniquePairId"/>. <paramref name="delta"/> is the elapsed milliseconds.
    /// </summary>
    void LogBlockEnd(FunctionId functionId, LogMessage logMessage, int uniquePairId, int delta, CancellationToken cancellationToken);
}