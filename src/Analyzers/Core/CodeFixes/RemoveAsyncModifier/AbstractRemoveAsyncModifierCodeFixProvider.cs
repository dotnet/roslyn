// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.RemoveAsyncModifier;

internal abstract class AbstractRemoveAsyncModifierCodeFixProvider<TReturnStatementSyntax, TExpressionSyntax> : SyntaxEditorBasedCodeFixProvider
    where TReturnStatementSyntax : SyntaxNode
    where TExpressionSyntax : SyntaxNode
{
    protected abstract bool IsAsyncSupportingFunctionSyntax(SyntaxNode node);
    protected abstract SyntaxNode RemoveAsyncModifier(SyntaxGenerator generator, SyntaxNode methodLikeNode);
    protected abstract SyntaxNode? ConvertToBlockBody(SyntaxNode node, TExpressionSyntax expressionBody);

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var document = context.Document;
        var cancellationToken = context.CancellationToken;
        var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
        var knownTypes = new KnownTaskTypes(compilation);

        var diagnostic = context.Diagnostics.First();
        var token = diagnostic.Location.FindToken(cancellationToken);
        var node = token.GetAncestor(IsAsyncSupportingFunctionSyntax);
        if (node == null)
        {
            return;
        }

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var methodSymbol = GetMethodSymbol(node, semanticModel, cancellationToken);

        if (methodSymbol == null)
        {
            return;
        }

        if (ShouldOfferFix(methodSymbol.ReturnType, knownTypes))
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    CodeFixesResources.Remove_async_modifier,
                    GetDocumentUpdater(context),
                    nameof(CodeFixesResources.Remove_async_modifier)),
                context.Diagnostics);
        }
    }

    protected sealed override async Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor, CancellationToken cancellationToken)
    {
        var solutionServices = document.Project.Solution.Services;
        var generator = editor.Generator;
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var compilation = semanticModel.Compilation;
        var knownTypes = new KnownTaskTypes(compilation);

        // For fix all we need to do nested locals or lambdas first, so order the diagnostics by location descending
        foreach (var diagnostic in diagnostics.OrderByDescending(d => d.Location.SourceSpan.Start))
        {
            var token = diagnostic.Location.FindToken(cancellationToken);
            var node = token.GetAncestor(IsAsyncSupportingFunctionSyntax);
            if (node == null)
            {
                Debug.Fail("We should always be able to find the node from the diagnostic.");
                continue;
            }

            var methodSymbol = GetMethodSymbol(node, semanticModel, cancellationToken);
            if (methodSymbol == null)
            {
                Debug.Fail("We should always be able to find the method symbol for the diagnostic.");
                continue;
            }

            // We might need to perform control flow analysis as part of the fix, so we need to do it on the original node
            // so do it up front. Nothing in the fixer changes the reachability of the end of the method so this is safe
            var controlFlow = GetControlFlowAnalysis(generator, semanticModel, node);
            // If control flow couldn't be computed then its probably an empty block, which means we need to add a return anyway
            var needsReturnStatementAdded = controlFlow == null || controlFlow.EndPointIsReachable;

            editor.ReplaceNode(node,
                (updatedNode, generator) => RemoveAsyncModifier(
                    solutionServices, syntaxFacts, generator, updatedNode, methodSymbol.ReturnType, knownTypes, needsReturnStatementAdded));
        }
    }

    private static IMethodSymbol? GetMethodSymbol(SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken)
        => semanticModel.GetSymbolInfo(node, cancellationToken).Symbol as IMethodSymbol ??
           semanticModel.GetDeclaredSymbol(node, cancellationToken) as IMethodSymbol;

    private static bool ShouldOfferFix(ITypeSymbol returnType, KnownTaskTypes knownTypes)
        => IsTaskType(returnType, knownTypes)
            || returnType.OriginalDefinition.Equals(knownTypes.TaskOfTType)
            || returnType.OriginalDefinition.Equals(knownTypes.ValueTaskOfTType);

    private static bool IsTaskType(ITypeSymbol returnType, KnownTaskTypes knownTypes)
        => returnType.OriginalDefinition.Equals(knownTypes.TaskType)
            || returnType.OriginalDefinition.Equals(knownTypes.ValueTaskType);

    private SyntaxNode RemoveAsyncModifier(
        SolutionServices solutionServices,
        ISyntaxFacts syntaxFacts,
        SyntaxGenerator generator,
        SyntaxNode node,
        ITypeSymbol returnType,
        KnownTaskTypes knownTypes,
        bool needsReturnStatementAdded)
    {
        node = RemoveAsyncModifier(generator, node);

        var expression = generator.GetExpression(node);
        if (expression is TExpressionSyntax expressionBody)
        {
            if (IsTaskType(returnType, knownTypes))
            {
                // We need to add a `return Task.CompletedTask;` so we have to convert to a block body
                var blockBodiedNode = ConvertToBlockBody(node, expressionBody);

                // Expression bodied members can't have return statements so if we can't convert to a block
                // body then we've done all we can
                if (blockBodiedNode != null)
                {
                    node = AddReturnStatement(generator, blockBodiedNode);
                }
            }
            else
            {
                // For Task<T> returning expression bodied methods we can just wrap the whole expression
                var newExpressionBody = WrapExpressionWithTaskFromResult(generator, expressionBody, returnType, knownTypes);
                node = generator.WithExpression(node, newExpressionBody);
            }
        }
        else
        {
            if (IsTaskType(returnType, knownTypes))
            {
                // If the end of the method isn't reachable, or there were no statements to analyze, then we
                // need to add an explicit return
                if (needsReturnStatementAdded)
                {
                    node = AddReturnStatement(generator, node);
                }
            }
        }

        return ChangeReturnStatements(solutionServices, syntaxFacts, generator, node, returnType, knownTypes);
    }

    private static ControlFlowAnalysis? GetControlFlowAnalysis(SyntaxGenerator generator, SemanticModel semanticModel, SyntaxNode node)
    {
        var statements = generator.GetStatements(node);
        if (statements.Count > 0)
        {
            return semanticModel.AnalyzeControlFlow(statements[0], statements[statements.Count - 1]);
        }

        return null;
    }

    private static SyntaxNode AddReturnStatement(SyntaxGenerator generator, SyntaxNode node)
        => generator.WithStatements(node, generator.GetStatements(node).Concat(generator.ReturnStatement()));

    private SyntaxNode ChangeReturnStatements(
        SolutionServices solutionServices,
        ISyntaxFacts syntaxFacts,
        SyntaxGenerator generator,
        SyntaxNode node,
        ITypeSymbol returnType,
        KnownTaskTypes knownTypes)
    {
        var editor = new SyntaxEditor(node, solutionServices);

        // Look for all return statements, but if we find a new node that can have the async modifier we stop
        // because that will have its own diagnostic and fix, if applicable
        var returns = node.DescendantNodes(n => n == node || !IsAsyncSupportingFunctionSyntax(n)).OfType<TReturnStatementSyntax>();

        foreach (var returnSyntax in returns)
        {
            var returnExpression = syntaxFacts.GetExpressionOfReturnStatement(returnSyntax);
            if (returnExpression is null)
            {
                // Convert return; into return Task.CompletedTask;
                var returnTaskCompletedTask = GetReturnTaskCompletedTaskStatement(generator, returnType, knownTypes);
                editor.ReplaceNode(returnSyntax, returnTaskCompletedTask);
            }
            else
            {
                // Convert return <expr>; into return Task.FromResult(<expr>);
                var newExpression = WrapExpressionWithTaskFromResult(generator, returnExpression, returnType, knownTypes);
                editor.ReplaceNode(returnExpression, newExpression);
            }
        }

        return editor.GetChangedRoot();
    }

    private static SyntaxNode GetReturnTaskCompletedTaskStatement(SyntaxGenerator generator, ITypeSymbol returnType, KnownTaskTypes knownTypes)
    {
        SyntaxNode invocation;
        if (returnType.OriginalDefinition.Equals(knownTypes.TaskType))
        {
            var taskTypeExpression = TypeExpressionForStaticMemberAccess(generator, knownTypes.TaskType);
            invocation = generator.MemberAccessExpression(taskTypeExpression, nameof(Task.CompletedTask));
        }
        else
        {
            invocation = generator.ObjectCreationExpression(knownTypes.ValueTaskType!);
        }

        var statement = generator.ReturnStatement(invocation);
        return statement;
    }

    private static SyntaxNode WrapExpressionWithTaskFromResult(SyntaxGenerator generator, SyntaxNode expression, ITypeSymbol returnType, KnownTaskTypes knownTypes)
    {
        if (returnType.OriginalDefinition.Equals(knownTypes.TaskOfTType))
        {
            var taskTypeExpression = TypeExpressionForStaticMemberAccess(generator, knownTypes.TaskType!);
            var unwrappedReturnType = returnType.GetTypeArguments()[0];
            var memberName = generator.GenericName(nameof(Task.FromResult), unwrappedReturnType);
            var taskFromResult = generator.MemberAccessExpression(taskTypeExpression, memberName);
            return generator.InvocationExpression(taskFromResult, expression.WithoutTrivia()).WithTriviaFrom(expression);
        }
        else
        {
            return generator.ObjectCreationExpression(returnType, expression);
        }
    }

    // Workaround for https://github.com/dotnet/roslyn/issues/43950
    // Copied from https://github.com/dotnet/roslyn-analyzers/blob/f24a5b42c85be6ee572f3a93bef223767fbefd75/src/Utilities/Workspaces/SyntaxGeneratorExtensions.cs#L68-L74
    private static SyntaxNode TypeExpressionForStaticMemberAccess(SyntaxGenerator generator, INamedTypeSymbol typeSymbol)
    {
        var qualifiedNameSyntaxKind = generator.QualifiedName(generator.IdentifierName("ignored"), generator.IdentifierName("ignored")).RawKind;
        var memberAccessExpressionSyntaxKind = generator.MemberAccessExpression(generator.IdentifierName("ignored"), "ignored").RawKind;

        var typeExpression = generator.TypeExpression(typeSymbol);
        return QualifiedNameToMemberAccess(qualifiedNameSyntaxKind, memberAccessExpressionSyntaxKind, typeExpression, generator);

        // Local function
        static SyntaxNode QualifiedNameToMemberAccess(int qualifiedNameSyntaxKind, int memberAccessExpressionSyntaxKind, SyntaxNode expression, SyntaxGenerator generator)
        {
            if (expression.RawKind == qualifiedNameSyntaxKind)
            {
                var left = QualifiedNameToMemberAccess(qualifiedNameSyntaxKind, memberAccessExpressionSyntaxKind, expression.ChildNodes().First(), generator);
                var right = expression.ChildNodes().Last();
                return generator.MemberAccessExpression(left, right);
            }

            return expression;
        }
    }
}
