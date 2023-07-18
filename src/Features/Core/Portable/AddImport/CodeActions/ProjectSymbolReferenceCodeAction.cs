// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
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
            /// <summary>
            /// This code action may or may not add a project reference.  If it does, it requires a non document change
            /// (and is thus restricted in which hosts it can run).  If it doesn't, it can run anywhere.
            /// </summary>
            public ProjectSymbolReferenceCodeAction(
                Document originalDocument,
                AddImportFixData fixData)
                : base(originalDocument,
                       fixData,
                       additionalTags: ShouldAddProjectReference(originalDocument, fixData) ? RequiresNonDocumentChangeTags : ImmutableArray<string>.Empty)
            {
                Contract.ThrowIfFalse(fixData.Kind == AddImportFixKind.ProjectSymbol);
            }

            private static bool ShouldAddProjectReference(Document originalDocument, AddImportFixData fixData)
                => fixData.ProjectReferenceToAdd != null && fixData.ProjectReferenceToAdd != originalDocument.Project.Id;

            protected override Task<CodeActionOperation?> UpdateProjectAsync(Project project, bool isPreview, CancellationToken cancellationToken)
            {
                if (!ShouldAddProjectReference(this.OriginalDocument, this.FixData))
                    return SpecializedTasks.Null<CodeActionOperation>();

                var projectWithAddedReference = project.AddProjectReference(new ProjectReference(FixData.ProjectReferenceToAdd));
                var applyOperation = new ApplyChangesOperation(projectWithAddedReference.Solution);
                if (isPreview)
                {
                    return Task.FromResult<CodeActionOperation?>(applyOperation);
                }

                return Task.FromResult<CodeActionOperation?>(new AddProjectReferenceCodeActionOperation(OriginalDocument.Project.Id, FixData.ProjectReferenceToAdd, applyOperation));
            }

            private sealed class AddProjectReferenceCodeActionOperation(ProjectId referencingProject, ProjectId referencedProject, ApplyChangesOperation applyOperation) : CodeActionOperation
            {
                private readonly ProjectId _referencingProject = referencingProject;
                private readonly ProjectId _referencedProject = referencedProject;
                private readonly ApplyChangesOperation _applyOperation = applyOperation;

                internal override bool ApplyDuringTests => true;

                public override void Apply(Workspace workspace, CancellationToken cancellationToken)
                {
                    if (!CanApply(workspace))
                        return;

                    _applyOperation.Apply(workspace, cancellationToken);
                }

                internal override Task<bool> TryApplyAsync(
                    Workspace workspace, Solution originalSolution, IProgressTracker progressTracker, CancellationToken cancellationToken)
                {
                    if (!CanApply(workspace))
                        return SpecializedTasks.False;

                    return _applyOperation.TryApplyAsync(workspace, originalSolution, progressTracker, cancellationToken);
                }

                private bool CanApply(Workspace workspace)
                {
                    return workspace.CanAddProjectReference(_referencingProject, _referencedProject);
                }
            }
        }
    }
}
