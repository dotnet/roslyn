// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal partial class VisualStudioWorkspaceImpl
    {
        private class RemoveAdditionalDocumentUndoUnit : AbstractRemoveDocumentUndoUnit
        {
            public RemoveAdditionalDocumentUndoUnit(
                VisualStudioWorkspaceImpl workspace,
                DocumentId documentId)
                : base(workspace, documentId)
            {
            }

            protected override IReadOnlyList<DocumentId> GetDocumentIds(Project fromProject)
                => fromProject.AdditionalDocumentIds;

            protected override TextDocument? GetDocument(Solution currentSolution)
                => currentSolution.GetAdditionalDocument(this.DocumentId);
        }
    }
}
