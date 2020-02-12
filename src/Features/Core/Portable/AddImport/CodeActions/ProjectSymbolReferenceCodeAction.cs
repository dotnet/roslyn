// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddImport
{
    internal abstract partial class AbstractAddImportFeatureService<TSimpleNameSyntax>
    {
        /// <summary>
        /// Code action for adding an import when we find a symbol in source in either our
        /// starting project, or some other unreferenced project in the solution.  If we 
        /// find a source symbol in a different project, we'll also add a p2p reference when
        /// we apply the code action.
        /// </summary>
        private class ProjectSymbolReferenceCodeAction : SymbolReferenceCodeAction
        {
            public ProjectSymbolReferenceCodeAction(
                Document originalDocument,
                AddImportFixData fixData)
                : base(originalDocument, fixData)
            {
                Contract.ThrowIfFalse(fixData.Kind == AddImportFixKind.ProjectSymbol);
            }

            private bool ShouldAddProjectReference()
                => FixData.ProjectReferenceToAdd != null && FixData.ProjectReferenceToAdd != OriginalDocument.Project.Id;

            internal override bool PerformFinalApplicabilityCheck
                => ShouldAddProjectReference();

            internal override bool IsApplicable(Workspace workspace)
                => ShouldAddProjectReference() &&
                   workspace.CanAddProjectReference(
                    OriginalDocument.Project.Id, FixData.ProjectReferenceToAdd);

            protected override Project UpdateProject(Project project)
            {
                return ShouldAddProjectReference()
                    ? project.AddProjectReference(new ProjectReference(FixData.ProjectReferenceToAdd))
                    : project;
            }
        }
    }
}
