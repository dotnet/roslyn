// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.MakeMethodAsynchronous;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.MakeMethodAsynchronous
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class CSharpMakeMethodAsynchronousCodeFixProvider : AbstractMakeMethodAsynchronousCodeFixProvider
    {
        private const string CS4032 = nameof(CS4032); // The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task'.
        private const string CS4033 = nameof(CS4033); // The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task'.
        private const string CS4034 = nameof(CS4034); // The 'await' operator can only be used within an async lambda expression. Consider marking this method with the 'async' modifier.

        private static readonly SyntaxToken s_asyncToken = SyntaxFactory.Token(SyntaxKind.AsyncKeyword);

        [ImportingConstructor]
        public CSharpMakeMethodAsynchronousCodeFixProvider()
        {
        }

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

        protected override bool IsAsyncReturnType(ITypeSymbol type, KnownTypes knownTypes)
        {
            return IsIAsyncEnumerableOrEnumerator(type, knownTypes)
                || IsTaskLike(type, knownTypes);
        }

        protected override SyntaxNode AddAsyncTokenAndFixReturnType(
            bool keepVoid, IMethodSymbol methodSymbolOpt, SyntaxNode node,
            KnownTypes knownTypes)
        {
            switch (node)
            {
                case MethodDeclarationSyntax method: return FixMethod(keepVoid, methodSymbolOpt, method, knownTypes);
                case LocalFunctionStatementSyntax localFunction: return FixLocalFunction(keepVoid, methodSymbolOpt, localFunction, knownTypes);
                case AnonymousMethodExpressionSyntax method: return FixAnonymousMethod(method);
                case ParenthesizedLambdaExpressionSyntax lambda: return FixParenthesizedLambda(lambda);
                case SimpleLambdaExpressionSyntax lambda: return FixSimpleLambda(lambda);
                default: return node;
            }
        }

        private SyntaxNode FixMethod(
            bool keepVoid, IMethodSymbol methodSymbol, MethodDeclarationSyntax method,
            KnownTypes knownTypes)
        {
            var newReturnType = FixMethodReturnType(keepVoid, methodSymbol, method.ReturnType, knownTypes);
            var newModifiers = AddAsyncModifierWithCorrectedTrivia(method.Modifiers, ref newReturnType);
            return method.WithReturnType(newReturnType).WithModifiers(newModifiers);
        }

        private SyntaxNode FixLocalFunction(
            bool keepVoid, IMethodSymbol methodSymbol, LocalFunctionStatementSyntax localFunction,
            KnownTypes knownTypes)
        {
            var newReturnType = FixMethodReturnType(keepVoid, methodSymbol, localFunction.ReturnType, knownTypes);
            var newModifiers = AddAsyncModifierWithCorrectedTrivia(localFunction.Modifiers, ref newReturnType);
            return localFunction.WithReturnType(newReturnType).WithModifiers(newModifiers);
        }

        private static TypeSyntax FixMethodReturnType(
            bool keepVoid, IMethodSymbol methodSymbol, TypeSyntax returnTypeSyntax,
            KnownTypes knownTypes)
        {
            var newReturnType = returnTypeSyntax.WithAdditionalAnnotations(Formatter.Annotation);

            if (methodSymbol.ReturnsVoid)
            {
                if (!keepVoid)
                {
                    newReturnType = knownTypes._taskType.GenerateTypeSyntax();
                }
            }
            else
            {
                var returnType = methodSymbol.ReturnType;
                if (IsIEnumerable(returnType, knownTypes) && IsIterator(methodSymbol))
                {
                    newReturnType = knownTypes._iAsyncEnumerableOfTTypeOpt is null
                        ? MakeGenericType("IAsyncEnumerable", methodSymbol.ReturnType)
                        : knownTypes._iAsyncEnumerableOfTTypeOpt.Construct(methodSymbol.ReturnType.GetTypeArguments()[0]).GenerateTypeSyntax();
                }
                else if (IsIEnumerator(returnType, knownTypes) && IsIterator(methodSymbol))
                {
                    newReturnType = knownTypes._iAsyncEnumeratorOfTTypeOpt is null
                        ? MakeGenericType("IAsyncEnumerator", methodSymbol.ReturnType)
                        : knownTypes._iAsyncEnumeratorOfTTypeOpt.Construct(methodSymbol.ReturnType.GetTypeArguments()[0]).GenerateTypeSyntax();
                }
                else if (IsIAsyncEnumerableOrEnumerator(returnType, knownTypes))
                {
                    // Leave the return type alone
                }
                else if (!IsTaskLike(returnType, knownTypes))
                {
                    // If it's not already Task-like, then wrap the existing return type
                    // in Task<>.
                    newReturnType = knownTypes._taskOfTType.Construct(methodSymbol.ReturnType).GenerateTypeSyntax();
                }
            }

            return newReturnType.WithTriviaFrom(returnTypeSyntax);

            static TypeSyntax MakeGenericType(string type, ITypeSymbol typeArgumentFrom)
            {
                var result = SyntaxFactory.GenericName(SyntaxFactory.Identifier(type),
                        SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList(typeArgumentFrom.GetTypeArguments()[0].GenerateTypeSyntax())));

                return result.WithAdditionalAnnotations(Simplifier.Annotation);
            }
        }

        private static bool IsIterator(IMethodSymbol x)
        {
            return x.Locations.Any(l => ContainsYield(l.FindNode(cancellationToken: default)));

            bool ContainsYield(SyntaxNode node)
                => node.DescendantNodes(n => n == node || !n.IsReturnableConstruct()).Any(n => IsYield(n));

            static bool IsYield(SyntaxNode node)
                => node.IsKind(SyntaxKind.YieldBreakStatement, SyntaxKind.YieldReturnStatement);
        }

        private static bool IsIAsyncEnumerableOrEnumerator(ITypeSymbol returnType, KnownTypes knownTypes)
            => returnType.OriginalDefinition.Equals(knownTypes._iAsyncEnumerableOfTTypeOpt) ||
                returnType.OriginalDefinition.Equals(knownTypes._iAsyncEnumeratorOfTTypeOpt);

        private static bool IsIEnumerable(ITypeSymbol returnType, KnownTypes knownTypes)
            => returnType.OriginalDefinition.Equals(knownTypes._iEnumerableOfTType);

        private static bool IsIEnumerator(ITypeSymbol returnType, KnownTypes knownTypes)
            => returnType.OriginalDefinition.Equals(knownTypes._iEnumeratorOfTType);

        private static SyntaxTokenList AddAsyncModifierWithCorrectedTrivia(SyntaxTokenList modifiers, ref TypeSyntax newReturnType)
        {
            if (modifiers.Any())
                return modifiers.Add(s_asyncToken);

            // Move the leading trivia from the return type to the new modifiers list.
            var result = SyntaxFactory.TokenList(s_asyncToken.WithLeadingTrivia(newReturnType.GetLeadingTrivia()));
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
