// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;

namespace Microsoft.CodeAnalysis.Internal.Log
{
    /// <summary>
    /// a logger that doesn't do anything
    /// </summary>
    internal sealed class EmptyLogger : ILogger
    {
        public static readonly EmptyLogger Instance = new EmptyLogger();

        public bool IsEnabled(FunctionId functionId)
        {
            return false;
        }

        public void Log(FunctionId functionId, LogMessage logMessage)
        {
        }

        public void LogBlockStart(FunctionId functionId, LogMessage logMessage, int uniquePairId, CancellationToken cancellationToken)
        {
        }

        public void LogBlockEnd(FunctionId functionId, LogMessage logMessage, int uniquePairId, int delta, CancellationToken cancellationToken)
        {
        }
    }
}
