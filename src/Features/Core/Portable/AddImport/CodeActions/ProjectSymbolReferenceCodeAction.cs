// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.AddImport
{
    internal abstract partial class AbstractAddImportCodeFixProvider<TSimpleNameSyntax>
    {
        private class ProjectSymbolReferenceCodeAction : SymbolReferenceCodeAction
        {
            /// <summary>
            /// The optional id for a <see cref="Project"/> we'd like to add a reference to.
            /// </summary>
            private readonly ProjectId _projectReferenceToAdd;

            public ProjectSymbolReferenceCodeAction(
                Document originalDocument,
                ImmutableArray<TextChange> textChanges,
                string title, ImmutableArray<string> tags,
                CodeActionPriority priority,
                ProjectId projectReferenceToAdd)
                    : base(originalDocument, textChanges, title, tags, priority)
            {
                // We only want to add a project reference if the project the import references
                // is different from the project we started from.
                if (projectReferenceToAdd != originalDocument.Project.Id)
                {
                    _projectReferenceToAdd = projectReferenceToAdd;
                }
            }

            internal override bool PerformFinalApplicabilityCheck
                => _projectReferenceToAdd != null;

            internal override bool IsApplicable(Workspace workspace)
                => _projectReferenceToAdd != null && workspace.CanAddProjectReference(OriginalDocument.Project.Id, _projectReferenceToAdd);

            protected override Project UpdateProject(Project project)
            {
                return _projectReferenceToAdd == null
                    ? project
                    : project.AddProjectReference(new ProjectReference(_projectReferenceToAdd));
            }
        }
    }
}