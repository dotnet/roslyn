// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
        internal enum OperationKind
        {
            MoveType,
            RenameType,
            RenameFile
        }

        public bool ShouldAnalyze(SyntaxNode root, TextSpan span)
        {
            return GetNodetoAnalyze(root, span) is TTypeDeclarationSyntax;
        }

        protected virtual SyntaxNode GetNodetoAnalyze(SyntaxNode root, TextSpan span)
        {
            return root.FindNode(span);
        }

        private bool IsNestedType(TTypeDeclarationSyntax typeNode) =>
            typeNode.Parent is TTypeDeclarationSyntax;

        /// <summary>
        /// checks if there is a single top level type declaration in a document
        /// </summary>
        private bool IsSingleTypeDeclarationInSourceDocument(SyntaxNode root) =>
            root.DescendantNodes()
            .OfType<TTypeDeclarationSyntax>()
            .Count() == 1;

        public async Task<CodeRefactoring> GetRefactoringAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            var semanticDocument = await SemanticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var state = State.Generate((TService)this, semanticDocument, textSpan, cancellationToken);
            if (state == null)
            {
                return null;
            }

            var actions = CreateActions(state, cancellationToken);
            if (actions.Count == 0)
            {
                return null;
            }

            return new CodeRefactoring(null, actions);
        }

        private List<CodeAction> CreateActions(State state, CancellationToken cancellationToken)
        {
            var actions = new List<CodeAction>();
            var singleType = IsSingleTypeDeclarationInSourceDocument(state.SemanticDocument.Root);

            if (singleType)
            {
                // one type declaration in current document. No moving around required, just sync
                // document name and type name by offering rename in both directions between type and document.
                actions.Add(GetCodeAction(state, operationKind: OperationKind.RenameFile));
                actions.Add(GetCodeAction(state, operationKind: OperationKind.RenameType));
            }
            else
            {
                // multiple type declarations in current document. so, move to new file.
                actions.Add(GetCodeAction(state, operationKind: OperationKind.MoveType));
            }

            return actions;
        }

        private CodeAction GetCodeAction(
            State state,
            OperationKind operationKind)
        {
            return new MoveTypeCodeAction((TService)this, state, operationKind);
        }
    }
}
