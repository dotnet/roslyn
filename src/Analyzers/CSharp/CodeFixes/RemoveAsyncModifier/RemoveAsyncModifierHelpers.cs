// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.RemoveAsyncModifier;

internal static class RemoveAsyncModifierHelpers
{
    internal static SyntaxNode WithoutAsyncModifier(MethodDeclarationSyntax method, TypeSyntax returnType)
    {
        var newModifiers = RemoveAsyncModifier(method.Modifiers, ref returnType);
        return method.WithReturnType(returnType).WithModifiers(newModifiers);
    }

    internal static SyntaxNode WithoutAsyncModifier(LocalFunctionStatementSyntax localFunction, TypeSyntax returnType)
    {
        var newModifiers = RemoveAsyncModifier(localFunction.Modifiers, ref returnType);
        return localFunction.WithReturnType(returnType).WithModifiers(newModifiers);
    }

    internal static SyntaxNode WithoutAsyncModifier(ParenthesizedLambdaExpressionSyntax lambda)
        => lambda.WithAsyncKeyword(default).WithPrependedLeadingTrivia(lambda.AsyncKeyword.LeadingTrivia);

    internal static SyntaxNode WithoutAsyncModifier(SimpleLambdaExpressionSyntax lambda)
        => lambda.WithAsyncKeyword(default).WithPrependedLeadingTrivia(lambda.AsyncKeyword.LeadingTrivia);

    internal static SyntaxNode WithoutAsyncModifier(AnonymousMethodExpressionSyntax method)
        => method.WithAsyncKeyword(default).WithPrependedLeadingTrivia(method.AsyncKeyword.LeadingTrivia);

    private static SyntaxTokenList RemoveAsyncModifier(SyntaxTokenList modifiers, ref TypeSyntax newReturnType)
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
                newModifiers = default;
                newReturnType = newReturnType.WithPrependedLeadingTrivia(asyncLeadingTrivia);
            }
        }
        else
        {
            newModifiers = modifiers.RemoveAt(asyncTokenIndex);
        }

        return newModifiers;
    }
}
