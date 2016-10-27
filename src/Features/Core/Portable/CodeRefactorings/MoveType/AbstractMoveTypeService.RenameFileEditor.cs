// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.CodeRefactorings.MoveType
{
    internal abstract partial class AbstractMoveTypeService<TService, TTypeDeclarationSyntax, TNamespaceDeclarationSyntax, TMemberDeclarationSyntax, TCompilationUnitSyntax>
    {
        private class RenameFileEditor : Editor
        {
            public RenameFileEditor(TService service, State state, string fileName, CancellationToken cancellationToken)
                : base(service, state, fileName, cancellationToken)
            {
            }

            internal override Task<ImmutableArray<CodeActionOperation>> GetOperationsAsync()
            {
                return Task.FromResult(RenameFileToMatchTypeName());
            }

            /// <summary>
            /// Renames the file to match the type contained in it.
            /// </summary>
            private ImmutableArray<CodeActionOperation> RenameFileToMatchTypeName()
            {
                var oldDocument = SemanticDocument.Document;
                var solution = oldDocument.Project.Solution;
                var text = SemanticDocument.Text;
                var oldDocumentId = oldDocument.Id;
                var newDocumentId = DocumentId.CreateNewId(oldDocument.Project.Id, FileName);

                // currently, document rename is accomplished by a remove followed by an add.
                // the workspace takes care of resolving conflicts if the document name is not unique in the project
                // by adding numeric suffixes to the new document being added.
                var newSolution = solution.RemoveDocument(oldDocumentId);
                newSolution = newSolution.AddDocument(newDocumentId, FileName, text, oldDocument.Folders);

                return ImmutableArray.Create<CodeActionOperation>(
                    new ApplyChangesOperation(newSolution),
                    new OpenDocumentOperation(newDocumentId, activateIfAlreadyOpen: true));
            }
        }
    }
}
