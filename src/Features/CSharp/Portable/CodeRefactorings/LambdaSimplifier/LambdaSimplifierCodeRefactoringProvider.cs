// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.LambdaSimplifier
{
    // Disabled due to: https://github.com/dotnet/roslyn/issues/5835 & https://github.com/dotnet/roslyn/pull/6642
    // [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.SimplifyLambda)]
    internal partial class LambdaSimplifierCodeRefactoringProvider : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, textSpan, cancellationToken) = context;
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
            {
                return;
            }

            var semanticDocument = await SemanticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            var lambda = await context.TryGetSelectedNodeAsync<LambdaExpressionSyntax>().ConfigureAwait(false);
            if (lambda == null)
            {
                return;
            }

            if (!CanSimplify(semanticDocument, lambda as SimpleLambdaExpressionSyntax, cancellationToken) &&
                !CanSimplify(semanticDocument, lambda as ParenthesizedLambdaExpressionSyntax, cancellationToken))
            {
                return;
            }

            context.RegisterRefactoring(
                new MyCodeAction(
                    CSharpFeaturesResources.Simplify_lambda_expression,
                    c => SimplifyLambdaAsync(document, lambda, c)));

            context.RegisterRefactoring(
                new MyCodeAction(
                    CSharpFeaturesResources.Simplify_all_occurrences,
                    c => SimplifyAllLambdasAsync(document, c)));
        }

        private async Task<Document> SimplifyLambdaAsync(
            Document document,
            SyntaxNode lambda,
            CancellationToken cancellationToken)
        {
            var semanticDocument = await SemanticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var rewriter = new Rewriter(this, semanticDocument, n => n == lambda, cancellationToken);
            var result = rewriter.Visit(semanticDocument.Root);
            return document.WithSyntaxRoot(result);
        }

        private async Task<Document> SimplifyAllLambdasAsync(
            Document document,
            CancellationToken cancellationToken)
        {
            var semanticDocument = await SemanticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var rewriter = new Rewriter(this, semanticDocument, n => true, cancellationToken);
            var result = rewriter.Visit(semanticDocument.Root);
            return document.WithSyntaxRoot(result);
        }

        private static bool CanSimplify(
            SemanticDocument document,
            SimpleLambdaExpressionSyntax node,
            CancellationToken cancellationToken)
        {
            if (node == null)
            {
                return false;
            }

            var paramName = node.Parameter.Identifier;
            var invocation = TryGetInvocationExpression(node.Body);
            return CanSimplify(document, node, new List<SyntaxToken>() { paramName }, invocation, cancellationToken);
        }

        private static bool CanSimplify(
            SemanticDocument document,
            ParenthesizedLambdaExpressionSyntax node,
            CancellationToken cancellationToken)
        {
            if (node == null)
            {
                return false;
            }

            var paramNames = node.ParameterList.Parameters.Select(p => p.Identifier).ToList();
            var invocation = TryGetInvocationExpression(node.Body);
            return CanSimplify(document, node, paramNames, invocation, cancellationToken);
        }

        private static bool CanSimplify(
           SemanticDocument document,
            ExpressionSyntax lambda,
            List<SyntaxToken> paramNames,
            InvocationExpressionSyntax invocation,
            CancellationToken cancellationToken)
        {
            if (invocation == null)
            {
                return false;
            }

            if (invocation.ArgumentList.Arguments.Count != paramNames.Count)
            {
                return false;
            }

            for (var i = 0; i < paramNames.Count; i++)
            {
                var argument = invocation.ArgumentList.Arguments[i];
                if (argument.NameColon != null ||
                    argument.RefOrOutKeyword.Kind() != SyntaxKind.None ||
                    !argument.Expression.IsKind(SyntaxKind.IdentifierName))
                {
                    return false;
                }

                var identifierName = (IdentifierNameSyntax)argument.Expression;
                if (identifierName.Identifier.ValueText != paramNames[i].ValueText)
                {
                    return false;
                }
            }

            var semanticModel = document.SemanticModel;
            var lambdaSemanticInfo = semanticModel.GetSymbolInfo(lambda, cancellationToken);
            var invocationSemanticInfo = semanticModel.GetSymbolInfo(invocation, cancellationToken);
            if (lambdaSemanticInfo.Symbol == null ||
                invocationSemanticInfo.Symbol == null)
            {
                // Don't offer this if there are any errors or ambiguities.
                return false;
            }
            if (!(lambdaSemanticInfo.Symbol is IMethodSymbol lambdaMethod) || !(invocationSemanticInfo.Symbol is IMethodSymbol invocationMethod))
            {
                return false;
            }

            // TODO(cyrusn): Handle extension methods as well.
            if (invocationMethod.IsExtensionMethod)
            {
                return false;
            }

            // Check if any of the parameter is of Type Dynamic
            foreach (var parameter in lambdaMethod.Parameters)
            {
                if (parameter.Type != null && parameter.Type.Kind == SymbolKind.DynamicType)
                {
                    return false;
                }
            }

            // Check if the parameter and return types match between the lambda and the
            // invocation.  Note: return types can be covariant and argument types can be
            // contravariant.
            if (lambdaMethod.ReturnsVoid != invocationMethod.ReturnsVoid ||
                lambdaMethod.Parameters.Length != invocationMethod.Parameters.Length)
            {
                return false;
            }

            if (!lambdaMethod.ReturnsVoid)
            {
                // Return type has to be covariant.
                var conversion = document.SemanticModel.Compilation.ClassifyConversion(
                    invocationMethod.ReturnType, lambdaMethod.ReturnType);
                if (!conversion.IsIdentityOrImplicitReference())
                {
                    return false;
                }
            }

            // Parameter types have to be contravariant.
            for (var i = 0; i < lambdaMethod.Parameters.Length; i++)
            {
                var conversion = document.SemanticModel.Compilation.ClassifyConversion(
                    lambdaMethod.Parameters[i].Type, invocationMethod.Parameters[i].Type);

                if (!conversion.IsIdentityOrImplicitReference())
                {
                    return false;
                }
            }

            if (WouldCauseAmbiguity(lambda, invocation, semanticModel, cancellationToken))
            {
                return false;
            }

            // Looks like something we can simplify.
            return true;
        }

        // Ensure that if we replace the invocation with its expression that its expression will
        // bind unambiguously.  This can happen with awesome cases like:
#if false
    static void Goo<T>(T x) where T : class { }
    static void Bar(Action<int> x) { }
    static void Bar(Action<string> x) { }
    static void Main()
    {
        Bar(x => Goo(x)); // error CS0121: The call is ambiguous between the following methods or properties: 'A.Bar(System.Action<int>)' and 'A.Bar(System.Action<string>)'
    }
#endif
        private static bool WouldCauseAmbiguity(
            ExpressionSyntax lambda,
            InvocationExpressionSyntax invocation,
            SemanticModel oldSemanticModel,
            CancellationToken cancellationToken)
        {
            var annotation = new SyntaxAnnotation();

            // In order to check if there will be a problem, we actually make the change, fork the
            // compilation, and then verify that the new expression bound unambiguously.  
            var oldExpression = invocation.Expression.WithAdditionalAnnotations(annotation);
            var oldCompilation = oldSemanticModel.Compilation;
            var oldTree = oldSemanticModel.SyntaxTree;
            var oldRoot = oldTree.GetRoot(cancellationToken);

            var newRoot = oldRoot.ReplaceNode(lambda, oldExpression);

            var newTree = oldTree.WithRootAndOptions(newRoot, oldTree.Options);

            var newCompilation = oldCompilation.ReplaceSyntaxTree(oldTree, newTree);
            var newExpression = newTree.GetRoot(cancellationToken).GetAnnotatedNodesAndTokens(annotation).First().AsNode();
            var newSemanticModel = newCompilation.GetSemanticModel(newTree);

            var info = newSemanticModel.GetSymbolInfo(newExpression, cancellationToken);

            return info.CandidateReason != CandidateReason.None;
        }

        private static InvocationExpressionSyntax TryGetInvocationExpression(
            SyntaxNode lambdaBody)
        {
            if (lambdaBody is ExpressionSyntax exprBody)
            {
                return exprBody.WalkDownParentheses() as InvocationExpressionSyntax;
            }
            else if (lambdaBody is BlockSyntax block)
            {
                if (block.Statements.Count == 1)
                {
                    var statement = block.Statements.First();
                    if (statement is ReturnStatementSyntax returnStatement)
                    {
                        return returnStatement.Expression.WalkDownParentheses() as InvocationExpressionSyntax;
                    }
                    else if (statement is ExpressionStatementSyntax exprStatement)
                    {
                        return exprStatement.Expression.WalkDownParentheses() as InvocationExpressionSyntax;
                    }
                }
            }

            return null;
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}
