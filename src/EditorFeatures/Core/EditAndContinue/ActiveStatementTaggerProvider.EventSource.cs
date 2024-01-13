// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal partial class ActiveStatementTaggerProvider
    {
        private sealed class EventSource(ITextBuffer subjectBuffer) : AbstractWorkspaceTrackingTaggerEventSource(subjectBuffer)
        {
            protected override void ConnectToWorkspace(Workspace workspace)
            {
                var trackingService = workspace.Services.GetService<IActiveStatementTrackingService>();
                if (trackingService != null)
                {
                    trackingService.TrackingChanged += RaiseChanged;
                    RaiseChanged();
                }
            }

            protected override void DisconnectFromWorkspace(Workspace workspace)
            {
                var trackingService = workspace.Services.GetService<IActiveStatementTrackingService>();
                if (trackingService != null)
                {
                    trackingService.TrackingChanged -= RaiseChanged;
                    RaiseChanged();
                }
            }
        }
    }
}
