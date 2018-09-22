// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Utilities;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;

namespace Microsoft.CodeAnalysis.IntroduceUsingStatement
{
    internal abstract class AbstractIntroduceUsingStatementCodeRefactoringProvider<TStatementSyntax, TLocalDeclarationSyntax, TBlockSyntax> : CodeRefactoringProvider
        where TStatementSyntax : SyntaxNode
        where TLocalDeclarationSyntax : TStatementSyntax
        where TBlockSyntax : SyntaxNode
    {
        protected abstract string CodeActionTitle { get; }

        protected abstract SyntaxList<TStatementSyntax> GetStatements(TBlockSyntax blockSyntax);

        protected abstract TBlockSyntax WithStatements(TBlockSyntax blockSyntax, SyntaxList<TStatementSyntax> statements);

        protected abstract TStatementSyntax CreateUsingStatement(TLocalDeclarationSyntax declarationStatement, SyntaxTriviaList sameLineTrivia, SyntaxList<TStatementSyntax> statementsToSurround);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var span = context.Span;

            var (declarationSyntax, _) =
                await FindDisposableLocalDeclaration(document, span, context.CancellationToken).ConfigureAwait(false);

            if (declarationSyntax != null)
            {
                context.RegisterRefactoring(new MyCodeAction(
                    CodeActionTitle,
                    cancellationToken => IntroduceUsingStatementAsync(document, span, cancellationToken)));
            }
        }

        private static async Task<(TLocalDeclarationSyntax, ILocalSymbol)> FindDisposableLocalDeclaration(Document document, TextSpan selection, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var declarationSyntax =
                root.FindNode(selection)?.GetAncestor<TLocalDeclarationSyntax>()
                ?? root.FindTokenOnLeftOfPosition(selection.End).GetAncestor<TLocalDeclarationSyntax>();

            if (declarationSyntax is null)
            {
                return default;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var disposableType = semanticModel.Compilation.GetSpecialType(SpecialType.System_IDisposable);
            if (disposableType is null)
            {
                return default;
            }

            var operation = semanticModel.GetOperation(declarationSyntax, cancellationToken) as IVariableDeclarationGroupOperation;
            if (operation?.Declarations.Length != 1)
            {
                return default;
            }

            var localDeclaration = operation.Declarations[0];
            if (localDeclaration.Declarators.Length != 1)
            {
                return default;
            }

            var declarator = localDeclaration.Declarators[0];
            if (declarator.Initializer is null)
            {
                return default;
            }

            var localType = declarator.Symbol?.Type;
            if (localType is null)
            {
                return default;
            }

            var initializer = declarator.Initializer.Value;
            if (initializer is null || initializer.Kind == OperationKind.Invalid)
            {
                return default;
            }

            var isValidSelection = await CodeRefactoringHelpers.RefactoringSelectionIsValidAsync(
                document,
                selection,
                node: declarationSyntax,
                holes: ImmutableArray.Create(initializer.Syntax),
                cancellationToken).ConfigureAwait(false);

            if (!isValidSelection)
            {
                return default;
            }

            if (!IsLegalUsingStatementType(semanticModel.Compilation, disposableType, localType))
            {
                return default;
            }

            return (declarationSyntax, declarator.Symbol);
        }

        private static bool IsLegalUsingStatementType(Compilation compilation, ITypeSymbol disposableType, ITypeSymbol type)
        {
            if (disposableType == null)
            {
                return false;
            }

            // CS1674: type used in a using statement must be implicitly convertible to 'System.IDisposable'
            return compilation.ClassifyCommonConversion(type, disposableType).IsImplicit;
        }

        private async Task<Document> IntroduceUsingStatementAsync(
            Document document,
            TextSpan span,
            CancellationToken cancellationToken)
        {
            var (declarationStatement, localVariable) = await FindDisposableLocalDeclaration(document, span, cancellationToken).ConfigureAwait(false);

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            if (!(declarationStatement.Parent is TBlockSyntax parent))
            {
                throw new NotImplementedException();
            }

            var syntaxFactsService = document.GetLanguageService<ISyntaxFactsService>();

            // Find the minimal number of statements to move into the using block
            // in order to not break existing references to the local.
            var lastUsageStatement = FindSiblingStatementContainingLastUsage(
                declarationStatement,
                localVariable,
                semanticModel,
                syntaxFactsService,
                cancellationToken);

            var parentStatementsList = GetStatements(parent);

            var declarationStatementIndex = parentStatementsList.IndexOf(declarationStatement);

            var lastUsageStatementIndex = lastUsageStatement is null
                ? -1
                : parentStatementsList.IndexOf(lastUsageStatement, declarationStatementIndex + 1);

            var statementsToSurround = GetStatementsToSurround(parentStatementsList, declarationStatementIndex, lastUsageStatementIndex);

            // Separate the newline from the trivia that is going on the using declaration line.
            var trailingTrivia = SplitTrailingTrivia(declarationStatement, syntaxFactsService);

            var usingStatement =
                CreateUsingStatement(
                    declarationStatement,
                    trailingTrivia.sameLine,
                    statementsToSurround)
                    .WithLeadingTrivia(declarationStatement.GetLeadingTrivia())
                    .WithTrailingTrivia(trailingTrivia.endOfLine)
                    .WithAdditionalAnnotations(Formatter.Annotation);

            var newParent = WithStatements(
                parent,
                ReplaceRange(
                    parentStatementsList,
                    index: declarationStatementIndex,
                    count: 1 + statementsToSurround.Count,
                    newNodes: new[] { usingStatement }));

            return document.WithSyntaxRoot(root.ReplaceNode(parent, newParent));
        }

        private static (SyntaxTriviaList sameLine, SyntaxTriviaList endOfLine) SplitTrailingTrivia(SyntaxNode node, ISyntaxFactsService syntaxFactsService)
        {
            var trailingTrivia = node.GetTrailingTrivia();
            var lastIndex = trailingTrivia.Count - 1;

            return lastIndex != -1 && syntaxFactsService.IsEndOfLineTrivia(trailingTrivia[lastIndex])
                ? (
                    sameLine: trailingTrivia.RemoveAt(lastIndex),
                    endOfLine: new SyntaxTriviaList(trailingTrivia[lastIndex]))
                : (
                    sameLine: trailingTrivia,
                    endOfLine: SyntaxTriviaList.Empty);
        }

        protected static SyntaxList<TStatementSyntax> GetStatementsToSurround(SyntaxList<TStatementSyntax> parentList, int declarationStatementIndex, int lastUsageStatementIndex)
        {
            if (lastUsageStatementIndex == -1)
            {
                return default;
            }

            return new SyntaxList<TStatementSyntax>(parentList
                .Take(lastUsageStatementIndex + 1)
                .Skip(declarationStatementIndex + 1));
        }

        private static TStatementSyntax FindSiblingStatementContainingLastUsage(
            TStatementSyntax declarationSyntax,
            ILocalSymbol localVariable,
            SemanticModel semanticModel,
            ISyntaxFactsService syntaxFactsService,
            CancellationToken cancellationToken)
        {
            foreach (var nodeOrToken in declarationSyntax.Parent.ChildNodesAndTokens().Reverse())
            {
                var node = (TStatementSyntax)nodeOrToken.AsNode();
                if (node is null)
                {
                    continue;
                }

                if (node == declarationSyntax)
                {
                    break; // Ignore the declaration and usages prior to the declaration
                }

                if (ContainsReference(node, localVariable, semanticModel, syntaxFactsService, cancellationToken))
                {
                    return node;
                }
            }

            return null;
        }

        private static bool ContainsReference(
            SyntaxNode node,
            ILocalSymbol localVariable,
            SemanticModel semanticModel,
            ISyntaxFactsService syntaxFactsService,
            CancellationToken cancellationToken)
        {
            if (syntaxFactsService.IsIdentifierName(node))
            {
                var identifierName = syntaxFactsService.GetIdentifierOfSimpleName(node).ValueText;

                return syntaxFactsService.StringComparer.Equals(localVariable.Name, identifierName) &&
                    localVariable.Equals(semanticModel.GetSymbolInfo(node).Symbol);
            }

            foreach (var nodeOrToken in node.ChildNodesAndTokens())
            {
                var childNode = nodeOrToken.AsNode();
                if (childNode is null)
                {
                    continue;
                }

                if (ContainsReference(childNode, localVariable, semanticModel, syntaxFactsService, cancellationToken))
                {
                    return true;
                }
            }

            return false;
        }

        private static SyntaxList<T> ReplaceRange<T>(SyntaxList<T> syntaxList, int index, int count, IEnumerable<T> newNodes)
            where T : SyntaxNode
        {
            var list = syntaxList.ToList();

            list.RemoveRange(index, count);
            list.InsertRange(index, newNodes);

            return new SyntaxList<T>(list);
        }

        private sealed class MyCodeAction : DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}
