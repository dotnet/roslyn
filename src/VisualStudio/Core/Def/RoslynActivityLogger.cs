// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices
{
    /// <summary>
    /// Let people to inject <see cref="TraceSource"/> to monitor Roslyn activity
    /// 
    /// Here, we don't technically use TraceSource as it is meant to be used. but just as an easy 
    /// way to log data to listeners.
    /// 
    /// this also involves creating string, boxing and etc. so, perf wise, it will impact VS quite a bit.
    /// this also won't collect trace from Roslyn OOP for now. only in proc activity
    /// </summary>
    internal static class RoslynActivityLogger
    {
        private static readonly object s_gate = new object();

        public static void SetLogger(TraceSource traceSource)
        {
            Contract.ThrowIfNull(traceSource);

            lock (s_gate)
            {
                // internally, it just uses our existing ILogger
                Logger.SetLogger(AggregateLogger.AddOrReplace(new TraceSourceLogger(traceSource), Logger.GetLogger(), l => (l as TraceSourceLogger)?.TraceSource == traceSource));
            }
        }

        public static void RemoveLogger(TraceSource traceSource)
        {
            Contract.ThrowIfNull(traceSource);

            lock (s_gate)
            {
                // internally, it just uses our existing ILogger
                Logger.SetLogger(AggregateLogger.Remove(Logger.GetLogger(), l => (l as TraceSourceLogger)?.TraceSource == traceSource));
            }
        }

        private class TraceSourceLogger : ILogger
        {
            private const int LogEventId = 0;
            private const int StartEventId = 1;
            private const int EndEventId = 2;

            private static readonly ImmutableDictionary<FunctionId, string> s_functionIdCache;

            public readonly TraceSource TraceSource;

            static TraceSourceLogger()
            {
                // build enum to string cache
                s_functionIdCache =
                    Enum.GetValues(typeof(FunctionId)).Cast<FunctionId>().ToImmutableDictionary(f => f, f => f.ToString());
            }

            public TraceSourceLogger(TraceSource traceSource)
            {
                TraceSource = traceSource;
            }

            public bool IsEnabled(FunctionId functionId)
            {
                // we log every roslyn activity
                return true;
            }

            public void Log(FunctionId functionId, LogMessage logMessage)
            {
                TraceSource.TraceData(TraceEventType.Verbose, LogEventId, s_functionIdCache[functionId], logMessage.GetMessage());
            }

            public void LogBlockStart(FunctionId functionId, LogMessage logMessage, int uniquePairId, CancellationToken cancellationToken)
            {
                TraceSource.TraceData(TraceEventType.Verbose, StartEventId, s_functionIdCache[functionId], uniquePairId);
            }

            public void LogBlockEnd(FunctionId functionId, LogMessage logMessage, int uniquePairId, int delta, CancellationToken cancellationToken)
            {
                TraceSource.TraceData(TraceEventType.Verbose, EndEventId, s_functionIdCache[functionId], uniquePairId, cancellationToken.IsCancellationRequested, delta, logMessage.GetMessage());
            }
        }
    }
}
