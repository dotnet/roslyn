// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    [Flags]
    internal enum OperatorPosition
    {
        None = 0,
        Prefix = 1,
        Infix = 2,
        Postfix = 4,
    }

    internal static class OperatorSymbolExtensions
    {
        internal static string GetOperatorSignOfOperator(this IMethodSymbol m)
        {
            return m.Name switch
            {
                // binary
                WellKnownMemberNames.AdditionOperatorName => "+",
                WellKnownMemberNames.BitwiseAndOperatorName => "&",
                WellKnownMemberNames.BitwiseOrOperatorName => "|",
                WellKnownMemberNames.DivisionOperatorName => "/",
                WellKnownMemberNames.EqualityOperatorName => "==",
                WellKnownMemberNames.ExclusiveOrOperatorName => "^",
                WellKnownMemberNames.GreaterThanOperatorName => ">",
                WellKnownMemberNames.GreaterThanOrEqualOperatorName => ">=",
                WellKnownMemberNames.InequalityOperatorName => "!=",
                WellKnownMemberNames.LeftShiftOperatorName => "<<",
                WellKnownMemberNames.LessThanOperatorName => "<",
                WellKnownMemberNames.LessThanOrEqualOperatorName => "<=",
                WellKnownMemberNames.ModulusOperatorName => "%",
                WellKnownMemberNames.MultiplyOperatorName => "*",
                WellKnownMemberNames.RightShiftOperatorName => ">>",
                WellKnownMemberNames.SubtractionOperatorName => "-",

                // Unary
                WellKnownMemberNames.DecrementOperatorName => "--",
                WellKnownMemberNames.FalseOperatorName => "false",
                WellKnownMemberNames.IncrementOperatorName => "++",
                WellKnownMemberNames.LogicalNotOperatorName => "!",
                WellKnownMemberNames.OnesComplementOperatorName => "~",
                WellKnownMemberNames.TrueOperatorName => "true",
                WellKnownMemberNames.UnaryNegationOperatorName => "-",
                WellKnownMemberNames.UnaryPlusOperatorName => "+",

                _ => throw ExceptionUtilities.UnexpectedValue(m.Name),
            };
        }

        public static string GetOperatorName(this IMethodSymbol m)
        {
            return m.Name switch
            {
                // binary
                WellKnownMemberNames.AdditionOperatorName => "a + b",
                WellKnownMemberNames.BitwiseAndOperatorName => "a & b",
                WellKnownMemberNames.BitwiseOrOperatorName => "a | b",
                WellKnownMemberNames.DivisionOperatorName => "a / b",
                WellKnownMemberNames.EqualityOperatorName => "a == b",
                WellKnownMemberNames.ExclusiveOrOperatorName => "a ^ b",
                WellKnownMemberNames.GreaterThanOperatorName => "a > b",
                WellKnownMemberNames.GreaterThanOrEqualOperatorName => "a >= b",
                WellKnownMemberNames.InequalityOperatorName => "a != b",
                WellKnownMemberNames.LeftShiftOperatorName => "a << b",
                WellKnownMemberNames.LessThanOperatorName => "a < b",
                WellKnownMemberNames.LessThanOrEqualOperatorName => "a <= b",
                WellKnownMemberNames.ModulusOperatorName => "a % b",
                WellKnownMemberNames.MultiplyOperatorName => "a * b",
                WellKnownMemberNames.RightShiftOperatorName => "a >> b",
                WellKnownMemberNames.SubtractionOperatorName => "a - b",

                // Unary
                WellKnownMemberNames.DecrementOperatorName => "a--",
                WellKnownMemberNames.FalseOperatorName => "false",
                WellKnownMemberNames.IncrementOperatorName => "a++",
                WellKnownMemberNames.LogicalNotOperatorName => "!a",
                WellKnownMemberNames.OnesComplementOperatorName => "~a",
                WellKnownMemberNames.TrueOperatorName => "true",
                WellKnownMemberNames.UnaryNegationOperatorName => "-a",
                WellKnownMemberNames.UnaryPlusOperatorName => "+a",

                _ => throw ExceptionUtilities.UnexpectedValue(m.Name),
            };
        }

        internal static OperatorPosition GetOperatorPosition(this IMethodSymbol m)
        {
            switch (m.Name)
            {
                // binary
                case WellKnownMemberNames.AdditionOperatorName:
                case WellKnownMemberNames.BitwiseAndOperatorName:
                case WellKnownMemberNames.BitwiseOrOperatorName:
                case WellKnownMemberNames.DivisionOperatorName:
                case WellKnownMemberNames.EqualityOperatorName:
                case WellKnownMemberNames.ExclusiveOrOperatorName:
                case WellKnownMemberNames.GreaterThanOperatorName:
                case WellKnownMemberNames.GreaterThanOrEqualOperatorName:
                case WellKnownMemberNames.InequalityOperatorName:
                case WellKnownMemberNames.LeftShiftOperatorName:
                case WellKnownMemberNames.LessThanOperatorName:
                case WellKnownMemberNames.LessThanOrEqualOperatorName:
                case WellKnownMemberNames.ModulusOperatorName:
                case WellKnownMemberNames.MultiplyOperatorName:
                case WellKnownMemberNames.RightShiftOperatorName:
                case WellKnownMemberNames.SubtractionOperatorName:
                    return OperatorPosition.Infix;
                // Unary
                case WellKnownMemberNames.DecrementOperatorName:
                case WellKnownMemberNames.IncrementOperatorName:
                    return OperatorPosition.Prefix | OperatorPosition.Postfix;
                case WellKnownMemberNames.FalseOperatorName:
                case WellKnownMemberNames.TrueOperatorName:
                    return OperatorPosition.None;
                case WellKnownMemberNames.LogicalNotOperatorName:
                case WellKnownMemberNames.OnesComplementOperatorName:
                case WellKnownMemberNames.UnaryNegationOperatorName:
                case WellKnownMemberNames.UnaryPlusOperatorName:
                    return OperatorPosition.Prefix;
                default:
                    throw ExceptionUtilities.UnexpectedValue(m.Name);
            }
        }
    }
}
