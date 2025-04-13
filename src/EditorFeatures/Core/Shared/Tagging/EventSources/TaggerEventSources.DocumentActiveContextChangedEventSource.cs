// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging;

internal partial class TaggerEventSources
{
    private sealed class DocumentActiveContextChangedEventSource(ITextBuffer subjectBuffer) : AbstractWorkspaceTrackingTaggerEventSource(subjectBuffer)
    {
        private IDisposable? _documentActiveContextChangedDisposer;

        protected override void ConnectToWorkspace(Workspace workspace)
        {
            _documentActiveContextChangedDisposer = workspace.RegisterDocumentActiveContextChangedHandler(OnDocumentActiveContextChangedAsync);
        }

        protected override void DisconnectFromWorkspace(Workspace workspace)
            => _documentActiveContextChangedDisposer?.Dispose();

        private Task OnDocumentActiveContextChangedAsync(DocumentActiveContextChangedEventArgs e, CancellationToken cancellationToken)
        {
            var document = SubjectBuffer.AsTextContainer().GetOpenDocumentInCurrentContext();

            if (document != null && document.Id == e.NewActiveContextDocumentId)
            {
                this.RaiseChanged();
            }

            return Task.CompletedTask;
        }
    }
}
