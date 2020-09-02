// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.InlineMethod
{
    internal abstract partial class AbstractInlineMethodRefactoringProvider<
            TMethodDeclarationSyntax,
            TStatementSyntax,
            TExpressionSyntax,
            TInvocationSyntax>
        : CodeRefactoringProvider
        where TMethodDeclarationSyntax : SyntaxNode
        where TStatementSyntax : SyntaxNode
        where TExpressionSyntax : SyntaxNode
        where TInvocationSyntax : TExpressionSyntax
    {
        /// <summary>
        /// A preferred name used to generated a declaration when the
        /// inline method's body is not a valid expresion in ExpressionStatement
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
        /// '1' is not a valid expression in ExpressionStatement so a declaration is needed to be generated.
        /// </summary>
        private const string TemporaryName = "temp";

        private readonly ISyntaxFacts _syntaxFacts;
        private readonly ISemanticFactsService _semanticFactsService;

        protected abstract TExpressionSyntax? GetRawInlineExpression(TMethodDeclarationSyntax calleeMethodDeclarationSyntaxNode);
        protected abstract SyntaxNode? GetEnclosingMethodLikeNode(SyntaxNode syntaxNode);
        protected abstract SyntaxNode GenerateTypeSyntax(ITypeSymbol symbol, bool allowVar);
        protected abstract TStatementSyntax ConvertToStatement(TExpressionSyntax expression, bool createReturnStatement);
        protected abstract TExpressionSyntax GenerateLiteralExpression(ITypeSymbol typeSymbol, object? value);
        protected abstract bool IsMethodWithExpressionBody(SyntaxNode callerNode);

        /// <summary>
        /// Check if <paramref name="expressionNode"/> could be used as an Expression in ExpressionStatement
        /// </summary>
        protected abstract bool IsValidExpressionUnderExpressionStatement(TExpressionSyntax expressionNode);

        /// <summary>
        /// Check if <paramref name="syntaxNode"/> could be replaced by ThrowExpression.
        /// For VB it always return false because ThrowExpression doesn't exist.
        /// </summary>
        protected abstract bool CanBeReplacedByThrowExpression(SyntaxNode syntaxNode);

        protected AbstractInlineMethodRefactoringProvider(
            ISyntaxFacts syntaxFacts,
            ISemanticFactsService semanticFactsService)
        {
            _syntaxFacts = syntaxFacts;
            _semanticFactsService = semanticFactsService;
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, _, cancellationToken) = context;
            var calleeInvocationNode = await context.TryGetRelevantNodeAsync<TInvocationSyntax>().ConfigureAwait(false);
            if (calleeInvocationNode == null)
            {
                return;
            }

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var calleeMethodSymbol = semanticModel.GetSymbolInfo(calleeInvocationNode, cancellationToken).GetAnySymbol() as IMethodSymbol;
            if (calleeMethodSymbol == null)
            {
                return;
            }

            if (!calleeMethodSymbol.IsOrdinaryMethod() && !calleeMethodSymbol.IsExtensionMethod)
            {
                return;
            }

            if (calleeMethodSymbol.DeclaredAccessibility != Accessibility.Private)
            {
                return;
            }

            var symbolDeclarationService = document.GetRequiredLanguageService<ISymbolDeclarationService>();
            var calleeMethodDeclarationSyntaxReferences = symbolDeclarationService.GetDeclarations(calleeMethodSymbol);
            if (calleeMethodDeclarationSyntaxReferences.Length != 1)
            {
                return;
            }

            var calleeMethodDeclarationSyntaxReference = calleeMethodDeclarationSyntaxReferences[0];
            var calleeMethodNode = await calleeMethodDeclarationSyntaxReference.GetSyntaxAsync(cancellationToken).ConfigureAwait(false) as TMethodDeclarationSyntax;
            if (calleeMethodNode == null)
            {
                return;
            }

            var inlineExpression = GetRawInlineExpression(calleeMethodNode);
            // Special case 1: AwaitExpression
            if (_syntaxFacts.IsAwaitExpression(inlineExpression))
            {
                // 1. If Caller & callee both have 'await' make sure there is no duplicate 'await'
                // Example:
                // Before:
                // async Task Caller() => await Callee();
                // async Task Callee() => await Task.CompletedTask;
                // After:
                // async Task Caller() => await Task.CompletedTask;
                // async Task Callee() => await Task.CompletedTask;
                // The original inline expression in callee will be 'await Task.CompletedTask'
                // The caller just need 'Task.CompletedTask' without the 'await'
                //
                // 2. If Caller doesn't have await but callee has.
                // Example:
                // Before:
                // void Caller() { Callee().Wait();}
                // async Task Callee() => await DoAsync();
                // After:
                // void Caller() { DoAsync().Wait(); }
                // async Task Callee() => await DoAsync();
                // What caller is expecting is an expression returns 'Task', which doesn't include the 'await'
                inlineExpression = _syntaxFacts.GetExpressionOfAwaitExpression(inlineExpression) as TExpressionSyntax;
            }

            if (inlineExpression == null)
            {
                return;
            }

            // Special case 2: ThrowStatement & ThrowExpresion
            if (_syntaxFacts.IsThrowStatement(inlineExpression.Parent) || _syntaxFacts.IsThrowExpression(inlineExpression))
            {
                // If this is a throw statement, then it should be valid for
                // 1. If it is invoked as ExpressionStatement
                // Example:
                // Before:
                // void Caller() { Callee(); }
                // void Callee() { throw new Exception();}
                // After:
                // void Caller() { throw new Exception(); }
                // void Callee() { throw new Exception();}
                // 2. If it is invoked in a place allow throw expression
                // Example:
                // Before:
                // void Caller(bool flag) { var x = flag ? Callee() : 1; }
                // int Callee() { throw new Exception();}
                // After:
                // void Caller() { var x = flag ? throw new Exception() : 1; }
                // int Callee() { throw new Exception();}
                // Note here throw statement is changed to throw expression after inlining
                // If this is a throw expression, the check is the same
                // 1. If it is invoked as ExpressionStatement
                // Example:
                // Before:
                // void Caller() { Callee(); }
                // void Callee() => throw new Exception();
                // After:
                // void Caller() { throw new Exception(); }
                // void Callee() => throw new Exception();
                // Note here throw expression is converted to throw statement
                // 2. If it is invoked in a place allow throw expression
                // Example:
                // Before:
                // void Caller(bool flag) { var x = flag ? Callee() : 1; }
                // int Callee() => throw new Exception();
                // After:
                // void Caller() { var x = flag ? throw new Exception() : 1; }
                // int Callee() => throw new Exception();
                if (!CanBeReplacedByThrowExpression(calleeInvocationNode)
                    && !_syntaxFacts.IsExpressionStatement(calleeInvocationNode.Parent))
                {
                    return;
                }
            }

            var callerSymbol = GetCallerSymbol(calleeInvocationNode, semanticModel, cancellationToken);
            if (callerSymbol == null)
            {
                return;
            }

            var callerReferences = symbolDeclarationService.GetDeclarations(callerSymbol);
            if (callerReferences.Length != 1)
            {
                return;
            }

            var callerDeclarationNode = await callerReferences[0].GetSyntaxAsync(cancellationToken).ConfigureAwait(false);

            // Either the containing statement node of the invocation can be found,
            // example:
            // void Caller() { int a = Callee(); }
            // int Callee() { return Foo();}
            // (This is to make sure later more statements could be inserted in caller)
            // Or the Caller is method using arrow expression or single-line lambda
            // example:
            // void Caller() => Callee();
            // int Callee() => 1;
            // (This is to make sure later the arrow expresion could be changed to block if more statements needs to be inserted to caller)
            var statementSyntaxContainsCallee = calleeInvocationNode.GetAncestor<TStatementSyntax>();
            if (statementSyntaxContainsCallee == null && !IsMethodWithExpressionBody(callerDeclarationNode))
            {
                return;
            }

            var invocationOperation = semanticModel.GetOperation(calleeInvocationNode, cancellationToken) as IInvocationOperation;
            if (invocationOperation == null)
            {
                return;
            }

            var codeActions = GenerateCodeActions(
                document,
                calleeInvocationNode,
                calleeMethodSymbol,
                calleeMethodNode,
                inlineExpression,
                callerSymbol,
                callerDeclarationNode,
                invocationOperation);

            var nestedCodeAction = new CodeAction.CodeActionWithNestedActions(
                string.Format(FeaturesResources.Inline_0, calleeMethodSymbol.ToNameDisplayString()),
                codeActions,
                isInlinable: true);

            context.RegisterRefactoring(nestedCodeAction, calleeInvocationNode.Span);
        }

        private ImmutableArray<CodeAction> GenerateCodeActions(
            Document document,
            TInvocationSyntax calleeMethodInvocationNode,
            IMethodSymbol calleeMethodSymbol,
            TMethodDeclarationSyntax calleeMethodNode,
            TExpressionSyntax inlineExpression,
            ISymbol callerSymbol,
            SyntaxNode callerMethodNode,
            IInvocationOperation invocationOperation)
        {
            var calleeMethodName = calleeMethodSymbol.ToNameDisplayString();
            var codeActionKeepsCallee = new MySolutionChangeAction(
                string.Format(FeaturesResources.Inline_and_keep_0, calleeMethodName),
                cancellationToken =>
                    InlineMethodAsync(document,
                        calleeMethodInvocationNode,
                        calleeMethodSymbol,
                        calleeMethodNode,
                        inlineExpression,
                        callerSymbol,
                        callerMethodNode,
                        invocationOperation,
                        removeCalleeDeclarationNode: false,
                        cancellationToken));

            var codeActionRemovesCallee = new MySolutionChangeAction(
                string.Format(FeaturesResources.Inline_0, calleeMethodName),
                cancellationToken =>
                    InlineMethodAsync(
                        document,
                        calleeMethodInvocationNode,
                        calleeMethodSymbol,
                        calleeMethodNode,
                        inlineExpression,
                        callerSymbol,
                        callerMethodNode,
                        invocationOperation,
                        removeCalleeDeclarationNode: true,
                        cancellationToken));

            return ImmutableArray.Create<CodeAction>(codeActionRemovesCallee, codeActionKeepsCallee);
        }

        private async Task<Solution> InlineMethodAsync(
            Document document,
            TInvocationSyntax calleeInvocationNode,
            IMethodSymbol calleeMethodSymbol,
            TMethodDeclarationSyntax calleeMethodNode,
            TExpressionSyntax rawInlineExpression,
            ISymbol callerSymbol,
            SyntaxNode callerNode,
            IInvocationOperation invocationOperation,
            bool removeCalleeDeclarationNode,
            CancellationToken cancellationToken)
        {
            var methodParametersInfo = await GetMethodParametersInfoAsync(
                document,
                calleeInvocationNode,
                calleeMethodNode,
                rawInlineExpression,
                invocationOperation,
                cancellationToken).ConfigureAwait(false);

            var inlineContext = await GetInlineMethodContextAsync(
                document,
                calleeMethodNode,
                calleeInvocationNode,
                calleeMethodSymbol,
                rawInlineExpression,
                methodParametersInfo,
                cancellationToken).ConfigureAwait(false);

            var solution = document.Project.Solution;
            var solutionEditor = new SolutionEditor(solution);
            if (removeCalleeDeclarationNode)
            {
                var calleeDocumentId = solution.GetDocumentId(calleeMethodNode.SyntaxTree);
                if (calleeDocumentId != null)
                {
                    var calleeDocumentEditor = await solutionEditor.GetDocumentEditorAsync(calleeDocumentId, cancellationToken).ConfigureAwait(false);
                    calleeDocumentEditor.RemoveNode(calleeMethodNode);
                }
            }

            var newCallerMethodNode = await GetChangedCallerAsync(
                document,
                calleeInvocationNode,
                calleeMethodSymbol,
                rawInlineExpression,
                callerSymbol,
                callerNode,
                methodParametersInfo,
                inlineContext, cancellationToken).ConfigureAwait(false);

            var callerDocumentEditor = await solutionEditor.GetDocumentEditorAsync(document.Id, cancellationToken).ConfigureAwait(false);
            callerDocumentEditor.ReplaceNode(callerNode, newCallerMethodNode);
            return solutionEditor.GetChangedSolution();
        }

        private async Task<SyntaxNode> GetChangedCallerAsync(
            Document document,
            TInvocationSyntax calleeInvocationNode,
            IMethodSymbol calleeMethodSymbol,
            TExpressionSyntax rawInlineExpression,
            ISymbol callerSymbol,
            SyntaxNode callerDeclarationNode,
            MethodParametersInfo methodParametersInfo,
            InlineMethodContext inlineMethodContext,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var syntaxGenerator = SyntaxGenerator.GetGenerator(document);
            var callerNodeEditor = new SyntaxEditor(callerDeclarationNode, syntaxGenerator);
            var statementsNeedToInsert = inlineMethodContext.StatementsToInsertBeforeInvocationOfCallee;

            if (inlineMethodContext.ContainsAwaitExpression)
            {
                // If the inline content has 'await' expression, then make sure the caller is changed to 'async' method
                // if its return type is awaitable. In all other cases, do nothing.
                if (callerSymbol is IMethodSymbol callerMethodSymbol
                    && !callerMethodSymbol.IsAsync
                    && (callerMethodSymbol.ReturnsVoid
                        || callerMethodSymbol.IsAwaitableNonDynamic(semanticModel, callerDeclarationNode.SpanStart)))
                {
                    var declarationModifiers = DeclarationModifiers.From(callerSymbol).WithAsync(true);
                    callerNodeEditor.SetModifiers(callerDeclarationNode, declarationModifiers);
                }
            }

            // Check for a few special cases which the inline node is generated statement,
            // replacement node is the containing node
            var inlineStatementNode = GetSpecialInlineStatementNode(
                calleeInvocationNode,
                calleeMethodSymbol,
                rawInlineExpression,
                methodParametersInfo,
                inlineMethodContext,
                semanticModel,
                syntaxGenerator,
                cancellationToken);

            // For method with block body there will be a statement wrapping the invocation,
            var statementSyntaxContainingCallee = calleeInvocationNode.GetAncestor<TStatementSyntax>();
            if (statementSyntaxContainingCallee != null)
            {
                foreach (var statement in statementsNeedToInsert)
                {
                    callerNodeEditor.InsertBefore(statementSyntaxContainingCallee, statement);
                }

                if (inlineStatementNode != null)
                {
                    callerNodeEditor.ReplaceNode(
                        statementSyntaxContainingCallee,
                        inlineStatementNode.WithTriviaFrom(statementSyntaxContainingCallee));
                }
                else
                {
                    // Replace the invocation with inlineExpression
                    var inlineExpression = GetInlineExpression(
                        calleeInvocationNode,
                        calleeMethodSymbol,
                        rawInlineExpression,
                        inlineMethodContext,
                        syntaxGenerator);
                    callerNodeEditor.ReplaceNode(calleeInvocationNode, inlineExpression);
                }

                return callerNodeEditor.GetChangedRoot();
            }

            // In case it can't find the wrapping statementSyntax for the invocation node,
            // Check the method is arrow function or lambda,
            // Like:
            // void Caller() => Callee();
            // void Callee() {}
            if (IsMethodWithExpressionBody(callerDeclarationNode))
            {
                var expressionBody = (TExpressionSyntax)syntaxGenerator.GetExpression(callerDeclarationNode);
                // If it is going to inline statement to caller,
                // change it from ArrowExpression to Block body
                if (inlineStatementNode != null)
                {
                    callerNodeEditor.RemoveNode(expressionBody.Parent);
                    callerNodeEditor.SetStatements(callerDeclarationNode, statementsNeedToInsert.Concat(inlineStatementNode));
                    return callerNodeEditor.GetChangedRoot();
                }

                // Replace the invocation with inlineExpression
                var inlineExpression = GetInlineExpression(
                    calleeInvocationNode,
                    calleeMethodSymbol,
                    rawInlineExpression,
                    inlineMethodContext,
                    syntaxGenerator);
                // If there are statements to insert
                // change it from ArrowExpression to Block body
                if (statementsNeedToInsert.Length > 0)
                {
                    var newExpressionBody = expressionBody.ReplaceNode(calleeInvocationNode, inlineExpression);
                    var newStatement = ConvertToStatement(
                        newExpressionBody,
                        createReturnStatement: callerSymbol is IMethodSymbol callerMethodSymbol && !callerMethodSymbol.ReturnsVoid);

                    callerNodeEditor.RemoveNode(expressionBody.Parent);
                    callerNodeEditor.SetStatements(callerDeclarationNode, statementsNeedToInsert.Concat(newStatement));
                    return callerNodeEditor.GetChangedRoot();
                }
                else
                {
                    callerNodeEditor.ReplaceNode(calleeInvocationNode, inlineExpression);
                    return callerNodeEditor.GetChangedRoot();
                }
            }

            // Check has been done to not reach here
            throw ExceptionUtilities.Unreachable;
        }

        private TStatementSyntax? GetSpecialInlineStatementNode(
            TInvocationSyntax calleeInvocationNode,
            IMethodSymbol calleeMethodSymbol,
            TExpressionSyntax rawInlineExpression,
            MethodParametersInfo methodParametersInfo,
            InlineMethodContext inlineMethodContext,
            SemanticModel semanticModel,
            SyntaxGenerator syntaxGenerator,
            CancellationToken cancellationToken)
        {
            if (methodParametersInfo.MergeInlineContentAndVariableDeclarationArgument)
            {
                var rightHandSideValue = _syntaxFacts.GetRightHandSideOfAssignment(inlineMethodContext.InlineExpression);
                var (parameterSymbol, name) = methodParametersInfo.ParametersWithVariableDeclarationArgument.Single();
                var declarationNode = (TStatementSyntax)syntaxGenerator
                    .LocalDeclarationStatement(parameterSymbol.Type, name, rightHandSideValue);
                return declarationNode;
            }

            if (_syntaxFacts.IsThrowStatement(rawInlineExpression.Parent)
                && _syntaxFacts.IsExpressionStatement(calleeInvocationNode.Parent))
            {
                var throwStatement = (TStatementSyntax)syntaxGenerator
                    .ThrowStatement(inlineMethodContext.InlineExpression);
                return throwStatement;
            }

            if (_syntaxFacts.IsThrowExpression(rawInlineExpression)
                && _syntaxFacts.IsExpressionStatement(calleeInvocationNode.Parent))
            {
                // Example:
                // Before:
                // void Caller() { Callee(); }
                // void Callee() => throw new Exception();
                // After:
                // void Caller() { throw new Exception(); }
                // void Callee() => throw new Exception();
                // Note: Throw expression is converted to throw statement
                var throwStatement = (TStatementSyntax)syntaxGenerator
                    .ThrowStatement(_syntaxFacts.GetExpressionOfThrowExpression(inlineMethodContext.InlineExpression));
                return throwStatement;
            }

            if (_syntaxFacts.IsExpressionStatement(calleeInvocationNode.Parent)
                && !calleeMethodSymbol.ReturnsVoid
                && !IsValidExpressionUnderExpressionStatement(inlineMethodContext.InlineExpression))
            {
                // If the callee is invoked as ExpressionStatement, but the inlined expression in the callee can't be
                // placed under ExpressionStatement
                // Example:
                // void Caller()
                // {
                //     Callee();
                // }
                // int Callee()
                // {
                //     return 1;
                // };
                // After it should be:
                // void Caller()
                // {
                //     int temp = 1;
                // }
                // int Callee()
                // {
                //     return 1;
                // };
                // One variable declaration needs to be generated.
                var unusedLocalName =
                    _semanticFactsService.GenerateUniqueLocalName(
                        semanticModel,
                        calleeInvocationNode,
                        containerOpt: null,
                        TemporaryName,
                        cancellationToken);

                var localDeclarationNode = (TStatementSyntax)syntaxGenerator
                    .LocalDeclarationStatement(calleeMethodSymbol.ReturnType, unusedLocalName.Text, inlineMethodContext.InlineExpression);
                return localDeclarationNode;
            }

            return null;
        }

        private TExpressionSyntax GetInlineExpression(
            TInvocationSyntax calleeInvocationNode,
            IMethodSymbol calleeMethodSymbol,
            TExpressionSyntax rawInlineExpression,
            InlineMethodContext inlineMethodContext,
            SyntaxGenerator syntaxGenerator)
        {
            if (_syntaxFacts.IsThrowStatement(rawInlineExpression.Parent))
            {
                // Example:
                // Before:
                // void Caller() => Callee();
                // void Callee() { throw new Exception(); }
                // After:
                // void Caller() => throw new Exception();
                // void Callee() { throw new Exception(); }
                // Note: Throw statement is converted to throw expression
                if (CanBeReplacedByThrowExpression(calleeInvocationNode))
                {
                    var throwExpression = (TExpressionSyntax)syntaxGenerator
                        .ThrowExpression(inlineMethodContext.InlineExpression)
                        .WithTriviaFrom(calleeInvocationNode);
                    return throwExpression;
                }
            }

            var inlineExpression = inlineMethodContext.InlineExpression;
            if (!calleeMethodSymbol.ReturnsVoid &&
                !_syntaxFacts.IsThrowExpression(inlineMethodContext.InlineExpression))
            {
                // Add type cast and parenthesis to the inline expression.
                // It is required to cover cases like:
                // Case 1 (parenthesis added):
                // Before:
                // void Caller() { var x = 3 * Callee(); }
                // int Callee() { return 1 + 2; }
                //
                // After
                // void Caller() { var x = 3 * (1 + 2); }
                // int Callee() { return 1 + 2; }
                //
                // Case 2 (type cast)
                // Before:
                // void Caller() { var x = Callee(); }
                // long Callee() { return 1 }
                //
                // After
                // void Caller() { var x = (long)1; }
                // int Callee() { return 1; }
                //
                // Case 3 (type cast & additional parenthesis)
                // Before:
                // void Caller() { var x = Callee()(); }
                // Func<int> Callee() { return () => 1; }
                // After:
                // void Caller() { var x = ((Func<int>)(() => 1))(); }
                // Func<int> Callee() { return () => 1; }
                inlineExpression = (TExpressionSyntax)syntaxGenerator.AddParentheses(
                    syntaxGenerator.CastExpression(
                        GenerateTypeSyntax(calleeMethodSymbol.ReturnType, allowVar: false),
                        syntaxGenerator.AddParentheses(inlineMethodContext.InlineExpression)));

            }

            return inlineExpression.WithTriviaFrom(calleeInvocationNode);
        }

        private static ISymbol? GetCallerSymbol(
            TInvocationSyntax calleeMethodInvocationNode,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            for (SyntaxNode? node = calleeMethodInvocationNode; node != null; node = node.Parent)
            {
                var declaredSymbol = semanticModel.GetDeclaredSymbol(node, cancellationToken);
                if (declaredSymbol.IsKind(SymbolKind.Method) || declaredSymbol.IsKind(SymbolKind.Property))
                {
                    return declaredSymbol;
                }
            }

            return null;
        }

        private class MySolutionChangeAction : CodeAction.SolutionChangeAction
        {
            public MySolutionChangeAction(
                string title,
                Func<CancellationToken, Task<Solution>> createChangedSolution)
                : base(title, createChangedSolution, null)
            {
            }
        }
    }
}
