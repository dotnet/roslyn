// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace Microsoft.CodeAnalysis.Internal.Log;

/// <summary>
/// A destination for discrete events and scopes identified by <see cref="FunctionId"/>.
/// Implementations decide, via <see cref="IsEnabled"/>, whether anything is recorded at all;
/// that is where consent (for telemetry sinks) and opt-in enablement (for diagnostic sinks) live.
/// </summary>
internal interface IEventSink
{
    /// <summary>
    /// Whether this sink will record anything for <paramref name="functionId"/>. Checked before any
    /// <see cref="LogMessage"/> is constructed, so returning false makes logging allocation-free.
    /// </summary>
    bool IsEnabled(FunctionId functionId);

    /// <summary>
    /// Record a discrete event with context message.
    /// </summary>
    void Log(FunctionId functionId, LogMessage logMessage);

    /// <summary>
    /// Record the start of a scope with context message.
    /// </summary>
    void LogBlockStart(FunctionId functionId, LogMessage logMessage, int uniquePairId, CancellationToken cancellationToken);

    /// <summary>
    /// Record the end of a scope.
    /// </summary>
    void LogBlockEnd(FunctionId functionId, LogMessage logMessage, int uniquePairId, int delta, CancellationToken cancellationToken);
}