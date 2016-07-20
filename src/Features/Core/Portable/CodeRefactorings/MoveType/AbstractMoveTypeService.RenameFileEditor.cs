// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.CodeRefactorings.MoveType
{
    internal abstract partial class AbstractMoveTypeService<TService, TTypeDeclarationSyntax, TNamespaceDeclarationSyntax, TMemberDeclarationSyntax, TCompilationUnitSyntax>
    {
        private class RenameFileEditor : Editor
        {
            public RenameFileEditor(TService service, State state, CancellationToken cancellationToken)
                : base(service, state, cancellationToken)
            {
            }

            internal override Task<IEnumerable<CodeActionOperation>> GetOperationsAsync()
            {
                return Task.FromResult(RenameFileToMatchTypeName());
            }

            /// <summary>
            /// Renames the file to match the type contained in it.
            /// </summary>
            private IEnumerable<CodeActionOperation> RenameFileToMatchTypeName()
            {
                var solution = SemanticDocument.Document.Project.Solution;
                var text = SemanticDocument.Text;
                var oldDocumentId = SemanticDocument.Document.Id;
                var newDocumentId = DocumentId.CreateNewId(SemanticDocument.Document.Project.Id, State.TargetFileNameCandidate);

                // currently, document rename is accomplished by a remove followed by an add.
                // the workspace takes care of resolving conflicts if the document name is not unique in the project
                // by adding numeric suffixes to the new document being added.
                var newSolution = solution.RemoveDocument(oldDocumentId);
                newSolution = newSolution.AddDocument(newDocumentId, State.TargetFileNameCandidate, text);

                return new CodeActionOperation[]
                {
                    new ApplyChangesOperation(newSolution),
                    new OpenDocumentOperation(newDocumentId)
                };
            }
        }
    }
}
