// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal partial class VisualStudioWorkspaceImpl
    {
        private class AddAnalyzerConfigDocumentUndoUnit : AbstractAddDocumentUndoUnit
        {
            public AddAnalyzerConfigDocumentUndoUnit(
                VisualStudioWorkspaceImpl workspace,
                DocumentInfo docInfo,
                SourceText text)
                : base(workspace, docInfo, text)
            {
            }

            protected override Project AddDocument(Project fromProject)
                => fromProject.AddAnalyzerConfigDocument(DocumentInfo.Name, Text, DocumentInfo.Folders, DocumentInfo.FilePath).Project;
        }
    }
}
