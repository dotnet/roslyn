// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.CodeRefactorings.MoveType
{
    internal abstract partial class AbstractMoveTypeService<TService, TTypeDeclarationSyntax, TNamespaceDeclarationSyntax, TMemberDeclarationSyntax, TCompilationUnitSyntax>
    {
        private partial class Editor
        {
            /// <summary>
            /// Renames the file to match the type contained in it.
            /// </summary>
            private IEnumerable<CodeActionOperation> RenameFileToMatchTypeName(Solution solution)
            {
                var text = SemanticDocument.Text;
                var oldDocumentId = SemanticDocument.Document.Id;
                var newDocumentId = DocumentId.CreateNewId(SemanticDocument.Document.Project.Id, _state.TargetFileNameCandidate);

                // currently, document rename is accomplished by a remove followed by an add.
                var newSolution = solution.RemoveDocument(oldDocumentId);
                newSolution = newSolution.AddDocument(newDocumentId, _state.TargetFileNameCandidate, text);

                return new CodeActionOperation[]
                {
                    new ApplyChangesOperation(newSolution),
                    new OpenDocumentOperation(newDocumentId)
                };
            }
        }
    }
}
