// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeRefactorings.MoveType
{
    internal abstract partial class AbstractMoveTypeService<TService, TTypeDeclarationSyntax, TNamespaceDeclarationSyntax, TMemberDeclarationSyntax, TCompilationUnitSyntax> :
        IMoveTypeService
        where TService : AbstractMoveTypeService<TService, TTypeDeclarationSyntax, TNamespaceDeclarationSyntax, TMemberDeclarationSyntax, TCompilationUnitSyntax>
        where TTypeDeclarationSyntax : SyntaxNode
        where TNamespaceDeclarationSyntax : SyntaxNode
        where TMemberDeclarationSyntax : SyntaxNode
        where TCompilationUnitSyntax : SyntaxNode
    {
        private enum OperationKind
        {
            MoveType,
            RenameType,
            RenameFile
        }

        protected bool ShouldAnalyze(SyntaxNode root, TextSpan span)
        {
            return GetNodeToAnalyze(root, span) is TTypeDeclarationSyntax;
        }

        protected virtual SyntaxNode GetNodeToAnalyze(SyntaxNode root, TextSpan span)
        {
            return root.FindNode(span);
        }

        private bool IsNestedType(TTypeDeclarationSyntax typeNode) =>
            typeNode.Parent is TTypeDeclarationSyntax;

        /// <summary>
        /// checks if there is a single top level type declaration in a document
        /// </summary>
        private bool MultipleTopLevelTypeDeclarationInSourceDocument(SyntaxNode root) =>
            root.DescendantNodes(n => (n is TCompilationUnitSyntax || n is TNamespaceDeclarationSyntax))
            .OfType<TTypeDeclarationSyntax>()
            .Count() > 1;

        public async Task<ImmutableArray<CodeAction>> GetRefactoringAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (!ShouldAnalyze(root, textSpan))
            {
                return default(ImmutableArray<CodeAction>);
            }

            var semanticDocument = await SemanticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var state = State.Generate((TService)this, semanticDocument, textSpan, cancellationToken);
            if (state == null)
            {
                return default(ImmutableArray<CodeAction>);
            }

            var actions = CreateActions(state, cancellationToken);

            Debug.Assert(actions.Count() != 0, "No code actions found for MoveType Refactoring");
            return actions;
        }

        private ImmutableArray<CodeAction> CreateActions(State state, CancellationToken cancellationToken)
        {
            var actions = new List<CodeAction>();
            var manyTypes = MultipleTopLevelTypeDeclarationInSourceDocument(state.SemanticDocument.Root);

            if (manyTypes || IsNestedType(state.TypeNode))
            {
                // If there are multiple type declarations in current document. offer, move to new file.
                // Or if this is a nested type, offer to move to new file.
                actions.Add(GetCodeAction(state, operationKind: OperationKind.MoveType));
            }
            else
            {
                // one type declaration in current document. No moving around required, just sync
                // document name and type name by offering rename in both directions between type and document.
                actions.Add(GetCodeAction(state, operationKind: OperationKind.RenameFile));

                // only if the document name can be legal identifier in the language,
                // offer to rename type with document name
                if (state.IsDocumentNameAValidIdentifier)
                {
                    actions.Add(GetCodeAction(state, operationKind: OperationKind.RenameType));
                }
            }

            return actions.ToImmutableArray();
        }

        private CodeAction GetCodeAction(
            State state,
            OperationKind operationKind)
        {
            return new MoveTypeCodeAction((TService)this, state, operationKind);
        }
    }
}
