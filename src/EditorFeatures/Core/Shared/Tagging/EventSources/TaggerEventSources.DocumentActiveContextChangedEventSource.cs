// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging;

internal partial class TaggerEventSources
{
    private class DocumentActiveContextChangedEventSource(ITextBuffer subjectBuffer) : AbstractWorkspaceTrackingTaggerEventSource(subjectBuffer)
    {
        protected override void ConnectToWorkspace(Workspace workspace)
            => workspace.DocumentActiveContextChanged += OnDocumentActiveContextChanged;

        protected override void DisconnectFromWorkspace(Workspace workspace)
            => workspace.DocumentActiveContextChanged -= OnDocumentActiveContextChanged;

        private void OnDocumentActiveContextChanged(object? sender, DocumentActiveContextChangedEventArgs e)
        {
            var document = SubjectBuffer.AsTextContainer().GetOpenDocumentInCurrentContext();

            if (document != null && document.Id == e.NewActiveContextDocumentId)
            {
                this.RaiseChanged();
            }
        }
    }
}
