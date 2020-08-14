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
    internal abstract partial class AbstractInlineMethodRefactoringProvider<TInvocationNode, TExpression, TArgumentSyntax, TMethodDeclarationSyntax, TIdentifierName>
        : CodeRefactoringProvider
        where TInvocationNode : SyntaxNode
        where TExpression : SyntaxNode
        where TArgumentSyntax : SyntaxNode
        where TMethodDeclarationSyntax : SyntaxNode
        where TIdentifierName : SyntaxNode
    {
        private readonly ISyntaxFacts _syntaxFacts;
        private readonly ISemanticFactsService _semanticFactsService;

        /// <summary>
        /// Check if the <param name="calleeMethodDeclarationSyntaxNode"/> has only one expression or it is using arrow expression.
        /// </summary>
        protected abstract bool IsSingleStatementOrExpressionMethod(TMethodDeclarationSyntax calleeMethodDeclarationSyntaxNode);
        protected abstract TExpression GetInlineStatement(TMethodDeclarationSyntax calleeMethodDeclarationSyntaxNode);
        protected abstract SyntaxNode? GetEnclosingMethod(SyntaxNode syntaxNode);
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
        protected abstract TExpression Parenthesize(TExpression node);

        protected AbstractInlineMethodRefactoringProvider(ISyntaxFacts syntaxFacts, ISemanticFactsService semanticFactsService)
        {
            _syntaxFacts = syntaxFacts;
            _semanticFactsService = semanticFactsService;
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, _, cancellationToken) = context;
            var calleeMethodInvocationNode = await context.TryGetRelevantNodeAsync<TInvocationNode>().ConfigureAwait(false);
            if (calleeMethodInvocationNode == null)
            {
                return;
            }

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var calleeMethodSymbol = semanticModel.GetSymbolInfo(calleeMethodInvocationNode, cancellationToken).GetAnySymbol();
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
            var calleeMethodDeclarationNode = await calleeMethodDeclarationSyntaxReference.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
            if (!(calleeMethodDeclarationNode is TMethodDeclarationSyntax))
            {
                return;
            }

            if (!IsSingleStatementOrExpressionMethod((TMethodDeclarationSyntax)calleeMethodDeclarationNode))
            {
                return;
            }

            var statementContainsCallee = GetStatementContainsCallee(calleeMethodInvocationNode);
            if (statementContainsCallee == null)
            {
                return;
            }

            var invocationOperation = semanticModel.GetOperation(calleeMethodInvocationNode, cancellationToken);
            if (!(invocationOperation is IInvocationOperation))
            {
                return;
            }

            var codeAction = new CodeAction.DocumentChangeAction(
                string.Format(FeaturesResources.Inline_0, calleeMethodSymbol.ToNameDisplayString()),
                cancellationToken => InlineMethodAsync(
                    document,
                    calleeMethodInvocationNode,
                    (IMethodSymbol)calleeMethodSymbol,
                    (TMethodDeclarationSyntax)calleeMethodDeclarationNode,
                    statementContainsCallee,
                    (IInvocationOperation)invocationOperation,
                    cancellationToken));

            context.RegisterRefactoring(codeAction);
        }

        private async Task<Document> InlineMethodAsync(
            Document document,
            SyntaxNode calleeMethodInvocationSyntaxNode,
            IMethodSymbol calleeMethodSymbol,
            TMethodDeclarationSyntax calleeMethodDeclarationSyntaxNode,
            SyntaxNode statementContainsCallee,
            IInvocationOperation invocationOperation,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var methodParametersInfo = MethodParametersInfo.GetMethodParametersInfo(_syntaxFacts, invocationOperation);
            var inlineContext = await InlineMethodContext.GetInlineContextAsync(
                this,
                document,
                calleeMethodInvocationSyntaxNode,
                calleeMethodSymbol,
                calleeMethodDeclarationSyntaxNode,
                statementContainsCallee,
                methodParametersInfo,
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
                var enclosingMethod = GetEnclosingMethod(calleeMethodInvocationSyntaxNode);
                if (enclosingMethod != null
                    && semanticModel.GetDeclaredSymbol(enclosingMethod, cancellationToken) is IMethodSymbol callerMethodSymbol
                    && !callerMethodSymbol.IsAsync)
                {
                    documentEditor.SetModifiers(enclosingMethod, DeclarationModifiers.From(calleeMethodSymbol).WithAsync(isAsync: true));
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
