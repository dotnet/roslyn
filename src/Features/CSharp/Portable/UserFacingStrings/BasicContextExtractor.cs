// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.UserFacingStrings;

/// <summary>
/// Single source of truth for extracting the BASIC context used for cache keys.
/// Do not modify this without also updating any cache key generation that depends on it.
/// </summary>
internal static class BasicContextExtractor
{
    /// <summary>
    /// Returns the basic context string for a string literal, used strictly for cache key hashing.
    /// </summary>
    public static string GetBasicContext(LiteralExpressionSyntax stringLiteral)
    {
        var parent = stringLiteral.Parent;

        return parent switch
        {
            ArgumentSyntax arg when arg.Parent?.Parent is InvocationExpressionSyntax invocation =>
                $"Argument to method: {invocation.Expression}",
            AssignmentExpressionSyntax assignment =>
                $"Assignment to: {assignment.Left}",
            VariableDeclaratorSyntax declarator =>
                $"Variable initialization: {declarator.Identifier}",
            ReturnStatementSyntax =>
                "Return statement",
            AttributeSyntax =>
                "Attribute value",
            ThrowStatementSyntax =>
                "Exception message",
            _ => "Other context"
        };
    }
}


