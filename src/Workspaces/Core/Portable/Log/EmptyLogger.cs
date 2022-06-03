// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;

namespace Microsoft.CodeAnalysis.Internal.Log
{
    /// <summary>
    /// a logger that doesn't do anything
    /// </summary>
    internal sealed class EmptyLogger : ILogger
    {
        public static readonly EmptyLogger Instance = new();

        public bool IsEnabled(FunctionId functionId)
            => false;

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
