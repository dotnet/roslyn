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
            private readonly AsyncBatchingDelay _asyncDelay;

            public WorkspaceChangedEventSource(
                ITextBuffer subjectBuffer,
                TaggerDelay delay,
                IAsynchronousOperationListener asyncListener)
                : base(subjectBuffer, delay)
            {
                // That will ensure that even if we get a flurry of workspace events that we
                // only process a tag change once.
                _asyncDelay = new AsyncBatchingDelay(
                    TimeSpan.FromMilliseconds(250),
                    processAsync: cancellationToken =>
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
                => _asyncDelay.RequeueWork();
        }
    }
}
