// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
            private readonly IAsynchronousOperationListener _listener;

            public WorkspaceChangedEventSource(
                ITextBuffer subjectBuffer,
                TaggerDelay delay,
                IAsynchronousOperationListener listener)
                : base(subjectBuffer, delay)
            {
                _listener = listener;
                _workQueue = new AsyncBatchingWorkQueue<bool>(
                    TimeSpan.FromMilliseconds(250),
                    )
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
            {
                RaiseChanged();
            }
        }
    }
}
