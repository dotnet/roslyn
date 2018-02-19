﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

            protected override TextDocument GetDocument(Solution currentSolution)
                => currentSolution.GetAdditionalDocument(this.DocumentId);
        }
    }
}
