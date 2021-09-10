// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.OLE.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal partial class VisualStudioWorkspaceImpl
    {
        private class RemoveProjectReferenceUndoUnit : AbstractAddRemoveUndoUnit
        {
            private readonly ProjectId _toProjectId;

            public RemoveProjectReferenceUndoUnit(
                VisualStudioWorkspaceImpl workspace,
                ProjectId fromProjectId,
                ProjectId toProjectId)
                : base(workspace, fromProjectId)
            {
                _toProjectId = toProjectId;
            }

            public override void Do(IOleUndoManager pUndoManager)
            {
                var currentSolution = Workspace.CurrentSolution;
                var fromProject = currentSolution.GetProject(FromProjectId);
                var toProject = currentSolution.GetProject(_toProjectId);

                if (fromProject != null &&
                    toProject != null &&
                    fromProject.ProjectReferences.Any(p => p.ProjectId == _toProjectId))
                {
                    var updatedProject = fromProject.RemoveProjectReference(new ProjectReference(_toProjectId));
                    Workspace.TryApplyChanges(updatedProject.Solution);
                }
            }

            public override void GetDescription(out string pBstr)
            {
                var currentSolution = Workspace.CurrentSolution;
                var toProject = currentSolution.GetProject(_toProjectId);
                var projectName = toProject?.Name ?? "";
                pBstr = string.Format(FeaturesResources.Remove_reference_to_0, projectName);
            }
        }
    }
}
