// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics.Tracing;
using System.Threading;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Internal.Log
{
    /// <summary>
    /// A logger that publishes events to ETW using an EventSource.
    /// </summary>
    internal sealed class EtwLogger : ILogger
    {
        private readonly Func<FunctionId, bool> loggingChecker;

        // Due to ETW specifics, RoslynEventSource.Instance needs to be initialized during EtwLogger construction 
        // so that we can enable the listeners synchronously before any events are logged.
        private readonly RoslynEventSource source = RoslynEventSource.Instance;

        public EtwLogger(IOptionService optionService)
            : this(Logger.GetLoggingChecker(optionService))
        {
        }

        public EtwLogger(Func<FunctionId, bool> loggingChecker)
        {
            this.loggingChecker = loggingChecker;
        }

        public bool IsEnabled(FunctionId functionId)
        {
            return source.IsEnabled() && (this.loggingChecker == null || this.loggingChecker(functionId));
        }

        public void Log(FunctionId functionId, LogMessage logMessage)
        {
            source.Log(GetMessage(logMessage), functionId);
        }

        public void LogBlockStart(FunctionId functionId, LogMessage logMessage, int uniquePairId, CancellationToken cancellationToken)
        {
            RoslynEventSource.Instance.BlockStart(GetMessage(logMessage), functionId, uniquePairId);
        }

        public void LogBlockEnd(FunctionId functionId, LogMessage logMessage, int uniquePairId, int delta, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                RoslynEventSource.Instance.BlockCanceled(functionId, delta, uniquePairId);
            }
            else
            {
                RoslynEventSource.Instance.BlockStop(functionId, delta, uniquePairId);
            }
        }

        private bool IsVerbose()
        {
            // "-1" makes this to work with any keyword
            return source.IsEnabled(EventLevel.Verbose, (EventKeywords)(-1));
        }

        private string GetMessage(LogMessage logMessage)
        {
            return IsVerbose() ? logMessage.GetMessage() : string.Empty;
        }
    }
}
