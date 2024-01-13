// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging
{
    internal partial class TaggerEventSources
    {
        private class WorkspaceChangedEventSource : AbstractWorkspaceTrackingTaggerEventSource
        {
            private readonly AsyncBatchingWorkQueue _asyncDelay;

            public WorkspaceChangedEventSource(
                ITextBuffer subjectBuffer,
                IAsynchronousOperationListener asyncListener)
                : base(subjectBuffer)
            {
                // That will ensure that even if we get a flurry of workspace events that we
                // only process a tag change once.
                _asyncDelay = new AsyncBatchingWorkQueue(
                    DelayTimeSpan.Short,
                    processBatchAsync: cancellationToken =>
                    {
                        RaiseChanged();
                        return ValueTaskFactory.CompletedTask;
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

            private void OnWorkspaceChanged(object? sender, WorkspaceChangeEventArgs eventArgs)
                => _asyncDelay.AddWork();
        }
    }
}
