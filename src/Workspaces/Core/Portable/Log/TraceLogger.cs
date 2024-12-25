// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.CodeAnalysis.Internal.Log;

/// <summary>
/// Implementation of <see cref="ILogger"/> that produce timing debug output. 
/// </summary>
internal sealed class TraceLogger(Func<FunctionId, bool>? isEnabledPredicate) : ILogger
{
    public static readonly TraceLogger Instance = new(isEnabledPredicate: null);

    public bool IsEnabled(FunctionId functionId)
        => isEnabledPredicate == null || isEnabledPredicate(functionId);

    public void Log(FunctionId functionId, LogMessage logMessage)
        => Trace.WriteLine(string.Format("[{0}] {1} - {2}", Environment.CurrentManagedThreadId, functionId.ToString(), logMessage.GetMessage()));

    public void LogBlockStart(FunctionId functionId, LogMessage logMessage, int uniquePairId, CancellationToken cancellationToken)
        => Trace.WriteLine(string.Format("[{0}] Start({1}) : {2} - {3}", Environment.CurrentManagedThreadId, uniquePairId, functionId.ToString(), logMessage.GetMessage()));

    public void LogBlockEnd(FunctionId functionId, LogMessage logMessage, int uniquePairId, int delta, CancellationToken cancellationToken)
    {
        var functionString = functionId.ToString() + (cancellationToken.IsCancellationRequested ? " Canceled" : string.Empty);
        Trace.WriteLine(string.Format("[{0}] End({1}) : [{2}ms] {3}", Environment.CurrentManagedThreadId, uniquePairId, delta, functionString));
    }
}
