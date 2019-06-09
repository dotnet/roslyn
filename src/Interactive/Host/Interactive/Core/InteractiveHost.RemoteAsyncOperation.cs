// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Runtime.Remoting;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Interactive
{
    internal partial class InteractiveHost
    {
        internal class RemoteAsyncOperation<TResult> : MarshalByRefObject
        {
            private readonly RemoteService _remoteService;
            private readonly TaskCompletionSource<TResult> _completion;
            private readonly EventHandler _processExitedHandler;

            internal RemoteAsyncOperation(RemoteService service)
            {
                Debug.Assert(service != null);

                _remoteService = service;
                _completion = new TaskCompletionSource<TResult>();
                _processExitedHandler = new EventHandler((_, __) => ProcessExited());
            }

            public override object InitializeLifetimeService()
            {
                return null;
            }

            public Task<TResult> AsyncExecute(Action<Service, RemoteAsyncOperation<TResult>> action)
            {
                try
                {
                    // async call to remote process:
                    action(_remoteService.Service, this);

                    _remoteService.Process.Exited += _processExitedHandler;
                    if (!_remoteService.Process.IsAlive())
                    {
                        ProcessExited();
                    }

                    return _completion.Task;
                }
                catch (RemotingException) when (!_remoteService.Process.IsAlive())
                {
                    // the operation might have terminated the process:
                    ProcessExited();
                    return _completion.Task;
                }
            }

            /// <summary>
            /// Might be called remotely from the service.
            /// </summary>
            /// <returns>Returns true if the operation hasn't been completed until this call.</returns>
            public void Completed(TResult result)
            {
                _remoteService.Process.Exited -= _processExitedHandler;
                SetResult(result);
            }

            private void ProcessExited()
            {
                Completed(result: default(TResult));
            }

            public void SetResult(TResult result)
            {
                // Warning: bug 9466 describes a rare race condition where this method can be called
                // more than once. If you just call SetResult the second time, it will throw an
                // InvalidOperationException saying: An attempt was made to transition a task to a final
                // state when it had already completed. To work around it without complicated locks, we
                // just call TrySetResult which in case where the Task is already in one of the three
                // states (RunToCompletion, Faulted, Canceled) will just do nothing instead of throwing.
                _completion.TrySetResult(result);
            }
        }
    }
}
