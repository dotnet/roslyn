// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.Async;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Resources = Microsoft.CodeAnalysis.CSharp.CSharpFeaturesResources;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.Async
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddAsync), Shared]
    internal class CSharpAddAsyncCodeFixProvider : AbstractAddAsyncCodeFixProvider
    {
        /// <summary>
        /// The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task'.
        /// </summary>
        private const string CS4032 = "CS4032";

        /// <summary>
        /// The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task'.
        /// </summary>
        private const string CS4033 = "CS4033";

        /// <summary>
        /// The 'await' operator can only be used within an async lambda expression. Consider marking this method with the 'async' modifier.
        /// </summary>
        private const string CS4034 = "CS4034";

        public override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(CS4032, CS4033, CS4034); }
        }

        protected override string GetDescription(Diagnostic diagnostic, SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return Resources.MakeAsync;
        }

        protected override async Task<SyntaxNode> GetNewRoot(SyntaxNode root, SyntaxNode oldNode, SemanticModel semanticModel, Diagnostic diagnostic, Document document, CancellationToken cancellationToken)
        {
            var methodNode = GetContainingMember(oldNode);
            if (methodNode == null)
            {
                return null;
            }

            var newMethodNode = await ConvertToAsync(methodNode, semanticModel, document, cancellationToken).ConfigureAwait(false);
            if (newMethodNode != null)
            {
                return root.ReplaceNode(methodNode, newMethodNode);
            }

            return null;
        }

        private static SyntaxNode GetContainingMember(SyntaxNode oldNode)
        {
            var parenthesizedLambda = oldNode
                .Ancestors()
                .FirstOrDefault(n =>
                    n.IsKind(SyntaxKind.ParenthesizedLambdaExpression));

            if (parenthesizedLambda != null)
            {
                return parenthesizedLambda;
            }

            var simpleLambda = oldNode
                .Ancestors()
                .FirstOrDefault(n =>
                    n.IsKind(SyntaxKind.SimpleLambdaExpression));

            if (simpleLambda != null)
            {
                return simpleLambda;
            }

            return oldNode
                .Ancestors()
                .FirstOrDefault(n =>
                    n.IsKind(SyntaxKind.MethodDeclaration));
        }

        private async Task<SyntaxNode> ConvertToAsync(SyntaxNode node, SemanticModel semanticModel, Document document, CancellationToken cancellationToken)
        {
            var methodNode = node as MethodDeclarationSyntax;
            if (methodNode != null)
            {
                return await ConvertMethodToAsync(document, semanticModel, methodNode, cancellationToken).ConfigureAwait(false);
            }

            var parenthesizedLambda = node as ParenthesizedLambdaExpressionSyntax;
            if (parenthesizedLambda != null)
            {
                return ConvertParenthesizedLambdaToAsync(parenthesizedLambda);
            }

            var simpleLambda = node as SimpleLambdaExpressionSyntax;
            if (simpleLambda != null)
            {
                return ConvertSimpleLambdaToAsync(simpleLambda);
            }

            return null;
        }

        private static SyntaxNode ConvertParenthesizedLambdaToAsync(ParenthesizedLambdaExpressionSyntax parenthesizedLambda)
        {
            return SyntaxFactory.ParenthesizedLambdaExpression(
                                SyntaxFactory.Token(SyntaxKind.AsyncKeyword),
                                parenthesizedLambda.ParameterList,
                                parenthesizedLambda.ArrowToken,
                                parenthesizedLambda.RefKeyword,
                                parenthesizedLambda.Body)
                                .WithAdditionalAnnotations(Formatter.Annotation);
        }

        private static SyntaxNode ConvertSimpleLambdaToAsync(SimpleLambdaExpressionSyntax simpleLambda)
        {
            return SyntaxFactory.SimpleLambdaExpression(
                                SyntaxFactory.Token(SyntaxKind.AsyncKeyword),
                                simpleLambda.Parameter,
                                simpleLambda.ArrowToken,
                                simpleLambda.RefKeyword,
                                simpleLambda.Body)
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

        protected override SyntaxNode AddAsyncKeywordAndTaskReturnType(SyntaxNode node, ITypeSymbol existingReturnType, INamedTypeSymbol taskTypeSymbol)
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

            var returnType = taskTypeSymbol.Construct(existingReturnType).GenerateTypeSyntax();
            return AddAsyncKeyword(methodNode.WithReturnType(returnType));
        }

        protected override bool DoesConversionExist(Compilation compilation, ITypeSymbol source, ITypeSymbol destination)
        {
            return compilation.ClassifyConversion(source, destination).Exists;
        }
    }
}
