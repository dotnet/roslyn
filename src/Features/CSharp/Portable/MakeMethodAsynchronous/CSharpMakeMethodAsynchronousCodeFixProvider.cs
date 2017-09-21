// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
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

        protected override bool IsAsyncSupportingFunctionSyntax(SyntaxNode node)
            => node.IsAsyncSupportingFunctionSyntax();

        protected override SyntaxNode AddAsyncTokenAndFixReturnType(
            bool keepVoid, IMethodSymbol methodSymbolOpt, SyntaxNode node,
            INamedTypeSymbol taskType, INamedTypeSymbol taskOfTType, INamedTypeSymbol valueTaskOfTType)
        {
            switch (node)
            {
                case MethodDeclarationSyntax method: return FixMethod(keepVoid, methodSymbolOpt, method, taskType, taskOfTType, valueTaskOfTType);
                case LocalFunctionStatementSyntax localFunction: return FixLocalFunction(keepVoid, methodSymbolOpt, localFunction, taskType, taskOfTType, valueTaskOfTType);
                case AnonymousMethodExpressionSyntax method: return FixAnonymousMethod(method);
                case ParenthesizedLambdaExpressionSyntax lambda: return FixParenthesizedLambda(lambda);
                case SimpleLambdaExpressionSyntax lambda: return FixSimpleLambda(lambda);
                default: return node;
            }
        }

        private SyntaxNode FixMethod(
            bool keepVoid, IMethodSymbol methodSymbol, MethodDeclarationSyntax method,
            INamedTypeSymbol taskType, INamedTypeSymbol taskOfTType, INamedTypeSymbol valueTaskOfTType)
        {
            var newReturnType = FixMethodReturnType(keepVoid, methodSymbol, method.ReturnType, taskType, taskOfTType, valueTaskOfTType);
            var newModifiers = AddAsyncModifierWithCorrectedTrivia(method.Modifiers, ref newReturnType);
            return method.WithReturnType(newReturnType).WithModifiers(newModifiers);
        }

        private SyntaxNode FixLocalFunction(
            bool keepVoid, IMethodSymbol methodSymbol, LocalFunctionStatementSyntax localFunction,
            INamedTypeSymbol taskType, INamedTypeSymbol taskOfTType, INamedTypeSymbol valueTaskOfTType)
        {
            var newReturnType = FixMethodReturnType(keepVoid, methodSymbol, localFunction.ReturnType, taskType, taskOfTType, valueTaskOfTType);
            var newModifiers = AddAsyncModifierWithCorrectedTrivia(localFunction.Modifiers, ref newReturnType);
            return localFunction.WithReturnType(newReturnType).WithModifiers(newModifiers);
        }

        private static TypeSyntax FixMethodReturnType(
            bool keepVoid, IMethodSymbol methodSymbol, TypeSyntax returnType,
            INamedTypeSymbol taskType, INamedTypeSymbol taskOfTType, INamedTypeSymbol valueTaskOfTType)
        {
            var newReturnType = returnType.WithAdditionalAnnotations(Formatter.Annotation);

            if (methodSymbol.ReturnsVoid)
            {
                if (!keepVoid)
                {
                    newReturnType = taskType.GenerateTypeSyntax();
                }
            }
            else
            {
                if (!IsTaskLike(methodSymbol.ReturnType, taskType, taskOfTType, valueTaskOfTType))
                {
                    // If it's not already Task-like, then wrap the existing return type
                    // in Task<>.
                    newReturnType = taskOfTType.Construct(methodSymbol.ReturnType).GenerateTypeSyntax();
                }
            }

            return newReturnType.WithTriviaFrom(returnType);
        }

        private static SyntaxTokenList AddAsyncModifierWithCorrectedTrivia(SyntaxTokenList modifiers, ref TypeSyntax newReturnType)
        {
            if (modifiers.Any())
                return modifiers.Add(s_asyncToken);

            // Move the leading trivia from the return type to the new modifiers list.
            SyntaxTokenList result = SyntaxFactory.TokenList(s_asyncToken.WithLeadingTrivia(newReturnType.GetLeadingTrivia()));
            newReturnType = newReturnType.WithoutLeadingTrivia();
            return result;
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
