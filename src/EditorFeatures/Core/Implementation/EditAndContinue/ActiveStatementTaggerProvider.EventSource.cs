// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.EditAndContinue
{
    internal partial class ActiveStatementTaggerProvider
    {
        private sealed class EventSource : AbstractWorkspaceTrackingTaggerEventSource
        {
            public EventSource(ITextBuffer subjectBuffer, TaggerDelay delay)
                : base(subjectBuffer, delay)
            {
            }

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
