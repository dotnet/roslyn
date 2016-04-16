// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            {
                this.RaiseChanged();
            }

            protected override void DisconnectFromWorkspace(Workspace workspace)
            {
                this.RaiseChanged();
            }
        }
    }
}
