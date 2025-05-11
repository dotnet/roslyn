﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging;

internal partial class TaggerEventSources
{
    private sealed class DocumentActiveContextChangedEventSource(ITextBuffer subjectBuffer) : AbstractWorkspaceTrackingTaggerEventSource(subjectBuffer)
    {
        private WorkspaceEventRegistration? _documentActiveContextChangedDisposer;

        // Require main thread on the callback as RaiseChanged implementors may have main thread dependencies.
        protected override void ConnectToWorkspace(Workspace workspace)
            => _documentActiveContextChangedDisposer = workspace.RegisterDocumentActiveContextChangedHandler(OnDocumentActiveContextChanged, WorkspaceEventOptions.RequiresMainThreadOptions);

        protected override void DisconnectFromWorkspace(Workspace workspace)
            => _documentActiveContextChangedDisposer?.Dispose();

        private void OnDocumentActiveContextChanged(DocumentActiveContextChangedEventArgs e)
        {
            var document = SubjectBuffer.AsTextContainer().GetOpenDocumentInCurrentContext();

            if (document != null && document.Id == e.NewActiveContextDocumentId)
                this.RaiseChanged();
        }
    }
}
