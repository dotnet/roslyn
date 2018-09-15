// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseCompoundAssignment
{
    internal static class Maps
    {
        public static readonly ImmutableDictionary<SyntaxKind, SyntaxKind> BinaryToAssignmentMap =
            new Dictionary<SyntaxKind, SyntaxKind>
            {
                { SyntaxKind.AddExpression, SyntaxKind.AddAssignmentExpression },
                { SyntaxKind.SubtractExpression, SyntaxKind.SubtractAssignmentExpression },
                { SyntaxKind.MultiplyExpression, SyntaxKind.MultiplyAssignmentExpression },
                { SyntaxKind.DivideExpression, SyntaxKind.DivideAssignmentExpression },
                { SyntaxKind.ModuloExpression, SyntaxKind.ModuloAssignmentExpression },
                { SyntaxKind.BitwiseAndExpression, SyntaxKind.AndAssignmentExpression },
                { SyntaxKind.ExclusiveOrExpression, SyntaxKind.ExclusiveOrAssignmentExpression },
                { SyntaxKind.BitwiseOrExpression, SyntaxKind.OrAssignmentExpression },
                { SyntaxKind.LeftShiftExpression, SyntaxKind.LeftShiftAssignmentExpression },
                { SyntaxKind.RightShiftExpression, SyntaxKind.RightShiftAssignmentExpression },
            }.ToImmutableDictionary();

        public static readonly ImmutableDictionary<SyntaxKind, SyntaxKind> AssignmentToTokenMap =
            BinaryToAssignmentMap.Values.ToImmutableDictionary(v => v, FindOperatorToken);

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
