// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging
{
    internal partial class TaggerEventSources
    {
        private class WorkspaceChangedEventSource : AbstractWorkspaceTrackingTaggerEventSource
        {
            private readonly AsyncBatchingWorkQueue<bool> _workQueue;

            public WorkspaceChangedEventSource(
                ITextBuffer subjectBuffer,
                TaggerDelay delay,
                IAsynchronousOperationListener asyncListener)
                : base(subjectBuffer, delay)
            {
                // Batch items so that if we get a flurry of notifications, we'll only process them all once every short while.
                _workQueue = new AsyncBatchingWorkQueue<bool>(
                    TimeSpan.FromMilliseconds(250),
                    addItemsToBatch: (batch, _) =>
                    {
                        // We only need to keep track of a single item since we don't care what type of event it was.
                        batch.Clear();
                        batch.Add(true);
                    },
                    processBatchAsync: (_1, _2) =>
                    {
                        RaiseChanged();
                        return Task.CompletedTask;
                    },
                    asyncListener,
                    CancellationToken.None);
            }

            protected override void ConnectToWorkspace(Workspace workspace)
            {
                workspace.WorkspaceChanged += OnWorkspaceChanged;
                this.RaiseChanged();
            }

            protected override void DisconnectFromWorkspace(Workspace workspace)
            {
                workspace.WorkspaceChanged -= OnWorkspaceChanged;
                this.RaiseChanged();
            }

            private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs eventArgs)
                => _workQueue.AddWork(true);
        }
    }
}
