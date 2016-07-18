// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeRefactorings.MoveType
{
    internal abstract partial class AbstractMoveTypeService<TService, TTypeDeclarationSyntax, TNamespaceDeclarationSyntax, TMemberDeclarationSyntax, TCompilationUnitSyntax> :
        IMoveTypeService
        where TService : AbstractMoveTypeService<TService, TTypeDeclarationSyntax, TNamespaceDeclarationSyntax, TMemberDeclarationSyntax, TCompilationUnitSyntax>
        where TTypeDeclarationSyntax : SyntaxNode
        where TNamespaceDeclarationSyntax : SyntaxNode
        where TMemberDeclarationSyntax : SyntaxNode
    {
        protected virtual SyntaxNode GetNodetoAnalyze(SyntaxNode root, TextSpan span)
        {
            return root.FindNode(span);
        }

        private bool IsNestedType(TTypeDeclarationSyntax typeNode) =>
            typeNode.Parent is TTypeDeclarationSyntax;

        private bool IsSingleTypeDeclarationInSourceDocument(SyntaxNode root) =>
            root.DescendantNodes().OfType<TTypeDeclarationSyntax>().Count() == 1;

        /// <remarks>
        /// currently we generate into the same project. 
        /// so, if type name matches file name, target file name is already present.
        /// </remarks>
        private bool ProjectContainsTargetFile(Project project, string targetFileName, string typeName, string sourceDocumentName) =>
            TypeNameMatchesDocumentName(typeName, sourceDocumentName) ||
            project.ContainsDocument(DocumentId.CreateNewId(project.Id, targetFileName));

        private bool TypeNameMatchesDocumentName(string fileName, string typeName) =>
            string.Equals(fileName, typeName, StringComparison.CurrentCultureIgnoreCase);

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
            var isNestedType = IsNestedType(state.TypeNode);
            var singleType = IsSingleTypeDeclarationInSourceDocument(state.SemanticDocument.Root);
            var typeSymbol = (ITypeSymbol)state.SemanticDocument.SemanticModel.GetDeclaredSymbol(state.TypeNode, cancellationToken);

            if (singleType)
            {
                // one type declaration in current document. No moving around required, just sync
                // document name and type name by offering rename in both directions between type and document.
                AddSimpleCodeAction(actions, state, renameFile: true);
                AddSimpleCodeAction(actions, state, renameType: true);
            }
            else
            {
                // multiple type declarations in current document. so, move to new file.
                AddSimpleCodeAction(actions, state);
            }

            return actions;
        }

        private void AddSimpleCodeAction(
            List<CodeAction> actions,
            State state,
            bool renameFile = false,
            bool renameType = false)
        {
            actions.Add(
                new MoveTypeCodeAction(
                    (TService)this, state, renameFile, renameType));
        }
    }
}
