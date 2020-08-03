// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.InlineMethod
{
    internal abstract partial class AbstractInlineMethodRefactoringProvider : CodeRefactoringProvider
    {
        private readonly ISyntaxFacts _syntaxFacts;

        protected abstract Task<SyntaxNode?> GetInvocationExpressionSyntaxNodeAsync(CodeRefactoringContext context);

        /// <summary>
        /// Check if the <param name="calleeMethodDeclarationSyntaxNode"/> has only one expression or it is using arrow expression.
        /// </summary>
        protected abstract bool IsMethodContainsOneStatement(SyntaxNode calleeMethodDeclarationSyntaxNode);

        protected abstract SyntaxNode? GetInlineStatement(SyntaxNode calleeMethodDeclarationSyntaxNode);

        protected AbstractInlineMethodRefactoringProvider(ISyntaxFacts syntaxFacts)
        {
            _syntaxFacts = syntaxFacts;
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, _, cancellationToken) = context;
            var calleeMethodInvocationSyntaxNode = await GetInvocationExpressionSyntaxNodeAsync(context).ConfigureAwait(false);
            if (calleeMethodInvocationSyntaxNode == null)
            {
                return;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel == null)
            {
                return;
            }

            var methodSymbol = TryGetBestMatchSymbol(semanticModel, calleeMethodInvocationSyntaxNode, cancellationToken);
            if (methodSymbol == null
                || methodSymbol.DeclaredAccessibility != Accessibility.Private
                || !methodSymbol.IsOrdinaryMethod())
            {
                return;
            }

            if (methodSymbol is IMethodSymbol calleeMethodInvocationSymbol)
            {
                var calleeMethodDeclarationSyntaxNodes = await Task.WhenAll(calleeMethodInvocationSymbol.DeclaringSyntaxReferences
                    .Select(reference => reference.GetSyntaxAsync())).ConfigureAwait(false);

                if (calleeMethodDeclarationSyntaxNodes == null || calleeMethodDeclarationSyntaxNodes.Length != 1)
                {
                    return;
                }

                var calleeMethodDeclarationSyntaxNode = calleeMethodDeclarationSyntaxNodes[0];

                if (!IsMethodContainsOneStatement(calleeMethodDeclarationSyntaxNode))
                {
                    return;
                }

                var root = await document.GetSyntaxRootAsync().ConfigureAwait(false);
                if (root == null)
                {
                    return;
                }

                var codeAction = new CodeAction.DocumentChangeAction(
                    string.Format(FeaturesResources.Inline_0, calleeMethodInvocationSymbol.ToNameDisplayString()),
                    cancellationToken => InlineMethodAsync(
                        document,
                        semanticModel!,
                        calleeMethodInvocationSyntaxNode,
                        calleeMethodInvocationSymbol,
                        calleeMethodDeclarationSyntaxNode,
                        root!,
                        cancellationToken));

                context.RegisterRefactoring(codeAction);
            }
        }

        public static ISymbol? TryGetBestMatchSymbol(
            SemanticModel semanticModel,
            SyntaxNode node,
            CancellationToken cancellationToken)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(node, cancellationToken);
            var symbol = symbolInfo.Symbol;
            if (symbol != null)
            {
                return symbol;
            }
            else if (symbolInfo.CandidateSymbols.Any())
            {
                return symbolInfo.CandidateSymbols[0];
            }

            return null;
        }

        private async Task<Document> InlineMethodAsync(
            Document document,
            SemanticModel semanticModel,
            SyntaxNode calleeMethodInvocationSyntaxNode,
            IMethodSymbol calleeMethodSymbol,
            SyntaxNode calleeMethodDeclarationSyntaxNode,
            SyntaxNode root,
            CancellationToken cancellationToken)
        {
            var inlineContext = InlineMethodContext.GetInlineContext(
                this,
                _syntaxFacts,
                semanticModel,
                calleeMethodInvocationSyntaxNode,
                calleeMethodSymbol,
                calleeMethodDeclarationSyntaxNode,
                cancellationToken);

            var calleeMethodDeclarationNodeEditor = new SyntaxEditor(calleeMethodDeclarationSyntaxNode, document.Project.Solution.Workspace);

            foreach (var (symbol, identifierNameSyntaxNode) in inlineContext.ReplacementTable)
            {
                await ReplaceAllSyntaxNodesForSymbolAsync(
                    document.Project.Solution,
                    root,
                    calleeMethodDeclarationNodeEditor,
                    symbol,
                    identifierNameSyntaxNode,
                    cancellationToken).ConfigureAwait(false);
            }

            var inlineStatement = GetInlineStatement(calleeMethodDeclarationNodeEditor.GetChangedRoot());
            var documentEditor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            foreach (var statement in inlineContext.StatementsNeedInsert)
            {
                documentEditor.InsertBefore(inlineContext.StatementInvokesCallee, statement);
            }

            if (inlineStatement == null)
            {
                documentEditor.RemoveNode(inlineContext.StatementInvokesCallee);
            }
            else
            {
                documentEditor.ReplaceNode(calleeMethodInvocationSyntaxNode, inlineStatement);
            }

            return documentEditor.GetChangedDocument();
        }

        private static async Task ReplaceAllSyntaxNodesForSymbolAsync(
            Solution solution,
            SyntaxNode root,
            SyntaxEditor editor,
            ISymbol symbol,
            SyntaxNode replacementNode,
            CancellationToken cancellationToken)
        {
            var allReferences = await SymbolFinder.FindReferencesAsync(symbol, solution, cancellationToken).ConfigureAwait(false);
            var allSyntaxNodesToReplace = allReferences
                .SelectMany(reference => reference.Locations
                    .Select(location => root.FindNode(location.Location.SourceSpan))).ToImmutableArray();

            foreach (var nodeToReplace in allSyntaxNodesToReplace)
            {
                if (editor.OriginalRoot.Contains(nodeToReplace))
                {
                    var replacementNodeWithTrivia = replacementNode
                        .WithLeadingTrivia(nodeToReplace.GetLeadingTrivia())
                        .WithTrailingTrivia(nodeToReplace.GetTrailingTrivia());
                    editor.ReplaceNode(nodeToReplace, replacementNodeWithTrivia);
                }
            }
        }
    }
}
