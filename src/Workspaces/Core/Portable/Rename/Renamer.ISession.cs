// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Rename
{
    public static partial class Renamer
    {
        /// <summary>
        /// Represents a session kept alive between a host and the OOP process where renames can then occur.  This
        /// session improves the efficiency of rename by pinning the initial solution on the OOP side so that all calls
        /// to <see cref="IRemoteRenamerService.ResolveConflictsAsync"/> end up calling to OOP without having to
        /// rehydrate that solution and potentially recreate compilations.
        /// </summary>
        internal interface IKeepAliveSession : IDisposable
        {
        }

        /// <summary>
        /// No op impl of the API for when we're just performing rename in process.
        /// </summary>
        private sealed class NoOpKeepAliveSession : IKeepAliveSession
        {
            public static readonly IKeepAliveSession Instance = new NoOpKeepAliveSession();

            private NoOpKeepAliveSession()
            {
            }

            public void Dispose()
            {
            }
        }

        private sealed class KeepAliveSession : IKeepAliveSession
        {
            private readonly ReferenceCountedDisposable<KeepAliveConnection> _keepAliveConnection;

            public KeepAliveSession(ReferenceCountedDisposable<KeepAliveConnection> keepAliveConnection)
            {
                _keepAliveConnection = keepAliveConnection;
            }

            ~KeepAliveSession()
            {
                if (!Environment.HasShutdownStarted)
                    throw new InvalidOperationException("Dispose not called on KeepAliveSession");
            }

            public void Dispose()
            {
                GC.SuppressFinalize(this);
                _keepAliveConnection.Dispose();
            }
        }

        internal sealed class KeepAliveConnection : IDisposable // , IKeepAliveConnection
        {
            /// <summary>
            /// Cancellation token we trigger once we're disposed.  When it is canceled, then oop will see that has
            /// happened, and can return from the call to <see cref="IRemoteRenamerService.KeepAliveAsync"/> which it is
            /// stuck inside of, keeping the current solution pinned.
            /// </summary>
            private readonly CancellationTokenSource _cancellationTokenSource = new();

            /// <summary>
            /// Used to keep track if <see cref="KeepAliveAsync"/> has been called by the oop server.
            /// </summary>
            private readonly TaskCompletionSource<bool> _keepAliveCalledSource = new();

            public CancellationToken CancellationToken => _cancellationTokenSource.Token;
            public Task KeepAliveCalledTask => _keepAliveCalledSource.Task;

            public void Dispose()
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
            }

            /// <summary>
            /// Callback from OOP into the host side.  This allows OOP to tell us when it has successfully hydrated and
            /// pinned the solution snapshot on its side and is now holding it until <see
            /// cref="_cancellationTokenSource"/> is canceled.
            /// </summary>
            public ValueTask KeepAliveAsync()
            {
                _keepAliveCalledSource.TrySetResult(false);
                return default;
            }
        }
    }
}
