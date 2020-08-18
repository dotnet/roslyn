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
        protected abstract TExpression Parenthesize(TExpression expressionNode);

        /// <summary>
        /// Check if <param name="expressionNode"/> could be used as an Expression in ExpressionStatement
        /// </summary>
        protected abstract bool IsValidExpressionUnderStatementExpression(TExpression expressionNode);

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

            var codeActions = await GenerateCodeActionsAsync(
                document,
                calleeMethodInvocationNode,
                (IMethodSymbol)calleeMethodSymbol,
                (TMethodDeclarationSyntax)calleeMethodDeclarationNode,
                statementContainsCallee,
                (IInvocationOperation)invocationOperation,
                cancellationToken).ConfigureAwait(false);

            var nestedCodeAction = new MyNestedCodeAction(
                string.Format(FeaturesResources.Inline_0, calleeMethodSymbol.ToNameDisplayString()),
                codeActions,
                isInlinable: true);

            context.RegisterRefactoring(nestedCodeAction, calleeMethodInvocationNode.Span);
        }

        private async Task<ImmutableArray<CodeAction>> GenerateCodeActionsAsync(
            Document document,
            SyntaxNode calleeMethodInvocationSyntaxNode,
            IMethodSymbol calleeMethodSymbol,
            TMethodDeclarationSyntax calleeMethodDeclarationSyntaxNode,
            SyntaxNode statementContainsCallee,
            IInvocationOperation invocationOperation,
            CancellationToken cancellationToken)
        {
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

            var calleeMethodName = calleeMethodSymbol.ToNameDisplayString();
            var codeActionKeepsCallee = new MySolutionChangeAction(
                string.Format(FeaturesResources.Keep_0, calleeMethodName),
                cancellationToken =>
                    InlineMethodAsync(document,
                        calleeMethodInvocationSyntaxNode,
                        calleeMethodSymbol,
                        calleeMethodDeclarationSyntaxNode,
                        inlineContext,
                        removeCalleeDeclarationNode: false,
                        cancellationToken));

            var codeActionRemovesCallee = new MySolutionChangeAction(
                string.Format(FeaturesResources.Remove_0, calleeMethodName),
                cancellationToken =>
                    InlineMethodAsync(document,
                        calleeMethodInvocationSyntaxNode,
                        calleeMethodSymbol,
                        calleeMethodDeclarationSyntaxNode,
                        inlineContext,
                        removeCalleeDeclarationNode: true,
                        cancellationToken));

            return ImmutableArray.Create<CodeAction>(codeActionKeepsCallee, codeActionRemovesCallee);
        }

        private async Task<Solution> InlineMethodAsync(
            Document document,
            SyntaxNode calleeMethodInvocationNode,
            IMethodSymbol calleeMethodSymbol,
            TMethodDeclarationSyntax calleeMethodDeclarationNode,
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
                    statement.WithLeadingTrivia(statementContainsCalleeInvocationExpression.GetLeadingTrivia()));
            }

            var syntaxNodeToReplace = inlineMethodContext.SyntaxNodeToReplace;
            var inlineSyntaxNode = inlineMethodContext.InlineSyntaxNode;
            documentEditor.ReplaceNode(syntaxNodeToReplace, inlineSyntaxNode);

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            // If the inline content has 'await' expression, then make sure the caller is converted to 'async' method
            if (inlineMethodContext.ContainsAwaitExpression)
            {
                var enclosingMethod = GetEnclosingMethod(calleeMethodInvocationNode);
                if (enclosingMethod != null
                    && semanticModel.GetDeclaredSymbol(enclosingMethod, cancellationToken) is IMethodSymbol callerMethodSymbol
                    && !callerMethodSymbol.IsAsync)
                {
                    documentEditor.SetModifiers(enclosingMethod, DeclarationModifiers.From(calleeMethodSymbol).WithAsync(isAsync: true));
                }
            }

            if (removeCalleeDeclarationNode)
            {
                var documentId = solution.GetDocumentId(calleeMethodDeclarationNode.SyntaxTree);
                if (documentId != null)
                {
                    var editor = await solutionEditor.GetDocumentEditorAsync(documentId, cancellationToken).ConfigureAwait(false);
                    editor.RemoveNode(calleeMethodDeclarationNode);
                }
            }

            return solutionEditor.GetChangedSolution();
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
                if (ShouldConsideredAsContainingStatement(node))
                {
                    return node;
                }
            }

            return null;
        }

        #region CodeActions

        private class MyDocumentChangeCodeAction : CodeAction.DocumentChangeAction
        {
            public MyDocumentChangeCodeAction(
                string title,
                Func<CancellationToken, Task<Document>> createChangedDocument,
                string? equivalenceKey = null) : base(title, createChangedDocument, equivalenceKey)
            {
            }
        }

        private class MySolutionChangeAction : CodeAction.SolutionChangeAction
        {
            public MySolutionChangeAction(string title, Func<CancellationToken, Task<Solution>> createChangedSolution, string? equivalenceKey = null) : base(title, createChangedSolution, equivalenceKey)
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
        #endregion
    }
}
