// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Shared.TestHooks
{
    internal abstract partial class AsynchronousOperationListener : IAsynchronousOperationListener, IAsynchronousOperationWaiter
    {
        private readonly object gate = new object();

        private readonly HashSet<TaskCompletionSource<bool>> pendingTasks = new HashSet<TaskCompletionSource<bool>>();

        private int counter;
        private bool trackActiveTokens = false;
        private HashSet<DiagnosticAsyncToken> activeDiagnosticTokens = new HashSet<DiagnosticAsyncToken>();

        public IAsyncToken BeginAsyncOperation(string name, object tag = null)
        {
            lock (gate)
            {
                if (trackActiveTokens)
                {
                    var token = new DiagnosticAsyncToken(this, name, tag);
                    activeDiagnosticTokens.Add(token);
                    return token;
                }
                else
                {
                    return new AsyncToken(this);
                }
            }
        }

        private void Increment()
        {
            lock (gate)
            {
                counter++;
            }
        }

        private void Decrement(AsyncToken token)
        {
            lock (gate)
            {
                counter--;
                if (counter == 0)
                {
                    foreach (var task in pendingTasks)
                    {
                        task.SetResult(true);
                    }

                    pendingTasks.Clear();
                }

                if (trackActiveTokens)
                {
                    var diagnosticAsyncToken = token as DiagnosticAsyncToken;

                    if (diagnosticAsyncToken != null)
                    {
                        activeDiagnosticTokens.Remove(diagnosticAsyncToken);
                    }
                }
            }
        }

        public virtual Task CreateWaitTask()
        {
            lock (gate)
            {
                var source = new TaskCompletionSource<bool>();
                if (counter == 0)
                {
                    // There is nothing to wait for, so we are immediately done
                    source.SetResult(true);
                }
                else
                {
                    pendingTasks.Add(source);
                }

                return source.Task;
            }
        }

        public bool TrackActiveTokens
        {
            get
            {
                return trackActiveTokens;
            }

            set
            {
                lock (gate)
                {
                    if (trackActiveTokens == value)
                    {
                        return;
                    }

                    trackActiveTokens = value;

                    if (trackActiveTokens)
                    {
                        activeDiagnosticTokens = new HashSet<DiagnosticAsyncToken>();
                    }
                    else
                    {
                        activeDiagnosticTokens = null;
                    }
                }
            }
        }

        public bool HasPendingWork
        {
            get
            {
                return counter != 0;
            }
        }

        public ImmutableArray<DiagnosticAsyncToken> ActiveDiagnosticTokens
        {
            get
            {
                lock (gate)
                {
                    if (activeDiagnosticTokens == null)
                    {
                        return ImmutableArray<DiagnosticAsyncToken>.Empty;
                    }

                    return activeDiagnosticTokens.ToImmutableArray();
                }
            }
        }
    }
}
