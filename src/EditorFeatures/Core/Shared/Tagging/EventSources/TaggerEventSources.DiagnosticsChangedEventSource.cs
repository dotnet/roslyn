// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging
{
    internal partial class TaggerEventSources
    {
        private class DiagnosticsChangedEventSource(ITextBuffer subjectBuffer, IDiagnosticService service) : AbstractTaggerEventSource
        {
            private readonly ITextBuffer _subjectBuffer = subjectBuffer;
            private readonly IDiagnosticService _service = service;

            private void OnDiagnosticsUpdated(object? sender, DiagnosticsUpdatedArgs e)
            {
                var documentId = e.Workspace.GetDocumentIdInCurrentContext(_subjectBuffer.AsTextContainer());

                if (documentId == e.DocumentId)
                {
                    this.RaiseChanged();
                }
            }

            public override void Connect()
                => _service.DiagnosticsUpdated += OnDiagnosticsUpdated;

            public override void Disconnect()
                => _service.DiagnosticsUpdated -= OnDiagnosticsUpdated;
        }
    }
}
