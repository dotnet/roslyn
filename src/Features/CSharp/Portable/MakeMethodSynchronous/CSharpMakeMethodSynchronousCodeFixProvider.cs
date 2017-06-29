// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MakeMethodSynchronous;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.MakeMethodSynchronous
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.MakeMethodSynchronous), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.AddImport)]
    internal class CSharpMakeMethodSynchronousCodeFixProvider : AbstractMakeMethodSynchronousCodeFixProvider
    {
        private const string CS1998 = nameof(CS1998); // This async method lacks 'await' operators and will run synchronously.

        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(CS1998);

        protected override bool IsAsyncSupportingFunctionSyntax(SyntaxNode node)
            => node.IsAsyncSupportingFunctionSyntax();

        protected override SyntaxNode RemoveAsyncTokenAndFixReturnType(IMethodSymbol methodSymbolOpt, SyntaxNode node, ITypeSymbol taskType, ITypeSymbol taskOfTType)
        {
            switch (node)
            {
                case MethodDeclarationSyntax method: return FixMethod(methodSymbolOpt, method, taskType, taskOfTType);
                case LocalFunctionStatementSyntax localFunction: return FixLocalFunction(methodSymbolOpt, localFunction, taskType, taskOfTType);
                case AnonymousMethodExpressionSyntax method: return FixAnonymousMethod(method);
                case ParenthesizedLambdaExpressionSyntax lambda: return FixParenthesizedLambda(lambda);
                case SimpleLambdaExpressionSyntax lambda: return FixSimpleLambda(lambda);
                default: return node;
            }
        }

        private SyntaxNode FixMethod(IMethodSymbol methodSymbol, MethodDeclarationSyntax method, ITypeSymbol taskType, ITypeSymbol taskOfTType)
        {
            var newReturnType = FixMethodReturnType(methodSymbol, method.ReturnType, taskType, taskOfTType);
            var newModifiers = FixMethodModifiers(method.Modifiers, ref newReturnType);
            return method.WithReturnType(newReturnType).WithModifiers(newModifiers);
        }

        private SyntaxNode FixLocalFunction(IMethodSymbol methodSymbol, LocalFunctionStatementSyntax localFunction, ITypeSymbol taskType, ITypeSymbol taskOfTType)
        {
            var newReturnType = FixMethodReturnType(methodSymbol, localFunction.ReturnType, taskType, taskOfTType);
            var newModifiers = FixMethodModifiers(localFunction.Modifiers, ref newReturnType);
            return localFunction.WithReturnType(newReturnType).WithModifiers(newModifiers);
        }

        private static TypeSyntax FixMethodReturnType(IMethodSymbol methodSymbol, TypeSyntax returnType, ITypeSymbol taskType, ITypeSymbol taskOfTType)
        {
            var newReturnType = returnType;

            // If the return type is Task<T>, then make the new return type "T".
            // If it is Task, then make the new return type "void".
            if (methodSymbol.ReturnType.OriginalDefinition.Equals(taskType))
            {
                newReturnType = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)).WithTriviaFrom(returnType);
            }
            else if (methodSymbol.ReturnType.OriginalDefinition.Equals(taskOfTType))
            {
                newReturnType = methodSymbol.ReturnType.GetTypeArguments()[0].GenerateTypeSyntax().WithTriviaFrom(returnType);
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
            return lambda.WithAsyncKeyword(default(SyntaxToken)).WithPrependedLeadingTrivia(lambda.AsyncKeyword.LeadingTrivia);
        }

        private SyntaxNode FixSimpleLambda(SimpleLambdaExpressionSyntax lambda)
        {
            return lambda.WithAsyncKeyword(default(SyntaxToken)).WithPrependedLeadingTrivia(lambda.AsyncKeyword.LeadingTrivia);
        }

        private SyntaxNode FixAnonymousMethod(AnonymousMethodExpressionSyntax method)
        {
            return method.WithAsyncKeyword(default(SyntaxToken)).WithPrependedLeadingTrivia(method.AsyncKeyword.LeadingTrivia);
        }
    }
}
