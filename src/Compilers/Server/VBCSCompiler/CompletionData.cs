// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Utilities;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CommandLine;
using System.IO.Pipes;

#nullable enable

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal enum CompletionReason
    {
        /// <summary>
        /// The request completed and results were provided to the client
        /// </summary>
        RequestCompleted,

        /// <summary>
        /// There was an error processing the request
        /// </summary>
        RequestError,
    }

    internal readonly struct CompletionData
    {
        internal CompletionReason Reason { get; }
        internal TimeSpan? NewKeepAlive { get; }
        internal bool ShutdownRequest { get; }

        internal CompletionData(CompletionReason reason, TimeSpan? newKeepAlive = null, bool shutdownRequsted = false)
        {
            Reason = reason;
            NewKeepAlive = newKeepAlive;
            ShutdownRequest = shutdownRequsted;
        }

        internal static CompletionData RequestCompleted { get; } = new CompletionData(CompletionReason.RequestCompleted);

        internal static CompletionData RequestError { get; } = new CompletionData(CompletionReason.RequestError);
    }
}

