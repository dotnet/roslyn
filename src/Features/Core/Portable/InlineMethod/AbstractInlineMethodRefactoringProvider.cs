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
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.InlineMethod
{
    internal abstract partial class AbstractInlineMethodRefactoringProvider<TInvocationSyntaxNode, TExpressionSyntax, TArgumentSyntax, TMethodDeclarationSyntax>
        : CodeRefactoringProvider
        where TInvocationSyntaxNode : SyntaxNode
        where TExpressionSyntax : SyntaxNode
        where TArgumentSyntax : SyntaxNode
        where TMethodDeclarationSyntax : SyntaxNode
    {
        private readonly ISyntaxFacts _syntaxFacts;
        private readonly ISemanticFactsService _semanticFactsService;

        /// <summary>
        /// Check if the <param name="calleeMethodDeclarationSyntaxNode"/> has only one expression or it is using arrow expression.
        /// </summary>
        protected abstract bool IsSingleStatementOrExpressionMethod(TMethodDeclarationSyntax calleeMethodDeclarationSyntaxNode);
        protected abstract TExpressionSyntax GetInlineStatement(TMethodDeclarationSyntax calleeMethodDeclarationSyntaxNode);

        /// <summary>
        /// Get the mapping parameterSymbol from the <param name="argumentSyntaxNode"/>.
        /// Note: it could be null if there is error in the code.
        /// </summary>
        protected abstract SyntaxNode GenerateTypeSyntax(ITypeSymbol symbol);
        protected abstract SyntaxNode GenerateArrayInitializerExpression(ImmutableArray<SyntaxNode> arguments);

        /// <summary>
        /// Check if the <param name="syntaxNode"/> should be considered as the statement that contains the method invocation.
        /// Example:
        /// void Caller()
        /// {
        ///     var x = Callee();
        /// }
        /// LocalDeclarationSyntaxNode here is considered as the containing statement of Callee()
        /// </summary>
        protected abstract bool ShouldConsideredAsContainingStatement(SyntaxNode syntaxNode);
        protected abstract TExpressionSyntax Parenthesize(TExpressionSyntax node);

        protected AbstractInlineMethodRefactoringProvider(ISyntaxFacts syntaxFacts, ISemanticFactsService semanticFactsService)
        {
            _syntaxFacts = syntaxFacts;
            _semanticFactsService = semanticFactsService;
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, _, cancellationToken) = context;
            var calleeMethodInvocationSyntaxNode = await context.TryGetRelevantNodeAsync<TInvocationSyntaxNode>().ConfigureAwait(false);
            if (calleeMethodInvocationSyntaxNode == null)
            {
                return;
            }

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var calleeMethodSymbol = semanticModel.GetSymbolInfo(calleeMethodInvocationSyntaxNode, cancellationToken).GetAnySymbol();
            if (!(calleeMethodSymbol is IMethodSymbol))
            {
                return;
            }

            if (calleeMethodSymbol.DeclaredAccessibility != Accessibility.Private
                || calleeMethodSymbol.IsConstructor()
                || calleeMethodSymbol.IsUserDefinedOperator()
                || calleeMethodSymbol.IsConversion()
                || calleeMethodSymbol.IsDestructor())
            {
                return;
            }

            var calleeMethodDeclarationSyntaxReferences = calleeMethodSymbol.DeclaringSyntaxReferences;
            if (calleeMethodDeclarationSyntaxReferences.Length != 1)
            {
                return;
            }

            var calleeMethodDeclarationSyntaxReference = calleeMethodDeclarationSyntaxReferences[0];
            var calleeMethodDeclarationSyntaxNode = await calleeMethodDeclarationSyntaxReference.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
            if (!(calleeMethodDeclarationSyntaxNode is TMethodDeclarationSyntax))
            {
                return;
            }

            if (!IsSingleStatementOrExpressionMethod((TMethodDeclarationSyntax)calleeMethodDeclarationSyntaxNode))
            {
                return;
            }

            var statementContainsCallee = GetStatementContainsCallee(calleeMethodInvocationSyntaxNode);
            if (statementContainsCallee == null)
            {
                return;
            }

            var invocationOperation = semanticModel.GetOperation(calleeMethodInvocationSyntaxNode, cancellationToken);
            if (!(invocationOperation is IInvocationOperation))
            {
                return;
            }

            var codeAction = new CodeAction.DocumentChangeAction(
                string.Format(FeaturesResources.Inline_0, calleeMethodSymbol.ToNameDisplayString()),
                cancellationToken => InlineMethodAsync(
                    document,
                    calleeMethodInvocationSyntaxNode,
                    (IMethodSymbol)calleeMethodSymbol,
                    (TMethodDeclarationSyntax)calleeMethodDeclarationSyntaxNode,
                    (IInvocationOperation)invocationOperation,
                    cancellationToken));

            context.RegisterRefactoring(codeAction);
        }

        private async Task<Document> InlineMethodAsync(
            Document document,
            SyntaxNode calleeMethodInvocationSyntaxNode,
            IMethodSymbol calleeMethodSymbol,
            TMethodDeclarationSyntax calleeMethodDeclarationSyntaxNode,
            IInvocationOperation invocationOperation,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var methodParametersInfo = MethodParametersInfo.GetMethodParametersInfo2(_syntaxFacts, invocationOperation);
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
            foreach (var statement in inlineContext.StatementsToInsertBeforeCallee)
            {
                documentEditor.InsertBefore(
                    statementContainsCalleeInvocationExpression,
                    // Make sure the statement is aligned with the existing statement
                    statement.WithLeadingTrivia(statementContainsCalleeInvocationExpression.GetLeadingTrivia()));
            }

            var syntaxNodeToReplace = inlineContext.SyntaxNodeToReplace;
            var inlineSyntaxNode = inlineContext.InlineSyntaxNode;
            documentEditor.ReplaceNode(syntaxNodeToReplace, inlineSyntaxNode);

            // If the inline content has 'await' expression, then make sure the caller is converted to 'async' method
            if (inlineContext.ContainsAwaitExpression)
            {
                // TODO: handle local and lambda cases.
                var callerMethodSyntaxNode = _syntaxFacts.GetContainingMemberDeclaration(root, calleeMethodInvocationSyntaxNode.SpanStart);
                if (semanticModel.GetDeclaredSymbol(callerMethodSyntaxNode, cancellationToken) is IMethodSymbol callerMethodSymbol
                    && !callerMethodSymbol.IsAsync)
                {
                    documentEditor.SetModifiers(callerMethodSyntaxNode, DeclarationModifiers.From(calleeMethodSymbol).WithAsync(isAsync: true));
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
        /// LocalDeclarationSyntaxNode will be returned.
        ///
        /// void Caller()
        /// {
        ///     if (Callee())
        ///     {
        ///     }
        /// }
        /// IfStatementSyntax will be returned.
        /// Return null if such node can't be found.
        /// </summary>
        private SyntaxNode? GetStatementContainsCallee(SyntaxNode calleeInvocationSyntax)
        {
            for (var node = calleeInvocationSyntax; node != null; node = node!.Parent)
            {
                if (node != null && ShouldConsideredAsContainingStatement(node))
                {
                    return node;
                }
            }

            return null;
        }
    }
}
