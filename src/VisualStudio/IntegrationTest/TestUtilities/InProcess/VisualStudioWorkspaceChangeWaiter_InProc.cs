// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    public class VisualStudioWorkspaceChangeWaiter_InProc : IDisposable
    {
        private readonly Workspace _workspace;
        private readonly WorkspaceChangeKind _changeKind;
        private readonly int _numberOfChangesToWaitFor;
        private volatile int _numberOfChangesSeen;
        private readonly TaskCompletionSource<object> _taskCompletionSource
            = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

        public VisualStudioWorkspaceChangeWaiter_InProc(Workspace workspace, WorkspaceChangeKind changeKind, int numberOfChanges)
        {
            if (numberOfChanges < 1)
            {
                throw new ArgumentException($"{nameof(numberOfChanges)} must be a positive integer.");
            }

            _workspace = workspace;
            _changeKind = changeKind;
            _numberOfChangesToWaitFor = numberOfChanges;

            _workspace.WorkspaceChanged += OnWorkspaceChanged;
        }

        public void Dispose()
        {
            _workspace.WorkspaceChanged -= OnWorkspaceChanged;

            // Nobody should be waiting for this at this point, but just in case we'll give a clear exception
            // just in case.
            _taskCompletionSource.TrySetException(
                new Exception($"The {nameof(VisualStudioWorkspaceChangeWaiter_InProc)} never observed a {_changeKind} before being disposed."));
        }

        private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            if (e.Kind == _changeKind)
            {
                if (Interlocked.Increment(ref _numberOfChangesSeen) == _numberOfChangesToWaitFor)
                {
                    _taskCompletionSource.TrySetResult(null);
                    _workspace.WorkspaceChanged -= OnWorkspaceChanged;
                }
            }
        }

        internal void WaitForChange(TimeSpan timeout)
        {
            _taskCompletionSource.Task.Wait(timeout);
        }
    }
}
