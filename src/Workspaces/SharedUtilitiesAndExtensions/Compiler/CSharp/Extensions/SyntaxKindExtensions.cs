// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class SyntaxKindExtensions
    {
        /// <summary>
        /// Determine if the given <see cref="SyntaxKind"/> array contains the given kind.
        /// </summary>
        /// <param name="kinds">Array to search</param>
        /// <param name="kind">Sought value</param>
        /// <returns>True if <paramref name = "kinds"/> contains the value<paramref name= "kind"/>.</returns>
        /// <remarks>PERF: Not using Array.IndexOf here because it results in a call to IndexOf on the
        /// default EqualityComparer for SyntaxKind.The default comparer for SyntaxKind is the
        /// ObjectEqualityComparer which results in boxing allocations.</remarks>
        public static bool Contains(this SyntaxKind[] kinds, SyntaxKind kind)
        {
            foreach (var k in kinds)
            {
                if (k == kind)
                {
                    return true;
                }
            }

            return false;
        }

        public static SyntaxKind MapCompoundAssignmentKindToBinaryExpressionKind(this SyntaxKind syntaxKind)
        {
            switch (syntaxKind)
            {
                case SyntaxKind.AddAssignmentExpression:
                    return SyntaxKind.AddExpression;

                case SyntaxKind.SubtractAssignmentExpression:
                    return SyntaxKind.SubtractExpression;

                case SyntaxKind.MultiplyAssignmentExpression:
                    return SyntaxKind.MultiplyExpression;

                case SyntaxKind.DivideAssignmentExpression:
                    return SyntaxKind.DivideExpression;

                case SyntaxKind.ModuloAssignmentExpression:
                    return SyntaxKind.ModuloExpression;

                case SyntaxKind.AndAssignmentExpression:
                    return SyntaxKind.BitwiseAndExpression;

                case SyntaxKind.ExclusiveOrAssignmentExpression:
                    return SyntaxKind.ExclusiveOrExpression;

                case SyntaxKind.OrAssignmentExpression:
                    return SyntaxKind.BitwiseOrExpression;

                case SyntaxKind.LeftShiftAssignmentExpression:
                    return SyntaxKind.LeftShiftExpression;

                case SyntaxKind.RightShiftAssignmentExpression:
                    return SyntaxKind.RightShiftExpression;

                case SyntaxKind.CoalesceAssignmentExpression:
                    return SyntaxKind.CoalesceExpression;

                default:
                    Debug.Fail($"Unhandled compound assignment kind: {syntaxKind}");
                    return SyntaxKind.None;
            }
        }
    }
}
