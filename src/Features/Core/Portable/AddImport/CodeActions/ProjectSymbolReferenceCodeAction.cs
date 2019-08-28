// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
