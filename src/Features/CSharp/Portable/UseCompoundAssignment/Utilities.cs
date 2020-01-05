// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseCompoundAssignment
{
    internal static class Utilities
    {
        public static readonly ImmutableArray<(SyntaxKind, SyntaxKind, SyntaxKind)> Kinds =
            ImmutableArray.Create(
                (SyntaxKind.AddExpression, SyntaxKind.AddAssignmentExpression),
                (SyntaxKind.SubtractExpression, SyntaxKind.SubtractAssignmentExpression),
                (SyntaxKind.MultiplyExpression, SyntaxKind.MultiplyAssignmentExpression),
                (SyntaxKind.DivideExpression, SyntaxKind.DivideAssignmentExpression),
                (SyntaxKind.ModuloExpression, SyntaxKind.ModuloAssignmentExpression),
                (SyntaxKind.BitwiseAndExpression, SyntaxKind.AndAssignmentExpression),
                (SyntaxKind.ExclusiveOrExpression, SyntaxKind.ExclusiveOrAssignmentExpression),
                (SyntaxKind.BitwiseOrExpression, SyntaxKind.OrAssignmentExpression),
                (SyntaxKind.LeftShiftExpression, SyntaxKind.LeftShiftAssignmentExpression),
                (SyntaxKind.RightShiftExpression, SyntaxKind.RightShiftAssignmentExpression),
                (SyntaxKind.CoalesceExpression, SyntaxKind.CoalesceAssignmentExpression)).SelectAsArray(
                    tuple => (tuple.Item1, tuple.Item2, FindOperatorToken(tuple.Item2)));

        private static SyntaxKind FindOperatorToken(SyntaxKind assignmentExpressionKind)
        {
            for (var current = SyntaxKind.None; current <= SyntaxKind.ThrowExpression; current++)
            {
                if (SyntaxFacts.GetAssignmentExpression(current) == assignmentExpressionKind)
                {
                    return current;
                }
            }

            throw ExceptionUtilities.UnexpectedValue(assignmentExpressionKind);
        }
    }
}
