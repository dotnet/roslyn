// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging
{
    internal partial class TaggerEventSources
    {
        private class WorkspaceRegistrationChangedEventSource : AbstractWorkspaceTrackingTaggerEventSource
        {
            public WorkspaceRegistrationChangedEventSource(ITextBuffer subjectBuffer, TaggerDelay delay)
                : base(subjectBuffer, delay)
            {
            }

            protected override void ConnectToWorkspace(Workspace workspace)
                => this.RaiseChanged();

            protected override void DisconnectFromWorkspace(Workspace workspace)
                => this.RaiseChanged();
        }
    }
}
