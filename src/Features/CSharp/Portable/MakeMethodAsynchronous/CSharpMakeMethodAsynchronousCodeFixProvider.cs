// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MakeMethodAsynchronous;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.MakeMethodAsynchronous
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class CSharpMakeMethodAsynchronousCodeFixProvider : AbstractMakeMethodAsynchronousCodeFixProvider
    {
        private const string CS4032 = nameof(CS4032); // The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task'.
        private const string CS4033 = nameof(CS4033); // The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task'.
        private const string CS4034 = nameof(CS4034); // The 'await' operator can only be used within an async lambda expression. Consider marking this method with the 'async' modifier.

        private static readonly SyntaxToken s_asyncToken = SyntaxFactory.Token(SyntaxKind.AsyncKeyword);

        public override ImmutableArray<string> FixableDiagnosticIds { get; } = 
            ImmutableArray.Create(CS4032, CS4033, CS4034);

        protected override string GetMakeAsyncTaskFunctionResource()
        {
            return CSharpFeaturesResources.Make_method_async;
        }

        protected override string GetMakeAsyncVoidFunctionResource()
        {
            return CSharpFeaturesResources.Make_method_async_remain_void;
        }

        protected override bool IsMethodOrAnonymousFunction(SyntaxNode node)
        {
            return node.IsKind(SyntaxKind.MethodDeclaration) || node.IsAnyLambdaOrAnonymousMethod();
        }

        protected override SyntaxNode AddAsyncTokenAndFixReturnType(
            bool keepVoid, IMethodSymbol methodSymbolOpt, SyntaxNode node,
            INamedTypeSymbol taskType, INamedTypeSymbol taskOfTType)
        {
            return node.TypeSwitch(
                (MethodDeclarationSyntax method) => FixMethod(keepVoid, methodSymbolOpt, method, taskType, taskOfTType),
                (AnonymousMethodExpressionSyntax method) => FixAnonymousMethod(method),
                (ParenthesizedLambdaExpressionSyntax lambda) => FixParenthesizedLambda(lambda),
                (SimpleLambdaExpressionSyntax lambda) => FixSimpleLambda(lambda),
                _ => node);
        }

        private SyntaxNode FixMethod(
            bool keepVoid, IMethodSymbol methodSymbol, MethodDeclarationSyntax method,
            ITypeSymbol taskType, INamedTypeSymbol taskOfTType)
        {
            var newReturnType = method.ReturnType;

            if (methodSymbol.ReturnsVoid)
            {
                if (!keepVoid)
                {
                    newReturnType = taskType.GenerateTypeSyntax();
                }
            }
            else
            {
                if (!IsTaskLike(methodSymbol.ReturnType, taskType, taskOfTType))
                {
                    // If it's not already Task-like, then wrap the existing return type
                    // in Task<>.
                    newReturnType = taskOfTType.Construct(methodSymbol.ReturnType).GenerateTypeSyntax();
                }
            }

            var newModifiers = method.Modifiers.Add(s_asyncToken);
            return method.WithReturnType(newReturnType).WithModifiers(newModifiers);
        }

        private SyntaxNode FixParenthesizedLambda(ParenthesizedLambdaExpressionSyntax lambda)
        {
            return lambda.WithoutLeadingTrivia()
                         .WithAsyncKeyword(s_asyncToken.WithPrependedLeadingTrivia(lambda.GetLeadingTrivia()));
        }

        private SyntaxNode FixSimpleLambda(SimpleLambdaExpressionSyntax lambda)
        {
            return lambda.WithoutLeadingTrivia()
                         .WithAsyncKeyword(s_asyncToken.WithPrependedLeadingTrivia(lambda.GetLeadingTrivia()));
        }

        private SyntaxNode FixAnonymousMethod(AnonymousMethodExpressionSyntax method)
        {
            return method.WithoutLeadingTrivia()
                         .WithAsyncKeyword(s_asyncToken.WithPrependedLeadingTrivia(method.GetLeadingTrivia()));
        }
    }
}