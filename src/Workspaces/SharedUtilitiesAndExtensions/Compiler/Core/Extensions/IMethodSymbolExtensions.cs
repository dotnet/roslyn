// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class IMethodSymbolExtensions
    {
        /// <summary>
        /// Returns true for void returning methods with two parameters, where
        /// the first parameter is of <see cref="object"/> type and the second
        /// parameter inherits from or equals <see cref="EventArgs"/> type.
        /// </summary>
        public static bool HasEventHandlerSignature(this IMethodSymbol method, [NotNullWhen(returnValue: true)] INamedTypeSymbol? eventArgsType)
            => eventArgsType != null &&
               method.Parameters.Length == 2 &&
               method.Parameters[0].Type.SpecialType == SpecialType.System_Object &&
               method.Parameters[1].Type.InheritsFromOrEquals(eventArgsType);

        public static bool TryGetPredefinedComparisonOperator(this IMethodSymbol symbol, out PredefinedOperator op)
        {
            if (symbol.MethodKind == MethodKind.BuiltinOperator)
            {
                op = symbol.GetPredefinedOperator();
                switch (op)
                {
                    case PredefinedOperator.Equality:
                    case PredefinedOperator.Inequality:
                    case PredefinedOperator.GreaterThanOrEqual:
                    case PredefinedOperator.LessThanOrEqual:
                    case PredefinedOperator.GreaterThan:
                    case PredefinedOperator.LessThan:
                        return true;
                }
            }
            else
            {
                op = PredefinedOperator.None;
            }

            return false;
        }

        public static PredefinedOperator GetPredefinedOperator(this IMethodSymbol symbol)
        {
            switch (symbol.Name)
            {
                case "op_Addition":
                case "op_UnaryPlus":
                    return PredefinedOperator.Addition;
                case "op_BitwiseAnd":
                    return PredefinedOperator.BitwiseAnd;
                case "op_BitwiseOr":
                    return PredefinedOperator.BitwiseOr;
                case "op_Concatenate":
                    return PredefinedOperator.Concatenate;
                case "op_Decrement":
                    return PredefinedOperator.Decrement;
                case "op_Division":
                    return PredefinedOperator.Division;
                case "op_Equality":
                    return PredefinedOperator.Equality;
                case "op_ExclusiveOr":
                    return PredefinedOperator.ExclusiveOr;
                case "op_Exponent":
                    return PredefinedOperator.Exponent;
                case "op_GreaterThan":
                    return PredefinedOperator.GreaterThan;
                case "op_GreaterThanOrEqual":
                    return PredefinedOperator.GreaterThanOrEqual;
                case "op_Increment":
                    return PredefinedOperator.Increment;
                case "op_Inequality":
                    return PredefinedOperator.Inequality;
                case "op_IntegerDivision":
                    return PredefinedOperator.IntegerDivision;
                case "op_LeftShift":
                    return PredefinedOperator.LeftShift;
                case "op_LessThan":
                    return PredefinedOperator.LessThan;
                case "op_LessThanOrEqual":
                    return PredefinedOperator.LessThanOrEqual;
                case "op_Like":
                    return PredefinedOperator.Like;
                case "op_LogicalNot":
                case "op_OnesComplement":
                    return PredefinedOperator.Complement;
                case "op_Modulus":
                    return PredefinedOperator.Modulus;
                case "op_Multiply":
                    return PredefinedOperator.Multiplication;
                case "op_RightShift":
                    return PredefinedOperator.RightShift;
                case "op_Subtraction":
                case "op_UnaryNegation":
                    return PredefinedOperator.Subtraction;
                default:
                    return PredefinedOperator.None;
            }
        }
    }
}
