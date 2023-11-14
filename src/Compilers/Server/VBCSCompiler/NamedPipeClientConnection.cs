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
namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal sealed class NamedPipeClientConnection : IClientConnection
    {
        private CancellationTokenSource DisconnectCancellationTokenSource { get; } = new CancellationTokenSource();
        private TaskCompletionSource<object> DisconnectTaskCompletionSource { get; } = new TaskCompletionSource<object>();

        public NamedPipeServerStream Stream { get; }
        public ICompilerServerLogger Logger { get; }
        public bool IsDisposed { get; private set; }

        public Task DisconnectTask => DisconnectTaskCompletionSource.Task;

        internal NamedPipeClientConnection(NamedPipeServerStream stream, ICompilerServerLogger logger)
        {
            Stream = stream;
            Logger = logger;
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
                    Logger.LogException(ex, $"Error closing client connection");
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
            _ = MonitorDisconnectAsync();

            return request;

            async Task MonitorDisconnectAsync()
            {
                try
                {
                    await BuildServerConnection.MonitorDisconnectAsync(Stream, request.RequestId, Logger, DisconnectCancellationTokenSource.Token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, $"Error monitoring disconnect {request.RequestId}");
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
