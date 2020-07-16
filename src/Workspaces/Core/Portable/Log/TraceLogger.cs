// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Internal.Log
{
    /// <summary>
    /// Implementation of <see cref="ILogger"/> that produce timing debug output. 
    /// </summary>
    internal sealed class TraceLogger : ILogger
    {
        public static readonly TraceLogger Instance = new TraceLogger();

        private readonly Func<FunctionId, bool> _loggingChecker;

        public TraceLogger()
            : this((Func<FunctionId, bool>)null)
        {
        }

        public TraceLogger(IGlobalOptionService optionService)
            : this(Logger.GetLoggingChecker(optionService))
        {
        }

        public TraceLogger(Func<FunctionId, bool> loggingChecker)
            => _loggingChecker = loggingChecker;

        public bool IsEnabled(FunctionId functionId)
            => _loggingChecker == null || _loggingChecker(functionId);

        public void Log(FunctionId functionId, LogMessage logMessage)
            => Trace.WriteLine(string.Format("[{0}] {1} - {2}", Thread.CurrentThread.ManagedThreadId, functionId.ToString(), logMessage.GetMessage()));

        public void LogBlockStart(FunctionId functionId, LogMessage logMessage, int uniquePairId, CancellationToken cancellationToken)
            => Trace.WriteLine(string.Format("[{0}] Start({1}) : {2} - {3}", Thread.CurrentThread.ManagedThreadId, uniquePairId, functionId.ToString(), logMessage.GetMessage()));

        public void LogBlockEnd(FunctionId functionId, LogMessage logMessage, int uniquePairId, int delta, CancellationToken cancellationToken)
        {
            var functionString = functionId.ToString() + (cancellationToken.IsCancellationRequested ? " Canceled" : string.Empty);
            Trace.WriteLine(string.Format("[{0}] End({1}) : [{2}ms] {3}", Thread.CurrentThread.ManagedThreadId, uniquePairId, delta, functionString));
        }
    }
}
