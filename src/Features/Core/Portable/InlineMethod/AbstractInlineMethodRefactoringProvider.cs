// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.InlineMethod
{
    internal abstract partial class AbstractInlineMethodRefactoringProvider<TInvocationSyntaxNode, TExpressionSyntax, TArgumentSyntax>
        : CodeRefactoringProvider
        where TInvocationSyntaxNode : SyntaxNode
        where TExpressionSyntax : SyntaxNode
        where TArgumentSyntax : SyntaxNode
    {
        private readonly ISyntaxFacts _syntaxFacts;
        private readonly ISemanticFactsService _semanticFactsService;

        /// <summary>
        /// Check if the <param name="calleeMethodDeclarationSyntaxNode"/> has only one expression or it is using arrow expression.
        /// </summary>
        protected abstract bool IsSingleStatementOrExpressionMethod(SyntaxNode calleeMethodDeclarationSyntaxNode);
        protected abstract TExpressionSyntax GetInlineStatement(SyntaxNode calleeMethodDeclarationSyntaxNode);
        protected abstract IParameterSymbol? GetParameterSymbol(SemanticModel semanticModel, TArgumentSyntax argumentSyntaxNode, CancellationToken cancellationToken);
        protected abstract SyntaxNode GenerateTypeSyntax(ITypeSymbol symbol);
        protected abstract SyntaxNode GenerateArrayInitializerExpression(ImmutableArray<SyntaxNode> arguments);
        protected abstract bool IsStatementConsideredAsInvokingStatement(SyntaxNode node);
        protected abstract TExpressionSyntax Parenthesize(TExpressionSyntax node);

        protected AbstractInlineMethodRefactoringProvider(ISyntaxFacts syntaxFacts, ISemanticFactsService semanticFactsService)
        {
            _syntaxFacts = syntaxFacts;
            _semanticFactsService = semanticFactsService;
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, _, cancellationToken) = context;
            var calleeMethodInvocationSyntaxNode = await GetInvocationExpressionSyntaxNodeAsync(context).ConfigureAwait(false);
            if (calleeMethodInvocationSyntaxNode == null)
            {
                return;
            }

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var calleeMethodSymbol = semanticModel.GetSymbolInfo(calleeMethodInvocationSyntaxNode, cancellationToken).GetAnySymbol();
            if (calleeMethodSymbol == null
                || calleeMethodSymbol.DeclaredAccessibility != Accessibility.Private
                || !calleeMethodSymbol.IsOrdinaryMethod())
            {
                return;
            }

            if (calleeMethodSymbol is IMethodSymbol calleeMethodInvocationSymbol)
            {
                var calleeMethodDeclarationSyntaxReferences = calleeMethodInvocationSymbol.DeclaringSyntaxReferences;

                if (calleeMethodDeclarationSyntaxReferences.Length != 1)
                {
                    return;
                }

                var calleeMethodDeclarationSyntaxReference = calleeMethodDeclarationSyntaxReferences[0];
                var calleeMethodDeclarationSyntaxNode = await calleeMethodDeclarationSyntaxReference.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
                if (!IsSingleStatementOrExpressionMethod(calleeMethodDeclarationSyntaxNode))
                {
                    return;
                }

                var statementContainsCallee = GetStatementContainsCallee(calleeMethodInvocationSyntaxNode);
                if (statementContainsCallee == null)
                {
                    return;
                }

                var codeAction = new CodeAction.DocumentChangeAction(
                    string.Format(FeaturesResources.Inline_0, calleeMethodInvocationSymbol.ToNameDisplayString()),
                    cancellationToken => InlineMethodAsync(
                        document,
                        calleeMethodInvocationSyntaxNode,
                        calleeMethodInvocationSymbol,
                        calleeMethodDeclarationSyntaxNode,
                        cancellationToken));

                context.RegisterRefactoring(codeAction);
            }
        }

        private async Task<SyntaxNode?> GetInvocationExpressionSyntaxNodeAsync(CodeRefactoringContext context)
        {
            var syntaxNode = await context.TryGetRelevantNodeAsync<TInvocationSyntaxNode>().ConfigureAwait(false);
            return syntaxNode;
        }

        private static bool IsExpressionSyntax(SyntaxNode syntaxNode)
            => syntaxNode is TExpressionSyntax;

        private async Task<Document> InlineMethodAsync(
            Document document,
            SyntaxNode calleeMethodInvocationSyntaxNode,
            IMethodSymbol calleeMethodSymbol,
            SyntaxNode calleeMethodDeclarationSyntaxNode,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var methodParametersInfo = MethodParametersInfo.GetMethodParametersInfo(
                this,
                _syntaxFacts,
                semanticModel,
                calleeMethodInvocationSyntaxNode,
                calleeMethodSymbol,
                cancellationToken);

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var inlineContext = await InlineMethodContext.GetInlineContextAsync(
                this,
                _syntaxFacts,
                _semanticFactsService,
                document,
                semanticModel,
                calleeMethodInvocationSyntaxNode,
                calleeMethodSymbol,
                calleeMethodDeclarationSyntaxNode,
                methodParametersInfo,
                root,
                cancellationToken).ConfigureAwait(false);

            var documentEditor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var statementContainsCalleeInvocationExpression = inlineContext.StatementContainingCallee;
            // In case can't find the containing statement, don't do insertion
            if (statementContainsCalleeInvocationExpression != null)
            {
                foreach (var statement in inlineContext.StatementsToInsertBeforeCallee)
                {
                    documentEditor.InsertBefore(
                        statementContainsCalleeInvocationExpression,
                        // Make sure the statement is aligned with the existing statement
                        statement.WithLeadingTrivia(statementContainsCalleeInvocationExpression.GetLeadingTrivia()));
                }
            }

            var syntaxNodeToReplace = inlineContext.SyntaxNodeToReplace;
            var inlineSyntaxNode = inlineContext.InlineSyntaxNode;
            if (syntaxNodeToReplace != null)
            {
                if (inlineSyntaxNode == null)
                {
                    // When it has only one return statement in the callee & return void, just remove the whole statement.
                    documentEditor.RemoveNode(syntaxNodeToReplace);
                    return documentEditor.GetChangedDocument();
                }
                else
                {
                    documentEditor.ReplaceNode(syntaxNodeToReplace, inlineSyntaxNode);
                }
            }

            // If the inline content has 'await' expression, then make sure the caller is converted to 'async' method
            if (inlineContext.ContainsAwaitExpression)
            {
                var callerMethodSyntaxNode = _syntaxFacts.GetContainingMemberDeclaration(root, calleeMethodInvocationSyntaxNode.SpanStart);
                if (semanticModel.GetDeclaredSymbol(callerMethodSyntaxNode, cancellationToken) is IMethodSymbol callerMethodSymbol
                    && !callerMethodSymbol.IsAsync)
                {
                    documentEditor.SetModifiers(callerMethodSyntaxNode,
                        DeclarationModifiers.From(calleeMethodSymbol).WithAsync(isAsync: true));
                }
            }

            return documentEditor.GetChangedDocument();
        }

        /// <summary>
        /// Try to find the statement that contains the <param name="calleeInvocationSyntax"/>.
        /// For example,
        /// void Caller()
        /// {
        ///     var x = Callee();
        /// }
        /// The LocalDeclarationSyntaxNode will be returned.
        ///
        /// void Caller()
        /// {
        ///     if (Callee())
        ///     {
        ///     }
        /// }
        /// The IfStatementSyntax will be returned.
        /// Return null if such node can't be found.
        /// </summary>
        private SyntaxNode? GetStatementContainsCallee(SyntaxNode calleeInvocationSyntax)
        {
            for (var node = calleeInvocationSyntax; node != null; node = node!.Parent)
            {
                if (node != null && IsStatementConsideredAsInvokingStatement(node))
                {
                    return node;
                }
            }

            return null;
        }
    }
}
