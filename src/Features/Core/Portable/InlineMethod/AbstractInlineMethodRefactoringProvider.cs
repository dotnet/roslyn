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
            TStatementSyntax>
        : CodeRefactoringProvider
        where TExpressionSyntax : SyntaxNode
        where TInvocationSyntax : TExpressionSyntax
        where TMethodDeclarationSyntax : SyntaxNode
        where TStatementSyntax : SyntaxNode
    {
        private readonly ISyntaxFacts _syntaxFacts;
        private readonly ISemanticFactsService _semanticFactsService;

        protected abstract TExpressionSyntax? GetInlineExpression(TMethodDeclarationSyntax calleeMethodDeclarationSyntaxNode);
        protected abstract SyntaxNode? GetEnclosingMethodLikeNode(SyntaxNode syntaxNode);
        protected abstract SyntaxNode GenerateTypeSyntax(ITypeSymbol symbol, bool allowVar);
        protected abstract TExpressionSyntax Parenthesize(TExpressionSyntax expressionNode);
        protected abstract bool TryGetInlineSyntaxNodeAndReplacementNodeForDelegate(
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
            if (_syntaxFacts.IsAwaitExpression(inlineExpression))
            {
                // This will make sure there is no duplicate 'await'
                // Example:
                // Before:
                // async Task Caller() => await Callee();
                // async Task Callee() => await Task.CompletedTask;
                // After:
                // async Task Caller() => await Task.CompletedTask;
                // async Task Callee() => await Task.CompletedTask;
                // The original inline expression in callee will be 'await Task.CompletedTask'
                // The caller just need 'Task.CompletedTask' without the 'await'
                inlineExpression = _syntaxFacts.GetExpressionOfAwaitExpression(inlineExpression) as TExpressionSyntax;
            }

            if (inlineExpression == null)
            {
                return;
            }

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var callerDeclarationNode = _syntaxFacts.GetContainingMemberDeclaration(root, calleeMethodInvocationNode.SpanStart)
                ?? GetEnclosingMethodLikeNode(calleeMethodNode);
            if (callerDeclarationNode == null)
            {
                return;
            }

            var callerSymbol = semanticModel.GetDeclaredSymbol(callerDeclarationNode, cancellationToken)
                ?? semanticModel.GetSymbolInfo(callerDeclarationNode, cancellationToken).GetAnySymbol();
            if (callerSymbol == null)
            {
                return;
            }

            var containsAwaitExpression = ContainsAwaitExpression(inlineExpression, calleeMethodNode);
            if (containsAwaitExpression)
            {
                // If there is nested 'await', check if the caller is a method, and its return type
                // is void or AwaitableType because after move the 'await' to the caller it must be async
                // Example that shouldn't offer refactoring:
                // int Caller()
                // {
                //    var x = Callee();
                //    return 1;
                // }
                // async Task Callee() => await Task.Delay(await Task.FromResult(100));
                // Example that should offer refactoring (because it is safe to change the caller to async):
                // void Caller()
                // {
                //    var x = Callee();
                // }
                // async Task Callee() => await Task.Delay(await Task.FromResult(100));
                var canInlineAwaitExpression = callerSymbol is IMethodSymbol callerMethodSymbol
                       && (callerMethodSymbol.IsAsync ||
                           callerMethodSymbol.ReturnsVoid ||
                           calleeMethodSymbol.IsAwaitableNonDynamic(semanticModel, callerDeclarationNode.SpanStart));
                if (!canInlineAwaitExpression)
                {
                    return;
                }
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
                callerDeclarationNode,
                callerSymbol,
                containsAwaitExpression,
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
            SyntaxNode callerDeclarationNode,
            ISymbol callerSymbol,
            bool containsAwaitExpression,
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
                        callerDeclarationNode,
                        callerSymbol,
                        containsAwaitExpression,
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
                        callerDeclarationNode,
                        callerSymbol,
                        containsAwaitExpression,
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
            SyntaxNode callerDeclarationNode,
            ISymbol callerSymbol,
            bool containsAwaitExpression,
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
                inlineExpression,
                statementContainsCallee,
                methodParametersInfo,
                cancellationToken).ConfigureAwait(false);
            return await ChangeSolutionAsync(
                document,
                calleeMethodNode,
                callerDeclarationNode,
                callerSymbol,
                containsAwaitExpression,
                inlineContext,
                removeCalleeDeclarationNode,
                cancellationToken).ConfigureAwait(false);
        }

        private async Task<Solution> ChangeSolutionAsync(Document document,
            TMethodDeclarationSyntax calleeMethodNode,
            SyntaxNode callerDeclarationNode,
            ISymbol callerSymbol,
            bool containsAwaitExpression,
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

            // If the inline content has 'await' expression, then make sure the caller is converted to 'async' method
            if (containsAwaitExpression)
            {
                // If the inlineExpresion contains await Expression, check has been done to make sure
                // the caller is Method
                var callerMethodSymbol = (IMethodSymbol)callerSymbol;
                if (!callerMethodSymbol.IsAsync)
                {
                    var declarationModifiers = DeclarationModifiers.From(callerSymbol).WithAsync(true);
                    documentEditor.SetModifiers(callerDeclarationNode, declarationModifiers);
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
