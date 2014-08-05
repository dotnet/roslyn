// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Internal.Log
{
    /// <summary>
    /// Implementation of <see cref="ILogger"/> that produce timing debug output. 
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal sealed class TraceLogger : ILogger
    {
        public static readonly TraceLogger Instance = new TraceLogger();

        private readonly Func<FunctionId, bool> loggingChecker;

        public TraceLogger()
            : this((Func<FunctionId, bool>)null)
        {
        }

        public TraceLogger(IOptionService optionService)
            : this(Logger.GetLoggingChecker(optionService))
        {
        }

        public TraceLogger(Func<FunctionId, bool> loggingChecker)
        {
            this.loggingChecker = loggingChecker;
        }

        public bool IsEnabled(FunctionId functionId)
        {
            return this.loggingChecker == null || this.loggingChecker(functionId);
        }

        /// <summary>
        /// Trace logger always uses verbose mode.
        /// </summary>
        public bool IsVerbose()
        {
            return true;
        }

        public void Log(FunctionId functionId, string message)
        {
            Trace.WriteLine(string.Format("[{0}] {1}/{2} - {3}", Thread.CurrentThread.ManagedThreadId, functionId.ToString(), message));
        }

        public IDisposable LogBlock(FunctionId functionId, string message, int uniquePairId, CancellationToken cancellationToken)
        {
            return new TraceLogBlock(functionId, message, uniquePairId, cancellationToken);
        }

        private class TraceLogBlock : IDisposable
        {
            private readonly CancellationToken cancellationToken;
            private readonly FunctionId functionId;
            private readonly int uniquePairId;

            private readonly Stopwatch watch;

            public TraceLogBlock(FunctionId functionId, string message, int uniquePairId, CancellationToken cancellationToken)
            {
                this.functionId = functionId;
                this.uniquePairId = uniquePairId;
                this.cancellationToken = cancellationToken;

                this.watch = Stopwatch.StartNew();
                Trace.WriteLine(string.Format("[{0}] Start({1}) : {2}/{3} - {4}", Thread.CurrentThread.ManagedThreadId, uniquePairId, functionId.ToString(), message));
            }

            public void Dispose()
            {
                var timeSpan = watch.ElapsedMilliseconds;
                var functionString = functionId.ToString() + (cancellationToken.IsCancellationRequested ? " Canceled" : string.Empty);
                Trace.WriteLine(string.Format("[{0}] End({1}) : [{2}ms] {3}/{4}", Thread.CurrentThread.ManagedThreadId, uniquePairId, timeSpan, functionString));
            }
        }
    }
}
