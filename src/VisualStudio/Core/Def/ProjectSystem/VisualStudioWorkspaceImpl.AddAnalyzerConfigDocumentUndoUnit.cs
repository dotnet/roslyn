// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

internal partial class VisualStudioWorkspaceImpl
{
    private sealed class AddAnalyzerConfigDocumentUndoUnit : AbstractAddDocumentUndoUnit
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
