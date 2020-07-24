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

        /// <summary>
        /// There was a request from the client to shutdown the server.
        /// </summary>
        RequestShutdown,

        /// <summary>
        /// There was a request to change the timeout value of the server
        /// </summary>
        RequestTimeoutChange,
    }

    internal readonly struct CompletionData
    {
        private readonly TimeSpan? _timeout;

        internal CompletionReason Reason { get; }

        internal TimeSpan NewTimeout
        {
            get
            {
                Debug.Assert(Reason == CompletionReason.RequestTimeoutChange);
                Debug.Assert(_timeout.HasValue);
                return _timeout.Value;
            }
        }

        private CompletionData(CompletionReason reason)
        {
            Debug.Assert(reason != CompletionReason.RequestTimeoutChange);
            _timeout = null;
            Reason = reason;
        }

        private CompletionData(TimeSpan timeout)
        {
            _timeout = timeout;
            Reason = CompletionReason.RequestTimeoutChange;
        }

        internal static CompletionData RequestCompleted { get; } = new CompletionData(CompletionReason.RequestCompleted);

        internal static CompletionData RequestError { get; } = new CompletionData(CompletionReason.RequestError);

        internal static CompletionData RequestShutdown { get; } = new CompletionData(CompletionReason.RequestShutdown);

        internal static CompletionData CreateRequestTimeoutChange(TimeSpan timespan) => new CompletionData(timespan);
    }
}

