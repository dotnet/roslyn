// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
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
    internal abstract partial class AbstractInlineMethodRefactoringProvider<
            TInvocationSyntax,
            TExpressionSyntax,
            TMethodDeclarationSyntax,
            TStatementSyntax,
            TLocalDeclarationSyntax>
        : CodeRefactoringProvider
        where TExpressionSyntax : SyntaxNode
        where TInvocationSyntax : TExpressionSyntax
        where TMethodDeclarationSyntax : SyntaxNode
        where TStatementSyntax : SyntaxNode
        where TLocalDeclarationSyntax : TStatementSyntax
    {
        private readonly ISyntaxFacts _syntaxFacts;
        private readonly ISemanticFactsService _semanticFactsService;

        protected abstract TExpressionSyntax? GetInlineExpression(TMethodDeclarationSyntax calleeMethodDeclarationSyntaxNode);
        protected abstract SyntaxNode? GetEnclosingMethodLikeNode(SyntaxNode syntaxNode);
        protected abstract SyntaxNode GenerateTypeSyntax(ITypeSymbol symbol, bool allowVar);
        protected abstract SyntaxNode GenerateArrayInitializerExpression(ImmutableArray<SyntaxNode> arguments);
        protected abstract TExpressionSyntax Parenthesize(TExpressionSyntax expressionNode);
        protected abstract bool TryGetInlineNodeAndReplacementNodeForDelegate(
            TInvocationSyntax calleeInvocationNode,
            IMethodSymbol calleeMethodSymbol,
            TExpressionSyntax inlineExpressionNode,
            TStatementSyntax statementContainsCallee,
            SyntaxGenerator syntaxGenerator,
            out SyntaxNode? inlineSyntaxNode,
            out SyntaxNode? syntaxNodeToReplace);

        /// <summary>
        /// Check if <paramref name="expressionNode"/> could be used as an Expression in ExpressionStatement
        /// </summary>
        protected abstract bool IsValidExpressionUnderStatementExpression(TExpressionSyntax expressionNode);

        /// <summary>
        /// A preferred name used to generated a declaration when the
        /// inline method has a return value but is not assigned to a variable.
        /// Example:
        /// void Caller()
        /// {
        ///     Callee();
        /// }
        /// int Callee()
        /// {
        ///     return 1;
        /// };
        /// After it should be:
        /// void Caller()
        /// {
        ///     int temp = 1;
        /// }
        /// int Callee()
        /// {
        ///     return 1;
        /// };
        /// </summary>
        private const string TemporaryName = "temp";

        protected AbstractInlineMethodRefactoringProvider(ISyntaxFacts syntaxFacts, ISemanticFactsService semanticFactsService)
        {
            _syntaxFacts = syntaxFacts;
            _semanticFactsService = semanticFactsService;
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, _, cancellationToken) = context;
            var calleeMethodInvocationNode = await context.TryGetRelevantNodeAsync<TInvocationSyntax>().ConfigureAwait(false);
            if (calleeMethodInvocationNode == null)
            {
                return;
            }

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var calleeMethodSymbol = semanticModel.GetSymbolInfo(calleeMethodInvocationNode, cancellationToken).GetAnySymbol() as IMethodSymbol;
            if (calleeMethodSymbol == null)
            {
                return;
            }

            var isOrdinaryOrExtensionMethod = calleeMethodSymbol.IsOrdinaryMethod() || calleeMethodSymbol.IsExtensionMethod;
            if (calleeMethodSymbol.DeclaredAccessibility != Accessibility.Private || !isOrdinaryOrExtensionMethod)
            {
                return;
            }

            var calleeMethodDeclarationSyntaxReferences = calleeMethodSymbol.DeclaringSyntaxReferences;
            if (calleeMethodDeclarationSyntaxReferences.Length != 1)
            {
                return;
            }

            var calleeMethodDeclarationSyntaxReference = calleeMethodDeclarationSyntaxReferences[0];
            // For C# it will return MethodDeclarationSyntax,
            // For VB it will return MethodStatementSyntax, and what it is expecting is MethodBlockSyntax
            var calleeMethodDeclarationOrStatementNode = await calleeMethodDeclarationSyntaxReference.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
            var calleeMethodNode = calleeMethodDeclarationOrStatementNode as TMethodDeclarationSyntax
                ?? calleeMethodDeclarationOrStatementNode.Parent as TMethodDeclarationSyntax;

            if (calleeMethodNode == null)
            {
                return;
            }

            var inlineExpression = GetInlineExpression(calleeMethodNode);
            if (inlineExpression == null)
            {
                return;
            }

            var statementContainsCallee = calleeMethodInvocationNode.GetAncestor<TStatementSyntax>();
            if (statementContainsCallee == null)
            {
                return;
            }

            var invocationOperation = semanticModel.GetOperation(calleeMethodInvocationNode, cancellationToken) as IInvocationOperation;
            if (invocationOperation == null)
            {
                return;
            }

            var codeActions = GenerateCodeActions(
                document,
                calleeMethodInvocationNode,
                calleeMethodSymbol,
                calleeMethodNode,
                inlineExpression,
                statementContainsCallee,
                invocationOperation);

            var nestedCodeAction = new MyNestedCodeAction(
                string.Format(FeaturesResources.Inline_0, calleeMethodSymbol.ToNameDisplayString()),
                codeActions,
                isInlinable: false);

            context.RegisterRefactoring(nestedCodeAction, calleeMethodInvocationNode.Span);
        }

        private ImmutableArray<CodeAction> GenerateCodeActions(
            Document document,
            TInvocationSyntax calleeMethodInvocationNode,
            IMethodSymbol calleeMethodSymbol,
            TMethodDeclarationSyntax calleeMethodNode,
            TExpressionSyntax inlineExpression,
            TStatementSyntax statementContainsCallee,
            IInvocationOperation invocationOperation)
        {
            var calleeMethodName = calleeMethodSymbol.ToNameDisplayString();
            var codeActionKeepsCallee = new MySolutionChangeAction(
                string.Format(FeaturesResources.Keep_0, calleeMethodName),
                cancellationToken =>
                    InlineMethodAsync(document,
                        calleeMethodInvocationNode,
                        calleeMethodSymbol,
                        calleeMethodNode,
                        inlineExpression,
                        statementContainsCallee,
                        invocationOperation,
                        removeCalleeDeclarationNode: false,
                        cancellationToken));

            var codeActionRemovesCallee = new MySolutionChangeAction(
                string.Format(FeaturesResources.Remove_0, calleeMethodName),
                cancellationToken =>
                    InlineMethodAsync(
                        document,
                        calleeMethodInvocationNode,
                        calleeMethodSymbol,
                        calleeMethodNode,
                        inlineExpression,
                        statementContainsCallee,
                        invocationOperation,
                        removeCalleeDeclarationNode: true,
                        cancellationToken));

            return ImmutableArray.Create<CodeAction>(codeActionKeepsCallee, codeActionRemovesCallee);
        }

        private async Task<Solution> InlineMethodAsync(
            Document document,
            TInvocationSyntax calleeMethodInvocationNode,
            IMethodSymbol calleeMethodSymbol,
            TMethodDeclarationSyntax calleeMethodNode,
            TExpressionSyntax inlineExpression,
            TStatementSyntax statementContainsCallee,
            IInvocationOperation invocationOperation,
            bool removeCalleeDeclarationNode,
            CancellationToken cancellationToken)
        {
            var methodParametersInfo = await MethodParametersInfo.GetMethodParametersInfoAsync(_syntaxFacts, document, invocationOperation, cancellationToken).ConfigureAwait(false);
            var inlineContext = await GetInlineMethodContextAsync(
                document,
                calleeMethodInvocationNode,
                calleeMethodSymbol,
                calleeMethodNode,
                inlineExpression,
                statementContainsCallee,
                methodParametersInfo,
                cancellationToken).ConfigureAwait(false);
            return await ChangeSolutionAsync(
                document,
                calleeMethodInvocationNode,
                calleeMethodSymbol,
                calleeMethodNode,
                inlineContext,
                removeCalleeDeclarationNode,
                cancellationToken).ConfigureAwait(false);
        }

        private async Task<Solution> ChangeSolutionAsync(
            Document document,
            SyntaxNode calleeMethodInvocationNode,
            IMethodSymbol calleeMethodSymbol,
            TMethodDeclarationSyntax calleeMethodNode,
            InlineMethodContext inlineMethodContext,
            bool removeCalleeDeclarationNode,
            CancellationToken cancellationToken)
        {
            var solution = document.Project.Solution;
            var solutionEditor = new SolutionEditor(solution);
            var documentEditor = await solutionEditor.GetDocumentEditorAsync(document.Id, cancellationToken).ConfigureAwait(false);
            var statementContainsCalleeInvocationExpression = inlineMethodContext.StatementContainingCallee;
            foreach (var statement in inlineMethodContext.StatementsToInsertBeforeCallee)
            {
                documentEditor.InsertBefore(
                    statementContainsCalleeInvocationExpression,
                    // Make sure the statement is aligned with the existing statement
                    statement.WithTriviaFrom(statementContainsCalleeInvocationExpression));
            }

            var syntaxNodeToReplace = inlineMethodContext.SyntaxNodeToReplace;
            var inlineSyntaxNode = inlineMethodContext.InlineSyntaxNode;
            documentEditor.ReplaceNode(syntaxNodeToReplace, inlineSyntaxNode);

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            // If the inline content has 'await' expression, then make sure the caller is converted to 'async' method
            if (inlineMethodContext.ContainsAwaitExpression)
            {
                var enclosingMethod = GetEnclosingMethodLikeNode(calleeMethodInvocationNode);
                if (enclosingMethod != null)
                {
                    var methodSymbol = semanticModel.GetDeclaredSymbol(enclosingMethod, cancellationToken)
                        ?? semanticModel.GetSymbolInfo(enclosingMethod, cancellationToken).GetAnySymbol();
                    if (methodSymbol is IMethodSymbol callerMethodSymbol && !callerMethodSymbol.IsAsync)
                    {
                        documentEditor.SetModifiers(enclosingMethod, DeclarationModifiers.From(calleeMethodSymbol).WithAsync(isAsync: true));
                    }
                }
            }

            if (removeCalleeDeclarationNode)
            {
                var documentId = solution.GetDocumentId(calleeMethodNode.SyntaxTree);
                if (documentId != null)
                {
                    var editor = await solutionEditor.GetDocumentEditorAsync(documentId, cancellationToken).ConfigureAwait(false);
                    editor.RemoveNode(calleeMethodNode);
                }
            }

            return solutionEditor.GetChangedSolution();
        }

        private class MySolutionChangeAction : CodeAction.SolutionChangeAction
        {
            public MySolutionChangeAction(
                string title,
                 Func<CancellationToken, Task<Solution>> createChangedSolution,
                string? equivalenceKey = null) : base(title, createChangedSolution, equivalenceKey)
            {
            }
        }

        private class MyNestedCodeAction : CodeAction.CodeActionWithNestedActions
        {
            public MyNestedCodeAction(
                string title,
                ImmutableArray<CodeAction> nestedActions,
                bool isInlinable,
                CodeActionPriority priority = CodeActionPriority.Medium) : base(title, nestedActions, isInlinable, priority)
            {
            }
        }
    }
}
