// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.OLE.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

internal partial class VisualStudioWorkspaceImpl
{
    private abstract class AbstractAddDocumentUndoUnit : AbstractAddRemoveUndoUnit
    {
        protected readonly DocumentInfo DocumentInfo;
        protected readonly SourceText Text;

        protected AbstractAddDocumentUndoUnit(
            VisualStudioWorkspaceImpl workspace,
            DocumentInfo docInfo,
            SourceText text)
            : base(workspace, docInfo.Id.ProjectId)
        {
            DocumentInfo = docInfo;
            Text = text;
        }

        public override void Do(IOleUndoManager pUndoManager)
        {
            var currentSolution = Workspace.CurrentSolution;
            var fromProject = currentSolution.GetProject(FromProjectId);

            if (fromProject != null)
            {
                var updatedProject = AddDocument(fromProject);
                Workspace.TryApplyChanges(updatedProject.Solution);
            }
        }

        protected abstract Project AddDocument(Project fromProject);

        public override void GetDescription(out string pBstr)
            => pBstr = string.Format(FeaturesResources.Add_document_0, DocumentInfo.Name);
    }
}
