// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Shared.Utilities;
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

            protected override Task<CodeActionOperation?> UpdateProjectAsync(Project project, bool isPreview, CancellationToken cancellationToken)
            {
                if (!ShouldAddProjectReference())
                {
                    return SpecializedTasks.Null<CodeActionOperation>();
                }

                var projectWithAddedReference = project.AddProjectReference(new ProjectReference(FixData.ProjectReferenceToAdd));
                var applyOperation = new ApplyChangesOperation(projectWithAddedReference.Solution);
                if (isPreview)
                {
                    return Task.FromResult<CodeActionOperation?>(applyOperation);
                }

                return Task.FromResult<CodeActionOperation?>(new AddProjectReferenceCodeActionOperation(OriginalDocument.Project.Id, FixData.ProjectReferenceToAdd, applyOperation));
            }

            private sealed class AddProjectReferenceCodeActionOperation : CodeActionOperation
            {
                private readonly ProjectId _referencingProject;
                private readonly ProjectId _referencedProject;
                private readonly ApplyChangesOperation _applyOperation;

                public AddProjectReferenceCodeActionOperation(ProjectId referencingProject, ProjectId referencedProject, ApplyChangesOperation applyOperation)
                {
                    _referencingProject = referencingProject;
                    _referencedProject = referencedProject;
                    _applyOperation = applyOperation;
                }

                internal override bool ApplyDuringTests => true;

                public override void Apply(Workspace workspace, CancellationToken cancellationToken)
                {
                    if (!CanApply(workspace))
                        return;

                    _applyOperation.Apply(workspace, cancellationToken);
                }

                internal override bool TryApply(Workspace workspace, IProgressTracker progressTracker, CancellationToken cancellationToken)
                {
                    if (!CanApply(workspace))
                        return false;

                    return _applyOperation.TryApply(workspace, progressTracker, cancellationToken);
                }

                private bool CanApply(Workspace workspace)
                {
                    return workspace.CanAddProjectReference(_referencingProject, _referencedProject);
                }
            }
        }
    }
}
