// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
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
}
