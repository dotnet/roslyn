// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionInitializer;

using static SyntaxFactory;

internal static class CSharpCollectionInitializerRewriter
{
    public static ExpressionSyntax ConvertExpression(
        ExpressionSyntax expression,
        Func<ExpressionSyntax, ExpressionSyntax>? indent)
    {
        // This must be called from an expression from the original tree.  Not something we're already transforming.
        // Otherwise, we'll have no idea how to apply the preferredIndentation if present.
        Contract.ThrowIfNull(expression.Parent);
        return expression switch
        {
            InvocationExpressionSyntax invocation => ConvertInvocation(invocation, indent),
            AssignmentExpressionSyntax assignment => ConvertAssignment(assignment, indent),
            _ => throw new InvalidOperationException(),
        };
    }

    private static AssignmentExpressionSyntax ConvertAssignment(
        AssignmentExpressionSyntax assignment,
        Func<ExpressionSyntax, ExpressionSyntax>? indent)
    {
        // Assignment is only used for collection-initializers, which *currently* do not do any special
        // indentation handling on elements.
        Contract.ThrowIfTrue(indent != null);

        var elementAccess = (ElementAccessExpressionSyntax)assignment.Left;
        return assignment.WithLeft(
            ImplicitElementAccess(elementAccess.ArgumentList));
    }

    private static ExpressionSyntax ConvertInvocation(
        InvocationExpressionSyntax invocation,
        Func<ExpressionSyntax, ExpressionSyntax>? indent)
    {
        indent ??= static expr => expr;
        var arguments = invocation.ArgumentList.Arguments;

        if (arguments.Count == 1)
        {
            // Assignment expressions in a collection initializer will cause the compiler to 
            // report an error.  This is because { a = b } is the form for an object initializer,
            // and the two forms are not allowed to mix/match.  Parenthesize the assignment to
            // avoid the ambiguity.
            var expression = indent(arguments[0].Expression);
            return SyntaxFacts.IsAssignmentExpression(expression.Kind())
                ? ParenthesizedExpression(expression)
                : expression;
        }

        return InitializerExpression(
            SyntaxKind.ComplexElementInitializerExpression,
            Token(SyntaxKind.OpenBraceToken).WithoutTrivia(),
            SeparatedList(
                arguments.Select(a => a.Expression),
                arguments.GetSeparators()),
            Token(SyntaxKind.CloseBraceToken).WithoutTrivia());
    }
}
