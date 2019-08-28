// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.TestHooks
{
    internal sealed partial class AsynchronousOperationListener : IAsynchronousOperationListener, IAsynchronousOperationWaiter
    {
        private readonly NonReentrantLock _gate = new NonReentrantLock();

        private readonly string _featureName;
        private readonly HashSet<TaskCompletionSource<bool>> _pendingTasks = new HashSet<TaskCompletionSource<bool>>();
        private CancellationTokenSource _expeditedDelayCancellationTokenSource;

        private List<DiagnosticAsyncToken> _diagnosticTokenList = new List<DiagnosticAsyncToken>();
        private int _counter;
        private bool _trackActiveTokens;

        public AsynchronousOperationListener()
            : this(featureName: "noname", enableDiagnosticTokens: false)
        {
        }

        public AsynchronousOperationListener(string featureName, bool enableDiagnosticTokens)
        {
            _featureName = featureName;
            _expeditedDelayCancellationTokenSource = new CancellationTokenSource();
            TrackActiveTokens = Debugger.IsAttached || enableDiagnosticTokens;
        }

        public async Task<bool> Delay(TimeSpan delay, CancellationToken cancellationToken)
        {
            var expeditedDelayCancellationToken = _expeditedDelayCancellationTokenSource.Token;
            using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, expeditedDelayCancellationToken);

            try
            {
                await Task.Delay(delay, cancellationTokenSource.Token).ConfigureAwait(false);
                return true;
            }
            catch (OperationCanceledException) when (expeditedDelayCancellationToken.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                // The cancellation only occurred due to a request to expedite the operation
                return false;
            }
        }

        public IAsyncToken BeginAsyncOperation(string name, object tag = null, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            using (_gate.DisposableWait(CancellationToken.None))
            {
                IAsyncToken asyncToken;
                if (_trackActiveTokens)
                {
                    var token = new DiagnosticAsyncToken(this, name, tag, filePath, lineNumber);
                    _diagnosticTokenList.Add(token);
                    asyncToken = token;
                }
                else
                {
                    asyncToken = new AsyncToken(this);
                }

                return asyncToken;
            }
        }

        private void Increment_NoLock()
        {
            Contract.ThrowIfFalse(_gate.LockHeldByMe());
            _counter++;
        }

        private void Decrement_NoLock(AsyncToken token)
        {
            Contract.ThrowIfFalse(_gate.LockHeldByMe());

            _counter--;
            if (_counter == 0)
            {
                foreach (var task in _pendingTasks)
                {
                    task.SetResult(true);
                }

                _pendingTasks.Clear();

                // Replace the cancellation source used for expediting waits.
                var oldSource = Interlocked.Exchange(ref _expeditedDelayCancellationTokenSource, new CancellationTokenSource());
                oldSource.Dispose();
            }

            if (_trackActiveTokens)
            {
                var i = 0;
                var removed = false;
                while (i < _diagnosticTokenList.Count)
                {
                    if (_diagnosticTokenList[i] == token)
                    {
                        _diagnosticTokenList.RemoveAt(i);
                        removed = true;
                        break;
                    }

                    i++;
                }

                Debug.Assert(removed, "IAsyncToken and Listener mismatch");
            }
        }

        public Task CreateExpeditedWaitTask()
        {
            using (_gate.DisposableWait(CancellationToken.None))
            {
                if (_counter == 0)
                {
                    // There is nothing to wait for, so we are immediately done
                    return Task.CompletedTask;
                }
                else
                {
                    // Use CancelAfter to ensure cancellation callbacks are not synchronously invoked under the _gate.
                    _expeditedDelayCancellationTokenSource.CancelAfter(TimeSpan.Zero);

                    // Calling SetResult on a normal TaskCompletionSource can cause continuations to run synchronously
                    // at that point. That's a problem as that may cause additional code to run while we're holding a lock. 
                    // In order to prevent that, we pass along RunContinuationsAsynchronously in order to ensure that 
                    // all continuations will run at a future point when this thread has released the lock.
                    var source = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    _pendingTasks.Add(source);

                    return source.Task;
                }
            }
        }

        public async Task WaitUntilConditionIsMetAsync(Func<IEnumerable<DiagnosticAsyncToken>, bool> condition)
        {
            Contract.ThrowIfFalse(TrackActiveTokens);

            while (true)
            {
                if (condition(ActiveDiagnosticTokens))
                {
                    break;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(10)).ConfigureAwait(false);
            }
        }

        public bool TrackActiveTokens
        {
            get { return _trackActiveTokens; }
            set
            {
                using (_gate.DisposableWait(CancellationToken.None))
                {
                    if (_trackActiveTokens == value)
                    {
                        return;
                    }

                    _trackActiveTokens = value;
                    _diagnosticTokenList = _trackActiveTokens ? new List<DiagnosticAsyncToken>() : null;
                }
            }
        }

        public bool HasPendingWork
        {
            get
            {
                using (_gate.DisposableWait(CancellationToken.None))
                {
                    return _counter != 0;
                }
            }
        }

        public ImmutableArray<DiagnosticAsyncToken> ActiveDiagnosticTokens
        {
            get
            {
                using (_gate.DisposableWait(CancellationToken.None))
                {
                    if (_diagnosticTokenList == null)
                    {
                        return ImmutableArray<DiagnosticAsyncToken>.Empty;
                    }

                    return _diagnosticTokenList.ToImmutableArray();
                }
            }
        }
    }
}
