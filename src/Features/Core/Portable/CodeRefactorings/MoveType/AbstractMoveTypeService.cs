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

        protected virtual SyntaxNode GetNodeToAnalyze(SyntaxNode root, TextSpan span) => root.FindNode(span);

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

        private bool ShouldAnalyze(SyntaxNode root, TextSpan span) =>
            GetNodeToAnalyze(root, span) is TTypeDeclarationSyntax;

        private ImmutableArray<CodeAction> CreateActions(State state, CancellationToken cancellationToken)
        {
            var actions = new List<CodeAction>();
            var manyTypes = MultipleTopLevelTypeDeclarationInSourceDocument(state.SemanticDocument.Root);

            // (1) Add Move type to new file code action:
            // case 1: There are multiple type declarations in current document. offer, move to new file.
            // case 2: This is a nested type, offer to move to new file.
            // case 3: If there is a single type decl in current file, *do not* offer move to new file.
            if (manyTypes || IsNestedType(state.TypeNode))
            {
                actions.Add(GetCodeAction(state, operationKind: OperationKind.MoveType));
            }

            // (2) Add rename file and rename type code actions:
            // Case: No type declaration in file matches the file name.
            if (!AnyTopLevelTypeMatchesDocumentName(state, cancellationToken))
            {
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

        private CodeAction GetCodeAction(State state, OperationKind operationKind) =>
            new MoveTypeCodeAction((TService)this, state, operationKind);

        private bool IsNestedType(TTypeDeclarationSyntax typeNode) =>
            typeNode.Parent is TTypeDeclarationSyntax;

        /// <summary>
        /// checks if there is a single top level type declaration in a document
        /// </summary>
        /// <remarks>
        /// optimized for perf, uses Skip(1).Any() instead of Count() > 1
        /// </remarks>
        private bool MultipleTopLevelTypeDeclarationInSourceDocument(SyntaxNode root) =>
            TopLevelTypeDeclarations(root).Skip(1).Any();

        private static IEnumerable<TTypeDeclarationSyntax> TopLevelTypeDeclarations(SyntaxNode root) =>
            root.DescendantNodes(n => (n is TCompilationUnitSyntax || n is TNamespaceDeclarationSyntax))
                .OfType<TTypeDeclarationSyntax>();

        private static bool AnyTopLevelTypeMatchesDocumentName(State state, CancellationToken cancellationToken)
        {
            var root = state.SemanticDocument.Root;
            var semanticModel = state.SemanticDocument.SemanticModel;

            return TopLevelTypeDeclarations(root).Any(
                typeDeclaration => 
                {
                    var typeName = semanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken).Name;
                    return string.Equals(state.DocumentName, typeName, StringComparison.CurrentCulture);
                });
        }
    }
}
