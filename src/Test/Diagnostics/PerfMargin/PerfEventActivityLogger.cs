// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Roslyn.Hosting.Diagnostics.PerfMargin
{
    // A slightly modified version of Roslyn.Services.Internal.Log.EtwLogger.
    // This version updates the DataModel whenever an operations starts or stops.  There
    // isn't an efficient way to listen to ETW events within the same process unless
    // running as admin, so we need to add our logic to the logger instead.
    internal class PerfEventActivityLogger : ILogger
    {
        private readonly DataModel _model;

        public PerfEventActivityLogger(DataModel model)
        {
            _model = model;
        }

        public bool IsEnabled(FunctionId functionId)
        {
            return true;
        }

        public void Log(FunctionId functionId, LogMessage logMessage)
        {
            // do nothing
        }

        public void LogBlockStart(FunctionId functionId, LogMessage logMessage, int uniquePairId, CancellationToken cancellationToken)
        {
            _model.BlockStart(functionId);
        }

        public void LogBlockEnd(FunctionId functionId, LogMessage logMessage, int uniquePairId, int delta, CancellationToken cancellationToken)
        {
            _model.BlockDisposed(functionId);
        }
    }
}
