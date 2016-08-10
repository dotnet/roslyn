// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.Async;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.Async
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddAsync), Shared]
    internal class CSharpAddAsyncCodeFixProvider : AbstractAddAsyncCodeFixProvider
    {
        /// <summary>
        /// The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task'.
        /// </summary>
        private const string CS4032 = nameof(CS4032);

        /// <summary>
        /// The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task'.
        /// </summary>
        private const string CS4033 = nameof(CS4033);

        /// <summary>
        /// The 'await' operator can only be used within an async lambda expression. Consider marking this method with the 'async' modifier.
        /// </summary>
        private const string CS4034 = nameof(CS4034);

        public override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(CS4032, CS4033, CS4034); }
        }

        protected override async Task<IList<DescriptionAndNode>> GetDescriptionsAndNodesAsync(SyntaxNode root, SyntaxNode oldNode, SemanticModel semanticModel, Diagnostic diagnostic, Document document, CancellationToken cancellationToken)
        {
            var nodeToModify = GetContainingMember(oldNode);
            if (nodeToModify == null)
            {
                return null;
            }

            var nodesAndDescriptions = await ConvertToAsync(nodeToModify, semanticModel, document, cancellationToken).ConfigureAwait(false);
            if (nodesAndDescriptions == null)
            {
                return null;
            }

            var q = from n in nodesAndDescriptions
                    let newRoot = root.ReplaceNode(nodeToModify, n.Node)
                    select new DescriptionAndNode(n.Description, newRoot);

            return q.ToList();
        }

        private static SyntaxNode GetContainingMember(SyntaxNode oldNode)
        {
            foreach (var node in oldNode.Ancestors())
            {
                switch (node.Kind())
                {
                    case SyntaxKind.ParenthesizedLambdaExpression:
                    case SyntaxKind.SimpleLambdaExpression:
                    case SyntaxKind.AnonymousMethodExpression:
                        if ((node as AnonymousFunctionExpressionSyntax)?.AsyncKeyword.Kind() != SyntaxKind.AsyncKeyword)
                        {
                            return node;
                        }
                        break;
                    case SyntaxKind.MethodDeclaration:
                        if ((node as MethodDeclarationSyntax)?.Modifiers.Any(SyntaxKind.AsyncKeyword) == false)
                        {
                            return node;
                        }
                        break;
                    default:
                        continue;
                }
            }

            return null;
        }

        private Task<IList<DescriptionAndNode>> ConvertToAsync(
            SyntaxNode node, SemanticModel semanticModel, Document document, CancellationToken cancellationToken)
        {
            if (node is MethodDeclarationSyntax)
            {
                return ConvertMethodToAsync(document, semanticModel, node, cancellationToken);
            }

            var newNode = node.TypeSwitch(
                (ParenthesizedLambdaExpressionSyntax parenthesizedLambda) => ConvertParenthesizedLambdaToAsync(parenthesizedLambda),
                (SimpleLambdaExpressionSyntax simpleLambda) => ConvertSimpleLambdaToAsync(simpleLambda),
                (AnonymousMethodExpressionSyntax anonymousMethod) => ConvertAnonymousMethodToAsync(anonymousMethod));

            return Task.FromResult(SpecializedCollections.SingletonList(new DescriptionAndNode(
                FeaturesResources.Make_containing_scope_async,
                newNode)));
        }

        private static SyntaxNode ConvertParenthesizedLambdaToAsync(
            ParenthesizedLambdaExpressionSyntax parenthesizedLambda)
        {
            return SyntaxFactory.ParenthesizedLambdaExpression(
                SyntaxFactory.Token(SyntaxKind.AsyncKeyword),
                parenthesizedLambda.ParameterList,
                parenthesizedLambda.ArrowToken,
                parenthesizedLambda.Body)
                .WithTriviaFrom(parenthesizedLambda)
                .WithAdditionalAnnotations(Formatter.Annotation);
        }

        private static SyntaxNode ConvertSimpleLambdaToAsync(
            SimpleLambdaExpressionSyntax simpleLambda)
        {
            return SyntaxFactory.SimpleLambdaExpression(
                SyntaxFactory.Token(SyntaxKind.AsyncKeyword),
                simpleLambda.Parameter,
                simpleLambda.ArrowToken,
                simpleLambda.Body)
                .WithTriviaFrom(simpleLambda)
                .WithAdditionalAnnotations(Formatter.Annotation);
        }

        private static SyntaxNode ConvertAnonymousMethodToAsync(
            AnonymousMethodExpressionSyntax anonymousMethod)
        {
            return SyntaxFactory.AnonymousMethodExpression(
                SyntaxFactory.Token(SyntaxKind.AsyncKeyword),
                anonymousMethod.DelegateKeyword,
                anonymousMethod.ParameterList,
                anonymousMethod.Block)
                .WithTriviaFrom(anonymousMethod)
                .WithAdditionalAnnotations(Formatter.Annotation);
        }

        protected override SyntaxNode AddAsyncKeyword(SyntaxNode node)
        {
            var methodNode = node as MethodDeclarationSyntax;
            if (methodNode == null)
            {
                return null;
            }

            return methodNode
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.AsyncKeyword))
                .WithAdditionalAnnotations(Formatter.Annotation);
        }

        protected override SyntaxNode AddAsyncKeywordAndTaskReturnType(
            SyntaxNode node, ITypeSymbol existingReturnType, INamedTypeSymbol taskTypeSymbol)
        {
            var methodNode = node as MethodDeclarationSyntax;
            if (methodNode == null)
            {
                return null;
            }

            if (taskTypeSymbol == null)
            {
                return null;
            }

            var returnTypeSymbol = existingReturnType == null
                ? taskTypeSymbol
                : taskTypeSymbol.Construct(existingReturnType);

            var returnType = returnTypeSymbol.GenerateTypeSyntax();
            return AddAsyncKeyword(methodNode.WithReturnType(returnType));
        }

        protected override bool DoesConversionExist(Compilation compilation, ITypeSymbol source, ITypeSymbol destination)
        {
            return compilation.ClassifyConversion(source, destination).Exists;
        }
    }
}