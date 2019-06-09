﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Rename;

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

            public override Task<ImmutableArray<CodeActionOperation>> GetOperationsAsync()
                => Task.FromResult(RenameFileToMatchTypeName());

            public override Task<Solution> GetModifiedSolutionAsync()
            {
                var oldDocument = SemanticDocument.Document;
                var newDocumentId = DocumentId.CreateNewId(oldDocument.Project.Id, FileName);

                var modifiedSolution = oldDocument.Project.Solution
                    .RemoveDocument(oldDocument.Id)
                    .AddDocument(newDocumentId, FileName, SemanticDocument.Text, oldDocument.Folders);

                return Task.FromResult(modifiedSolution);
            }

            /// <summary>
            /// Renames the file to match the type contained in it.
            /// </summary>
            private ImmutableArray<CodeActionOperation> RenameFileToMatchTypeName()
            {
                var documentId = SemanticDocument.Document.Id;
                var oldSolution = SemanticDocument.Document.Project.Solution;
                var newSolution = oldSolution.WithDocumentName(documentId, FileName);

                return ImmutableArray.Create<CodeActionOperation>(
                    new ApplyChangesOperation(newSolution));
            }
        }
    }
}
