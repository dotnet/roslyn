// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;
using KnownTypes = Microsoft.CodeAnalysis.MakeMethodAsynchronous.AbstractMakeMethodAsynchronousCodeFixProvider.KnownTypes;

namespace Microsoft.CodeAnalysis.RemoveAsyncModifier
{
    internal abstract class AbstractRemoveAsyncModifierCodeFixProvider<TReturnStatementSyntax, TExpressionSyntax> : SyntaxEditorBasedCodeFixProvider
        where TReturnStatementSyntax : SyntaxNode
        where TExpressionSyntax : SyntaxNode
    {
        public static readonly string EquivalenceKey = FeaturesResources.Remove_async_modifier;
        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.Compile;

        protected abstract bool IsAsyncSupportingFunctionSyntax(SyntaxNode node);
        protected abstract bool TryGetExpressionBody(SyntaxNode methodSymbolOpt, out SyntaxNode expression);
        protected abstract bool ShouldOfferFix(IMethodSymbol methodSymbol, KnownTypes knownTypes);
        protected abstract SyntaxNode RemoveAsyncModifier(IMethodSymbol methodSymbolOpt, SyntaxNode node, KnownTypes knownTypes);
        protected abstract SyntaxNode ConvertToBlockBody(SyntaxNode node, SyntaxNode expressionBody, SyntaxEditor editor);
        protected abstract SyntaxNode GetLastChildOfBlock(SyntaxNode node);
        protected abstract ControlFlowAnalysis AnalyzeControlFlow(SemanticModel semanticModel, SyntaxNode originalNode);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;
            var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            var knownTypes = new KnownTypes(compilation);

            var diagnostic = context.Diagnostics.First();
            var token = diagnostic.Location.FindToken(cancellationToken);
            var node = token.GetAncestor(IsAsyncSupportingFunctionSyntax);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var methodSymbol = GetMethodSymbol(node, semanticModel, cancellationToken);

            if (ShouldOfferFix(methodSymbol, knownTypes))
            {
                context.RegisterCodeFix(
                    new MyCodeAction(c => FixAsync(document, diagnostic, c)),
                    context.Diagnostics);
            }
        }

        protected sealed override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var generator = editor.Generator;
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var compilation = semanticModel.Compilation;
            var knownTypes = new KnownTypes(compilation);

            foreach (var diagnostic in diagnostics)
            {
                var token = diagnostic.Location.FindToken(cancellationToken);
                var node = token.GetAncestor(IsAsyncSupportingFunctionSyntax);
                var methodSymbol = GetMethodSymbol(node, semanticModel, cancellationToken);

                RemoveAsyncModifier(editor, semanticModel, node, methodSymbol, knownTypes);
            }
        }

        private static IMethodSymbol GetMethodSymbol(SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken)
            => semanticModel.GetSymbolInfo(node, cancellationToken).Symbol is IMethodSymbol methodSymbol
                ? methodSymbol
                : semanticModel.GetDeclaredSymbol(node, cancellationToken) as IMethodSymbol;

        private void RemoveAsyncModifier(SyntaxEditor editor, SemanticModel semanticModel, SyntaxNode originalNode, IMethodSymbol methodSymbol, KnownTypes knownTypes)
        {
            // SyntaxEditor can't change or remove tokens so we have to replace the whole node for the method
            // which means this edit must come first
            var replacementNode = RemoveAsyncModifier(methodSymbol, originalNode, knownTypes);
            editor.ReplaceNode(originalNode, replacementNode);

            // Now that we've replaced the original node we have to work on the replacement for future work
            if (TryGetExpressionBody(replacementNode, out var expressionBody))
            {
                if (methodSymbol.ReturnType == knownTypes._taskType)
                {
                    // We need to add a `return Task.CompletedTask;` so we have to convert to a block body
                    var blockBodiedNode = ConvertToBlockBody(replacementNode, expressionBody, editor);

                    // Expression bodied members can't have return statements so if we can't convert to a block
                    // body then we've done all we can
                    if (blockBodiedNode != null)
                    {
                        editor.ReplaceNode(replacementNode, blockBodiedNode);
                        // We need to get the block inside the block bodied method to know where to add the return
                        var node = GetLastChildOfBlock(blockBodiedNode);
                        AppendTaskCompletedTaskReturn(node, editor, knownTypes);
                    }
                }
                else
                {
                    // For Task<T> returning expression bodied methods we can just wrap the whole expression
                    WrapExpressionWithTaskFromResult(expressionBody, editor, knownTypes);
                }
            }
            else
            {
                // Block bodied methods might have return statements so update them to task returning
                ChangeReturnStatements(replacementNode, editor, knownTypes);

                // An "async Task" method has an implicit return at the end, so removing async means
                // we need to insert it explicitly if the end of the method is reachable.
                if (methodSymbol.ReturnType == knownTypes._taskType)
                {
                    // We have to use the original node to do control flow analysis, but the reachability of it is the same
                    var controlFlow = AnalyzeControlFlow(semanticModel, originalNode);

                    if (controlFlow != null)
                    {
                        // For local functions and block bodied lambdas the EndPointIsReachable is false but we still might need to
                        // insert a return. We can tell by checking for the presence of exit points that aren't return statements
                        var hasNonReturnExitPoints = controlFlow.ExitPoints.Any<SyntaxNode>(e => !(e is TReturnStatementSyntax));
                        if (controlFlow.EndPointIsReachable || hasNonReturnExitPoints)
                        {
                            var node = GetLastChildOfBlock(replacementNode);
                            AppendTaskCompletedTaskReturn(node, editor, knownTypes);
                        }
                    }
                }
            }
        }

        private void ChangeReturnStatements(SyntaxNode node, SyntaxEditor editor, KnownTypes knownTypes)
        {
            var generator = editor.Generator;

            var returns = node.DescendantNodesAndSelf().Where(n => n is TReturnStatementSyntax);
            foreach (TReturnStatementSyntax returnSyntax in returns)
            {
                // Make sure we're not changing returns in nested local functions or lambdas
                var containingMethod = returnSyntax.FirstAncestorOrSelf<SyntaxNode>(IsAsyncSupportingFunctionSyntax);
                if (containingMethod != node)
                {
                    continue;
                }

                var returnExpression = generator.SyntaxFacts.GetExpressionOfReturnStatement(returnSyntax);
                if (returnExpression is null)
                {
                    // Convert return; into return Task.CompletedTask;
                    var returnTaskCompletedTask = GetReturnTaskCompletedTaskStatement(knownTypes, generator);
                    editor.ReplaceNode(returnSyntax, returnTaskCompletedTask);
                }
                else
                {
                    // Convert return <expr>; into return Task.FromResult(<expr>);
                    WrapExpressionWithTaskFromResult(returnExpression, editor, knownTypes);
                }
            }
        }

        private static void AppendTaskCompletedTaskReturn(SyntaxNode lastNode, SyntaxEditor editor, KnownTypes knownTypes)
        {
            var generator = editor.Generator;
            var returnTaskCompletedTask = GetReturnTaskCompletedTaskStatement(knownTypes, generator);

            editor.InsertAfter(lastNode, returnTaskCompletedTask);
        }

        private static SyntaxNode GetReturnTaskCompletedTaskStatement(KnownTypes knownTypes, SyntaxGenerator generator)
        {
            var taskCompletedTaskInvocation = GetTaskCompletedTaskInvocation(knownTypes, generator);
            var statement = generator.ReturnStatement(taskCompletedTaskInvocation);
            return statement;
        }

        private static TExpressionSyntax GetTaskCompletedTaskInvocation(KnownTypes knownTypes, SyntaxGenerator generator)
        {
            var taskTypeExpression = TypeExpressionForStaticMemberAccess(generator, knownTypes._taskType);
            return (TExpressionSyntax)generator.MemberAccessExpression(taskTypeExpression, nameof(Task.CompletedTask));
        }

        private static void WrapExpressionWithTaskFromResult(SyntaxNode expression, SyntaxEditor editor, KnownTypes knownTypes)
        {
            var generator = editor.Generator;

            var taskTypeExpression = TypeExpressionForStaticMemberAccess(generator, knownTypes._taskType);
            var taskFromResult = generator.MemberAccessExpression(taskTypeExpression, nameof(Task.FromResult));
            var invocation = generator.InvocationExpression(taskFromResult, expression.NormalizeWhitespace());
            editor.ReplaceNode(expression, invocation);
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

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(FeaturesResources.Remove_async_modifier, createChangedDocument, AbstractRemoveAsyncModifierCodeFixProvider<TReturnStatementSyntax, TExpressionSyntax>.EquivalenceKey)
            {
            }
        }
    }
}
