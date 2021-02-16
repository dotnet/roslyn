// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging
{
    internal abstract class AbstractDiagnosticsChangedEventSource : AbstractTaggerEventSource
    {
        private readonly ITextBuffer _subjectBuffer;

        public AbstractDiagnosticsChangedEventSource(ITextBuffer subjectBuffer, TaggerDelay delay)
            : base(delay)
        {
            _subjectBuffer = subjectBuffer;
        }

        protected void OnDiagnosticsUpdated(object? sender, DiagnosticsUpdatedArgs e)
        {
            var document = _subjectBuffer.AsTextContainer().GetOpenDocumentInCurrentContext();

            if (document != null && document.Project.Solution.Workspace == e.Workspace && document.Id == e.DocumentId)
            {
                this.RaiseChanged();
            }
        }
    }
}
