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
namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal enum CompletionReason
    {
        /// <summary>
        /// The request completed and results were provided to the client. This value is used even for such
        /// actions as a failed compilation because it was successfully executed and returned to the client.
        /// </summary>
        RequestCompleted,

        /// <summary>
        /// The request contained an error that should cause the server to shutdown. This can happen for cases
        /// like:
        ///    - server state is invalid because of a collision between analyzer assemblies
        ///    - client disconnected during build which is a treated as Ctrl-C event that should bring down 
        ///      the server.
        /// </summary>
        RequestError,
    }

    internal readonly struct CompletionData
    {
        internal CompletionReason Reason { get; }
        internal TimeSpan? NewKeepAlive { get; }
        internal bool ShutdownRequest { get; }

        internal CompletionData(CompletionReason reason, TimeSpan? newKeepAlive = null, bool shutdownRequested = false)
        {
            Reason = reason;
            NewKeepAlive = newKeepAlive;
            ShutdownRequest = shutdownRequested;
        }

        internal static CompletionData RequestCompleted { get; } = new CompletionData(CompletionReason.RequestCompleted);

        internal static CompletionData RequestError { get; } = new CompletionData(CompletionReason.RequestError);

        public override string ToString() => $"{Reason} KeepAlive:{NewKeepAlive} ShutdownRequest:{ShutdownRequest}";
    }
}

