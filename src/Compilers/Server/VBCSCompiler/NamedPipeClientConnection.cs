// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CommandLine;
using System.Security.AccessControl;

#nullable enable

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal sealed class NamedPipeClientConnection : IClientConnection
    {
        private CancellationTokenSource DisconnectCancellationTokenSource { get; } = new CancellationTokenSource();
        private TaskCompletionSource<object> DisconnectTaskCompletionSource { get; } = new TaskCompletionSource<object>();

        public NamedPipeServerStream Stream { get; }
        public string LoggingIdentifier { get; }
        public bool IsDisposed { get; private set; }

        public Task DisconnectTask => DisconnectTaskCompletionSource.Task;

        internal NamedPipeClientConnection(NamedPipeServerStream stream, string loggingIdentifier)
        {
            Stream = stream;
            LoggingIdentifier = loggingIdentifier;
        }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                try
                {
                    DisconnectTaskCompletionSource.TrySetResult(new object());
                    DisconnectCancellationTokenSource.Cancel();
                    Stream.Close();
                }
                catch (Exception ex)
                {
                    CompilerServerLogger.LogException(ex, $"Error closing client connection {LoggingIdentifier}");
                }

                IsDisposed = true;
            }
        }

        public async Task<BuildRequest> ReadBuildRequestAsync(CancellationToken cancellationToken)
        {
            var request = await BuildRequest.ReadAsync(Stream, cancellationToken).ConfigureAwait(false);

            // Now that we've read data from the stream we can validate the identity.
            if (!NamedPipeUtil.CheckClientElevationMatches(Stream))
            {
                throw new Exception("Client identity does not match server identity.");
            }

            // The result is deliberately discarded here. The idea is to kick off the monitor code and 
            // when it completes it will trigger the task. Don't want to block on that here.
            _ = MonitorDisconnect();

            return request;

            async Task MonitorDisconnect()
            {
                try
                {
                    await BuildServerConnection.MonitorDisconnectAsync(Stream, LoggingIdentifier, DisconnectCancellationTokenSource.Token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    CompilerServerLogger.LogException(ex, $"Error monitoring disconnect {LoggingIdentifier}");
                }
                finally
                {
                    DisconnectTaskCompletionSource.TrySetResult(this);
                }
            }
        }

        public Task WriteBuildResponseAsync(BuildResponse response, CancellationToken cancellationToken) => response.WriteAsync(Stream, cancellationToken);
    }
}
