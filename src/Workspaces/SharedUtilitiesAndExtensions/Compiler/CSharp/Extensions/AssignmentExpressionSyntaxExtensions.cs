// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Extensions;

internal static class AssignmentExpressionSyntaxExtensions
{
    internal static bool IsDeconstruction(this AssignmentExpressionSyntax assignment)
    {
        var left = assignment.Left;
        return assignment.Kind() == SyntaxKind.SimpleAssignmentExpression &&
               assignment.OperatorToken.Kind() == SyntaxKind.EqualsToken &&
               (left.Kind() == SyntaxKind.TupleExpression || left.Kind() == SyntaxKind.DeclarationExpression);
    }
}
