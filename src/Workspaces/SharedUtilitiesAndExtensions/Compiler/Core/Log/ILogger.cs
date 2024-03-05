// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace Microsoft.CodeAnalysis.Internal.Log;

/// <summary>
/// logger interface actual logger should implements
/// </summary>
internal interface ILogger
{
    /// <summary>
    /// answer whether it is enabled or not for the specific function id
    /// </summary>
    bool IsEnabled(FunctionId functionId);

    /// <summary>
    /// log a specific event with context message
    /// </summary>
    void Log(FunctionId functionId, LogMessage logMessage);

    /// <summary>
    /// log a start event with context message
    /// </summary>
    void LogBlockStart(FunctionId functionId, LogMessage logMessage, int uniquePairId, CancellationToken cancellationToken);

    /// <summary>
    /// log an end event
    /// </summary>
    void LogBlockEnd(FunctionId functionId, LogMessage logMessage, int uniquePairId, int delta, CancellationToken cancellationToken);
}
