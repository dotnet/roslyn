// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.InlineMethod;

internal abstract partial class AbstractInlineMethodRefactoringProvider<
        TMethodDeclarationSyntax,
        TStatementSyntax,
        TExpressionSyntax,
        TInvocationSyntax>(
    ISyntaxFacts syntaxFacts,
    ISemanticFactsService semanticFactsService)
    : CodeRefactoringProvider
    where TMethodDeclarationSyntax : SyntaxNode
    where TStatementSyntax : SyntaxNode
    where TExpressionSyntax : SyntaxNode
    where TInvocationSyntax : TExpressionSyntax
{
    /// <summary>
    /// A preferred name used to generated a declaration when the
    /// inline method's body is not a valid expression in ExpressionStatement
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

    private readonly ISyntaxFacts _syntaxFacts = syntaxFacts;
    private readonly ISemanticFactsService _semanticFactsService = semanticFactsService;

    protected abstract TExpressionSyntax? GetRawInlineExpression(TMethodDeclarationSyntax calleeMethodDeclarationSyntaxNode);
    protected abstract SyntaxNode GenerateTypeSyntax(ITypeSymbol symbol, bool allowVar);
    protected abstract TExpressionSyntax GenerateLiteralExpression(ITypeSymbol typeSymbol, object? value);
    protected abstract bool IsFieldDeclarationSyntax(SyntaxNode node);

    /// <summary>
    /// Check if <paramref name="expressionNode"/> could be used as an Expression in ExpressionStatement
    /// </summary>
    protected abstract bool IsValidExpressionUnderExpressionStatement(TExpressionSyntax expressionNode);

    /// <summary>
    /// Check if <paramref name="syntaxNode"/> could be replaced by ThrowExpression.
    /// For VB it always return false because ThrowExpression doesn't exist.
    /// </summary>
    protected abstract bool CanBeReplacedByThrowExpression(SyntaxNode syntaxNode);

    internal override CodeRefactoringKind Kind => CodeRefactoringKind.Inline;

    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        var (document, _, cancellationToken) = context;
        var calleeMethodInvocationNode = await context.TryGetRelevantNodeAsync<TInvocationSyntax>().ConfigureAwait(false);
        if (calleeMethodInvocationNode == null)
            return;

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel.GetSymbolInfo(calleeMethodInvocationNode, cancellationToken).GetAnySymbol() is not IMethodSymbol calleeMethodSymbol)
            return;

        calleeMethodSymbol = calleeMethodSymbol.PartialImplementationPart ?? calleeMethodSymbol;
        if (!calleeMethodSymbol.IsOrdinaryMethod() && !calleeMethodSymbol.IsExtensionMethod)
            return;

        if (calleeMethodSymbol.IsVararg)
            return;

        if (calleeMethodSymbol.DeclaredAccessibility != Accessibility.Private)
            return;

        var symbolDeclarationService = document.GetRequiredLanguageService<ISymbolDeclarationService>();
        if (symbolDeclarationService.GetDeclarations(calleeMethodSymbol) is not [var calleeMethodDeclarationSyntaxReference])
            return;

        if (await calleeMethodDeclarationSyntaxReference.GetSyntaxAsync(cancellationToken).ConfigureAwait(false) is not TMethodDeclarationSyntax calleeMethodNode)
            return;

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
            return;

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
            if (!CanBeReplacedByThrowExpression(calleeMethodInvocationNode)
                && !_syntaxFacts.IsExpressionStatement(calleeMethodInvocationNode.Parent))
            {
                return;
            }
        }

        var callerSymbol = GetCallerSymbol(calleeMethodInvocationNode, semanticModel, cancellationToken);
        if (callerSymbol == null)
            return;

        if (symbolDeclarationService.GetDeclarations(callerSymbol) is not [var callerReference])
            return;

        var callerDeclarationNode = await callerReference.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel.GetOperation(calleeMethodInvocationNode, cancellationToken) is not IInvocationOperation invocationOperation)
            return;

        var syntaxGenerator = SyntaxGenerator.GetGenerator(document);

        context.RegisterRefactoring(CodeAction.Create(
                string.Format(FeaturesResources.Inline_0, calleeMethodSymbol.ToNameDisplayString()),
                GenerateCodeActions(),
                isInlinable: true),
            calleeMethodInvocationNode.Span);

        ImmutableArray<CodeAction> GenerateCodeActions()
        {
            using var result = TemporaryArray<CodeAction>.Empty;

            var calleeMethodName = calleeMethodSymbol.ToNameDisplayString();

            // For recursive calls (caller and callee are the same method), we can't offer the
            // "Inline_" option because we can't remove a method while also modifying it.
            if (!SymbolEqualityComparer.Default.Equals(callerSymbol, calleeMethodSymbol))
            {
                result.Add(CodeAction.Create(
                    string.Format(FeaturesResources.Inline_0, calleeMethodName),
                    cancellationToken => InlineMethodAsync(
                        removeCalleeDeclarationNode: true,
                        cancellationToken)));
            }

            result.Add(CodeAction.Create(
                string.Format(FeaturesResources.Inline_and_keep_0, calleeMethodName),
                cancellationToken => InlineMethodAsync(
                    removeCalleeDeclarationNode: false,
                    cancellationToken)));

            return result.ToImmutableAndClear();
        }

        async Task<Solution> InlineMethodAsync(
            bool removeCalleeDeclarationNode,
            CancellationToken cancellationToken)
        {
            // Find the statement contains the invocation. This should happen when Callee is invoked in a block
            // example:
            // void Caller()
            // {
            //     Action a = () =>
            //     {
            //         var x = Callee();
            //     }
            // } (Local declaration x is the containing node)
            // Note: Stop the searching when it hits lambda or local function, because for this case below don't
            // treat the declaration of a is the containing node
            // void Caller()
            // {
            //     Action a = () => Callee();
            // }
            // it could be null if the caller is invoked as arrow function
            var statementContainsInvocation = calleeMethodInvocationNode.GetAncestors()
                .TakeWhile(node => !_syntaxFacts.IsAnonymousFunctionExpression(node) && !_syntaxFacts.IsLocalFunctionStatement(node))
                .FirstOrDefault(node => node is TStatementSyntax) as TStatementSyntax;

            var methodParametersInfo = await GetMethodParametersInfoAsync(
                document,
                calleeMethodInvocationNode,
                calleeMethodNode,
                statementContainsInvocation,
                inlineExpression,
                invocationOperation, cancellationToken).ConfigureAwait(false);

            var inlineContext = await GetInlineMethodContextAsync(
                document,
                calleeMethodNode,
                calleeMethodInvocationNode,
                calleeMethodSymbol,
                inlineExpression,
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
                statementContainsInvocation, methodParametersInfo, inlineContext, cancellationToken).ConfigureAwait(false);

            var callerDocumentEditor = await solutionEditor.GetDocumentEditorAsync(document.Id, cancellationToken).ConfigureAwait(false);
            callerDocumentEditor.ReplaceNode(callerDeclarationNode, newCallerMethodNode);

            return solutionEditor.GetChangedSolution();
        }

        async Task<SyntaxNode> GetChangedCallerAsync(
            TStatementSyntax? statementContainsInvocation,
            MethodParametersInfo methodParametersInfo,
            InlineMethodContext inlineMethodContext,
            CancellationToken cancellationToken)
        {
            var callerNodeEditor = new SyntaxEditor(callerDeclarationNode, syntaxGenerator);

            if (inlineMethodContext.ContainsAwaitExpression)
            {
                // If the inline content has 'await' expression, then make sure the caller is changed to 'async' method
                // if its return type is awaitable. In all other cases, do nothing.
                if (callerSymbol is IMethodSymbol { MethodKind: MethodKind.Ordinary, IsAsync: false } callerMethodSymbol
                    && (callerMethodSymbol.ReturnsVoid
                        || callerMethodSymbol.IsAwaitableNonDynamic(semanticModel, callerDeclarationNode.SpanStart)))
                {
                    var declarationModifiers = DeclarationModifiers.From(callerSymbol).WithAsync(true);
                    callerNodeEditor.SetModifiers(callerDeclarationNode, declarationModifiers);
                }
            }

            if (statementContainsInvocation != null)
            {
                foreach (var statement in inlineMethodContext.StatementsToInsertBeforeInvocationOfCallee)
                {
                    // Add a CarriageReturn to make sure for VB the statement would be in different line.
                    callerNodeEditor.InsertBefore(statementContainsInvocation,
                        statement.WithAppendedTrailingTrivia(_syntaxFacts.ElasticCarriageReturnLineFeed));
                }
            }

            var (nodeToReplace, inlineNode) = GetInlineNode(
                semanticModel,
                statementContainsInvocation,
                methodParametersInfo,
                inlineMethodContext,
                cancellationToken);
            callerNodeEditor.ReplaceNode(nodeToReplace, (node, generator) => inlineNode);

            return callerNodeEditor.GetChangedRoot();
        }

        (SyntaxNode nodeToReplace, SyntaxNode inlineNode) GetInlineNode(
            SemanticModel semanticModel,
            TStatementSyntax? statementContainsInvocation,
            MethodParametersInfo methodParametersInfo,
            InlineMethodContext inlineMethodContext,
            CancellationToken cancellationToken)
        {
            if (statementContainsInvocation != null)
            {
                if (methodParametersInfo.MergeInlineContentAndVariableDeclarationArgument)
                {
                    var rightHandSideValue = _syntaxFacts.GetRightHandSideOfAssignment(inlineMethodContext.InlineExpression);
                    var (parameterSymbol, name) = methodParametersInfo.ParametersWithVariableDeclarationArgument.Single();
                    var declarationNode = (TStatementSyntax)syntaxGenerator
                        .LocalDeclarationStatement(parameterSymbol.Type, name, rightHandSideValue);
                    return (statementContainsInvocation, declarationNode.WithTriviaFrom(statementContainsInvocation));
                }

                if (_syntaxFacts.IsThrowStatement(inlineExpression.Parent)
                    && _syntaxFacts.IsExpressionStatement(calleeMethodInvocationNode.Parent))
                {
                    var throwStatement = (TStatementSyntax)syntaxGenerator
                        .ThrowStatement(inlineMethodContext.InlineExpression);
                    return (statementContainsInvocation, throwStatement.WithTriviaFrom(statementContainsInvocation));
                }

                if (_syntaxFacts.IsThrowExpression(inlineExpression)
                    && _syntaxFacts.IsExpressionStatement(calleeMethodInvocationNode.Parent))
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
                    return (statementContainsInvocation, throwStatement.WithTriviaFrom(statementContainsInvocation));
                }

                if (_syntaxFacts.IsExpressionStatement(calleeMethodInvocationNode.Parent)
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
                            calleeMethodInvocationNode,
                            container: null,
                            TemporaryName,
                            cancellationToken);

                    var localDeclarationNode = (TStatementSyntax)syntaxGenerator
                        .LocalDeclarationStatement(calleeMethodSymbol.ReturnType, unusedLocalName.Text,
                            inlineMethodContext.InlineExpression);
                    return (statementContainsInvocation, localDeclarationNode.WithTriviaFrom(statementContainsInvocation));
                }
            }

            if (_syntaxFacts.IsThrowStatement(inlineExpression.Parent))
            {
                // Example:
                // Before:
                // void Caller() => Callee();
                // void Callee() { throw new Exception(); }
                // After:
                // void Caller() => throw new Exception();
                // void Callee() { throw new Exception(); }
                // Note: Throw statement is converted to throw expression
                if (CanBeReplacedByThrowExpression(calleeMethodInvocationNode))
                {
                    var throwExpression = (TExpressionSyntax)syntaxGenerator
                        .ThrowExpression(inlineMethodContext.InlineExpression)
                        .WithTriviaFrom(calleeMethodInvocationNode);
                    return (calleeMethodInvocationNode, throwExpression.WithTriviaFrom(calleeMethodInvocationNode));
                }
            }

            var finalInlineExpression = inlineMethodContext.InlineExpression;
            if (!_syntaxFacts.IsExpressionStatement(calleeMethodInvocationNode.Parent)
                && !calleeMethodSymbol.ReturnsVoid
                && !_syntaxFacts.IsThrowExpression(inlineMethodContext.InlineExpression))
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
                //
                // Also, ensure that the node is formatted properly at the destination location. This is needed as the
                // location of the destination node might be very different (indentation/nesting wise) from the original
                // method where the inlined code is coming from.
                finalInlineExpression = (TExpressionSyntax)syntaxGenerator.AddParentheses(
                    syntaxGenerator.CastExpression(
                        GenerateTypeSyntax(calleeMethodSymbol.ReturnType, allowVar: false),
                        syntaxGenerator.AddParentheses(finalInlineExpression.WithAdditionalAnnotations(Formatter.Annotation))));

            }

            return (calleeMethodInvocationNode, finalInlineExpression.WithTriviaFrom(calleeMethodInvocationNode));
        }
    }

    private ISymbol? GetCallerSymbol(
        TInvocationSyntax calleeMethodInvocationNode,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        for (SyntaxNode? node = calleeMethodInvocationNode; node != null; node = node.Parent)
        {
            var declaredSymbol = semanticModel.GetDeclaredSymbol(node, cancellationToken);
            if (declaredSymbol?.Kind is SymbolKind.Property or SymbolKind.Method or SymbolKind.Event)
                return declaredSymbol;

            if (IsFieldDeclarationSyntax(node))
            {
                foreach (var declarator in node.DescendantNodes().OfType<SyntaxNode>()
                    .Where(n => _syntaxFacts.IsVariableDeclarator(n)))
                {
                    var initializer = _syntaxFacts.GetInitializerOfVariableDeclarator(declarator);
                    if (initializer?.DescendantNodesAndSelf().Contains(calleeMethodInvocationNode) is true &&
                        semanticModel.GetDeclaredSymbol(declarator, cancellationToken) is IFieldSymbol fieldSymbol)
                    {
                        return fieldSymbol;
                    }
                }

                // Fall back to the current approach for the VB case
                if (semanticModel.GetAllDeclaredSymbols(node, cancellationToken).SingleOrDefault() is IFieldSymbol fieldSymbolFallBack)
                    return fieldSymbolFallBack;
            }

            if (_syntaxFacts.IsAnonymousFunctionExpression(node))
                return semanticModel.GetSymbolInfo(node, cancellationToken).Symbol;
        }

        return null;
    }
}
