// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            => symbol.Name switch
            {
                "op_Addition" or "op_UnaryPlus" => PredefinedOperator.Addition,
                "op_BitwiseAnd" => PredefinedOperator.BitwiseAnd,
                "op_BitwiseOr" => PredefinedOperator.BitwiseOr,
                "op_Concatenate" => PredefinedOperator.Concatenate,
                "op_Decrement" => PredefinedOperator.Decrement,
                "op_Division" => PredefinedOperator.Division,
                "op_Equality" => PredefinedOperator.Equality,
                "op_ExclusiveOr" => PredefinedOperator.ExclusiveOr,
                "op_Exponent" => PredefinedOperator.Exponent,
                "op_GreaterThan" => PredefinedOperator.GreaterThan,
                "op_GreaterThanOrEqual" => PredefinedOperator.GreaterThanOrEqual,
                "op_Increment" => PredefinedOperator.Increment,
                "op_Inequality" => PredefinedOperator.Inequality,
                "op_IntegerDivision" => PredefinedOperator.IntegerDivision,
                "op_LeftShift" => PredefinedOperator.LeftShift,
                "op_LessThan" => PredefinedOperator.LessThan,
                "op_LessThanOrEqual" => PredefinedOperator.LessThanOrEqual,
                "op_Like" => PredefinedOperator.Like,
                "op_LogicalNot" or "op_OnesComplement" => PredefinedOperator.Complement,
                "op_Modulus" => PredefinedOperator.Modulus,
                "op_Multiply" => PredefinedOperator.Multiplication,
                "op_RightShift" => PredefinedOperator.RightShift,
                "op_Subtraction" or "op_UnaryNegation" => PredefinedOperator.Subtraction,
                _ => PredefinedOperator.None,
            };

        public static bool IsEntryPoint(this IMethodSymbol methodSymbol, INamedTypeSymbol? taskType, INamedTypeSymbol? genericTaskType)
            => methodSymbol.Name is WellKnownMemberNames.EntryPointMethodName or WellKnownMemberNames.TopLevelStatementsEntryPointMethodName &&
               methodSymbol.IsStatic &&
               (methodSymbol.ReturnsVoid ||
                methodSymbol.ReturnType.SpecialType == SpecialType.System_Int32 ||
                methodSymbol.ReturnType.OriginalDefinition.Equals(taskType) ||
                methodSymbol.ReturnType.OriginalDefinition.Equals(genericTaskType));
    }
}
