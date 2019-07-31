// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MakeMethodSynchronous;
using Microsoft.CodeAnalysis.Shared.Extensions;
using static Microsoft.CodeAnalysis.MakeMethodAsynchronous.AbstractMakeMethodAsynchronousCodeFixProvider;

namespace Microsoft.CodeAnalysis.CSharp.MakeMethodSynchronous
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.MakeMethodSynchronous), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.AddImport)]
    internal class CSharpMakeMethodSynchronousCodeFixProvider : AbstractMakeMethodSynchronousCodeFixProvider
    {
        private const string CS1998 = nameof(CS1998); // This async method lacks 'await' operators and will run synchronously.

        [ImportingConstructor]
        public CSharpMakeMethodSynchronousCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(CS1998);

        protected override bool IsAsyncSupportingFunctionSyntax(SyntaxNode node)
            => node.IsAsyncSupportingFunctionSyntax();

        protected override SyntaxNode RemoveAsyncTokenAndFixReturnType(IMethodSymbol methodSymbolOpt, SyntaxNode node, KnownTypes knownTypes)
        {
            switch (node)
            {
                case MethodDeclarationSyntax method: return FixMethod(methodSymbolOpt, method, knownTypes);
                case LocalFunctionStatementSyntax localFunction: return FixLocalFunction(methodSymbolOpt, localFunction, knownTypes);
                case AnonymousMethodExpressionSyntax method: return FixAnonymousMethod(method);
                case ParenthesizedLambdaExpressionSyntax lambda: return FixParenthesizedLambda(lambda);
                case SimpleLambdaExpressionSyntax lambda: return FixSimpleLambda(lambda);
                default: return node;
            }
        }

        private SyntaxNode FixMethod(IMethodSymbol methodSymbol, MethodDeclarationSyntax method, KnownTypes knownTypes)
        {
            var newReturnType = FixMethodReturnType(methodSymbol, method.ReturnType, knownTypes);
            var newModifiers = FixMethodModifiers(method.Modifiers, ref newReturnType);
            return method.WithReturnType(newReturnType).WithModifiers(newModifiers);
        }

        private SyntaxNode FixLocalFunction(IMethodSymbol methodSymbol, LocalFunctionStatementSyntax localFunction, KnownTypes knownTypes)
        {
            var newReturnType = FixMethodReturnType(methodSymbol, localFunction.ReturnType, knownTypes);
            var newModifiers = FixMethodModifiers(localFunction.Modifiers, ref newReturnType);
            return localFunction.WithReturnType(newReturnType).WithModifiers(newModifiers);
        }

        private static TypeSyntax FixMethodReturnType(IMethodSymbol methodSymbol, TypeSyntax returnTypeSyntax, KnownTypes knownTypes)
        {
            var newReturnType = returnTypeSyntax;

            var returnType = methodSymbol.ReturnType;
            if (returnType.OriginalDefinition.Equals(knownTypes._taskType))
            {
                // If the return type is Task, then make the new return type "void".
                newReturnType = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)).WithTriviaFrom(returnTypeSyntax);
            }
            else if (returnType.OriginalDefinition.Equals(knownTypes._taskOfTType))
            {
                // If the return type is Task<T>, then make the new return type "T".
                newReturnType = returnType.GetTypeArguments()[0].GenerateTypeSyntax().WithTriviaFrom(returnTypeSyntax);
            }
            else if (returnType.OriginalDefinition.Equals(knownTypes._iAsyncEnumerableOfTTypeOpt))
            {
                // If the return type is IAsyncEnumerable<T>, then make the new return type IEnumerable<T>.
                newReturnType = knownTypes._iEnumerableOfTType.ConstructWithNullability(methodSymbol.ReturnType.GetTypeArguments()[0]).GenerateTypeSyntax();
            }
            else if (returnType.OriginalDefinition.Equals(knownTypes._iAsyncEnumeratorOfTTypeOpt))
            {
                // If the return type is IAsyncEnumerator<T>, then make the new return type IEnumerator<T>.
                newReturnType = knownTypes._iEnumeratorOfTType.ConstructWithNullability(methodSymbol.ReturnType.GetTypeArguments()[0]).GenerateTypeSyntax();
            }

            return newReturnType;
        }

        private static SyntaxTokenList FixMethodModifiers(SyntaxTokenList modifiers, ref TypeSyntax newReturnType)
        {
            var asyncTokenIndex = modifiers.IndexOf(SyntaxKind.AsyncKeyword);
            SyntaxTokenList newModifiers;
            if (asyncTokenIndex == 0)
            {
                // Have to move the trivia on the async token appropriately.
                var asyncLeadingTrivia = modifiers[0].LeadingTrivia;

                if (modifiers.Count > 1)
                {
                    // Move the trivia to the next modifier;
                    newModifiers = modifiers.Replace(
                        modifiers[1],
                        modifiers[1].WithPrependedLeadingTrivia(asyncLeadingTrivia));
                    newModifiers = newModifiers.RemoveAt(0);
                }
                else
                {
                    // move it to the return type.
                    newModifiers = modifiers.RemoveAt(0);
                    newReturnType = newReturnType.WithPrependedLeadingTrivia(asyncLeadingTrivia);
                }
            }
            else
            {
                newModifiers = modifiers.RemoveAt(asyncTokenIndex);
            }

            return newModifiers;
        }

        private SyntaxNode FixParenthesizedLambda(ParenthesizedLambdaExpressionSyntax lambda)
        {
            return lambda.WithAsyncKeyword(default).WithPrependedLeadingTrivia(lambda.AsyncKeyword.LeadingTrivia);
        }

        private SyntaxNode FixSimpleLambda(SimpleLambdaExpressionSyntax lambda)
        {
            return lambda.WithAsyncKeyword(default).WithPrependedLeadingTrivia(lambda.AsyncKeyword.LeadingTrivia);
        }

        private SyntaxNode FixAnonymousMethod(AnonymousMethodExpressionSyntax method)
        {
            return method.WithAsyncKeyword(default).WithPrependedLeadingTrivia(method.AsyncKeyword.LeadingTrivia);
        }
    }
}
