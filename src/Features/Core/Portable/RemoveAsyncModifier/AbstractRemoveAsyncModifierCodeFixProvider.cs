// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.Compile;

        protected abstract bool IsAsyncSupportingFunctionSyntax(SyntaxNode node);
        protected abstract bool TryGetExpressionBody(SyntaxNode methodSymbolOpt, [NotNullWhen(returnValue: true)] out TExpressionSyntax? expression);
        protected abstract SyntaxNode RemoveAsyncModifier(IMethodSymbol methodSymbolOpt, SyntaxNode node, KnownTypes knownTypes);
        protected abstract SyntaxNode? ConvertToBlockBody(SyntaxNode node, SyntaxNode expressionBody);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;
            var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            var knownTypes = new KnownTypes(compilation);

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
                    new MyCodeAction(c => FixAsync(document, diagnostic, c)),
                    context.Diagnostics);
            }
        }

        protected sealed override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var generator = editor.Generator;
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var compilation = semanticModel.Compilation;
            var knownTypes = new KnownTypes(compilation);

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
                // so do it up front. Nothing in the fixer changes the reachabiliy of the end of the method so this is safe
                var controlFlow = GetControlFlowAnalysis(generator, semanticModel, node);
                // If control flow couldn't be computed then its probably an empty block, which means we need to add a return anyway
                var needsReturnStatementAdded = (controlFlow == null || controlFlow.EndPointIsReachable);

                editor.ReplaceNode(node, (n, generator) =>
                {
                    var subEditor = new SyntaxEditor(n, generator);
                    RemoveAsyncModifier(subEditor, needsReturnStatementAdded, n, methodSymbol, knownTypes);
                    return subEditor.GetChangedRoot();
                });
            }
        }

        private static IMethodSymbol? GetMethodSymbol(SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken)
            => semanticModel.GetSymbolInfo(node, cancellationToken).Symbol is IMethodSymbol methodSymbol
                ? methodSymbol
                : semanticModel.GetDeclaredSymbol(node, cancellationToken) as IMethodSymbol;

        private static bool ShouldOfferFix(ITypeSymbol returnType, KnownTypes knownTypes)
            => IsTaskType(returnType, knownTypes)
                || returnType.OriginalDefinition.Equals(knownTypes._taskOfTType)
                || returnType.OriginalDefinition.Equals(knownTypes._valueTaskOfTTypeOpt);

        private static bool IsTaskType(ITypeSymbol returnType, KnownTypes knownTypes)
            => returnType.OriginalDefinition.Equals(knownTypes._taskType)
                || returnType.OriginalDefinition.Equals(knownTypes._valueTaskType);

        private void RemoveAsyncModifier(SyntaxEditor editor, bool needsReturnStatementAdded, SyntaxNode originalNode, IMethodSymbol methodSymbol, KnownTypes knownTypes)
        {
            var replacementNode = RemoveAsyncModifier(methodSymbol, originalNode, knownTypes);
            editor.ReplaceNode(originalNode, replacementNode);

            if (TryGetExpressionBody(replacementNode, out var expressionBody))
            {
                if (IsTaskType(methodSymbol.ReturnType, knownTypes))
                {
                    // We need to add a `return Task.CompletedTask;` so we have to convert to a block body
                    var blockBodiedNode = ConvertToBlockBody(replacementNode, expressionBody);

                    // Expression bodied members can't have return statements so if we can't convert to a block
                    // body then we've done all we can
                    if (blockBodiedNode != null)
                    {
                        editor.ReplaceNode(replacementNode, blockBodiedNode);

                        AddReturnStatement(editor, blockBodiedNode, methodSymbol.ReturnType, knownTypes);
                    }
                }
                else
                {
                    // For Task<T> returning expression bodied methods we can just wrap the whole expression
                    WrapExpressionWithTaskFromResult(expressionBody, editor, methodSymbol.ReturnType, knownTypes);
                }
            }
            else
            {
                if (IsTaskType(methodSymbol.ReturnType, knownTypes))
                {
                    // If the end of the method isn't reachable, or there were no statements to analyze, then we
                    // need to add an explicit return
                    if (needsReturnStatementAdded)
                    {
                        AddReturnStatement(editor, replacementNode, methodSymbol.ReturnType, knownTypes);
                    }
                }

                ChangeReturnStatements(replacementNode, editor, methodSymbol.ReturnType, knownTypes);
            }
        }

        private static ControlFlowAnalysis? GetControlFlowAnalysis(SyntaxGenerator generator, SemanticModel semanticModel, SyntaxNode originalNode)
        {
            var statements = generator.GetStatements(originalNode);
            if (statements.Count > 0)
            {
                return semanticModel.AnalyzeControlFlow(statements[0], statements[statements.Count - 1]);
            }

            return null;
        }

        private static void AddReturnStatement(SyntaxEditor editor, SyntaxNode replacementNode, ITypeSymbol returnType, KnownTypes knownTypes)
        {
            var generator = editor.Generator;
            var statements = generator.GetStatements(replacementNode).ToImmutableArray();

            var returnStatement = GetReturnTaskCompletedTaskStatement(returnType, knownTypes, generator);

            // We can't just use generator.WithStatements because that breaks nested functions in a Fix All scenario
            // but we can't append if its an empty block (no statements), so we have to support both
            if (statements.Any())
            {
                editor.InsertAfter(statements.Last(), returnStatement);
            }
            else
            {
                // Only need to add a plain "return;" statement, it will be updatedbelow
                var newStatements = statements.Add(returnStatement);
                var newNode = generator.WithStatements(replacementNode, newStatements);
                editor.ReplaceNode(replacementNode, newNode);
            }
        }

        private void ChangeReturnStatements(SyntaxNode node, SyntaxEditor editor, ITypeSymbol returnType, KnownTypes knownTypes)
        {
            var generator = editor.Generator;

            var returns = node.DescendantNodes(n => n == node || !IsAsyncSupportingFunctionSyntax(n)).Where(n => n is TReturnStatementSyntax);
            foreach (TReturnStatementSyntax returnSyntax in returns)
            {
                var returnExpression = generator.SyntaxFacts.GetExpressionOfReturnStatement(returnSyntax);
                if (returnExpression is null)
                {
                    // Convert return; into return Task.CompletedTask;
                    var returnTaskCompletedTask = GetReturnTaskCompletedTaskStatement(returnType, knownTypes, generator);
                    editor.ReplaceNode(returnSyntax, returnTaskCompletedTask);
                }
                else
                {
                    // Convert return <expr>; into return Task.FromResult(<expr>);
                    WrapExpressionWithTaskFromResult(returnExpression, editor, returnType, knownTypes);
                }
            }
        }

        private static SyntaxNode GetReturnTaskCompletedTaskStatement(ITypeSymbol returnType, KnownTypes knownTypes, SyntaxGenerator generator)
        {
            TExpressionSyntax invocation;
            if (returnType.OriginalDefinition.Equals(knownTypes._taskType))
            {
                var taskTypeExpression = TypeExpressionForStaticMemberAccess(generator, knownTypes._taskType);
                invocation = (TExpressionSyntax)generator.MemberAccessExpression(taskTypeExpression, nameof(Task.CompletedTask));
            }
            else
            {
                invocation = (TExpressionSyntax)generator.ObjectCreationExpression(knownTypes._valueTaskType);
            }

            var statement = generator.ReturnStatement(invocation);
            return statement;
        }

        private static void WrapExpressionWithTaskFromResult(SyntaxNode expression, SyntaxEditor editor, ITypeSymbol returnType, KnownTypes knownTypes)
        {
            var generator = editor.Generator;

            TExpressionSyntax invocation;
            if (returnType.OriginalDefinition.Equals(knownTypes._taskOfTType))
            {
                var taskTypeExpression = TypeExpressionForStaticMemberAccess(generator, knownTypes._taskType);
                var taskFromResult = generator.MemberAccessExpression(taskTypeExpression, nameof(Task.FromResult));
                invocation = (TExpressionSyntax)generator.InvocationExpression(taskFromResult, expression.WithoutTrivia()).WithTriviaFrom(expression);
            }
            else
            {
                invocation = (TExpressionSyntax)generator.ObjectCreationExpression(returnType, expression);
            }
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
                : base(FeaturesResources.Remove_async_modifier, createChangedDocument, FeaturesResources.Remove_async_modifier)
            {
            }
        }
    }
}
