﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.LanguageService;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class IMethodSymbolExtensions
    {
        /// <summary>
        /// Returns the methodSymbol and any partial parts.
        /// </summary>
        public static ImmutableArray<IMethodSymbol> GetAllMethodSymbolsOfPartialParts(this IMethodSymbol method)
        {
            if (method.PartialDefinitionPart != null)
            {
                Debug.Assert(method.PartialImplementationPart == null && !Equals(method.PartialDefinitionPart, method));
                return ImmutableArray.Create(method, method.PartialDefinitionPart);
            }
            else if (method.PartialImplementationPart != null)
            {
                Debug.Assert(!Equals(method.PartialImplementationPart, method));
                return ImmutableArray.Create(method.PartialImplementationPart, method);
            }
            else
            {
                return ImmutableArray.Create(method);
            }
        }

        /// <summary>
        /// Returns true for void returning methods with two parameters, where
        /// the first parameter is of <see cref="object"/> type and the second
        /// parameter inherits from or equals <see cref="EventArgs"/> type.
        /// </summary>
        public static bool HasEventHandlerSignature(this IMethodSymbol method, [NotNullWhen(returnValue: true)] INamedTypeSymbol? eventArgsType)
            => eventArgsType != null &&
               method.Parameters is [{ Type.SpecialType: SpecialType.System_Object }, var secondParam] &&
               secondParam.Type.InheritsFromOrEquals(eventArgsType);

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
                WellKnownMemberNames.AdditionOperatorName or WellKnownMemberNames.CheckedAdditionOperatorName or WellKnownMemberNames.UnaryPlusOperatorName => PredefinedOperator.Addition,
                WellKnownMemberNames.BitwiseAndOperatorName => PredefinedOperator.BitwiseAnd,
                WellKnownMemberNames.BitwiseOrOperatorName => PredefinedOperator.BitwiseOr,
                WellKnownMemberNames.ConcatenateOperatorName => PredefinedOperator.Concatenate,
                WellKnownMemberNames.DecrementOperatorName or WellKnownMemberNames.CheckedDecrementOperatorName => PredefinedOperator.Decrement,
                WellKnownMemberNames.DivisionOperatorName or WellKnownMemberNames.CheckedDivisionOperatorName => PredefinedOperator.Division,
                WellKnownMemberNames.EqualityOperatorName => PredefinedOperator.Equality,
                WellKnownMemberNames.ExclusiveOrOperatorName => PredefinedOperator.ExclusiveOr,
                WellKnownMemberNames.ExponentOperatorName => PredefinedOperator.Exponent,
                WellKnownMemberNames.GreaterThanOperatorName => PredefinedOperator.GreaterThan,
                WellKnownMemberNames.GreaterThanOrEqualOperatorName => PredefinedOperator.GreaterThanOrEqual,
                WellKnownMemberNames.IncrementOperatorName or WellKnownMemberNames.CheckedIncrementOperatorName => PredefinedOperator.Increment,
                WellKnownMemberNames.InequalityOperatorName => PredefinedOperator.Inequality,
                WellKnownMemberNames.IntegerDivisionOperatorName => PredefinedOperator.IntegerDivision,
                WellKnownMemberNames.LeftShiftOperatorName => PredefinedOperator.LeftShift,
                WellKnownMemberNames.LessThanOperatorName => PredefinedOperator.LessThan,
                WellKnownMemberNames.LessThanOrEqualOperatorName => PredefinedOperator.LessThanOrEqual,
                WellKnownMemberNames.LikeOperatorName => PredefinedOperator.Like,
                WellKnownMemberNames.LogicalNotOperatorName or WellKnownMemberNames.OnesComplementOperatorName => PredefinedOperator.Complement,
                WellKnownMemberNames.ModulusOperatorName => PredefinedOperator.Modulus,
                WellKnownMemberNames.MultiplyOperatorName or WellKnownMemberNames.CheckedMultiplyOperatorName => PredefinedOperator.Multiplication,
                WellKnownMemberNames.RightShiftOperatorName => PredefinedOperator.RightShift,
                WellKnownMemberNames.UnsignedRightShiftOperatorName => PredefinedOperator.UnsignedRightShift,
                WellKnownMemberNames.SubtractionOperatorName or WellKnownMemberNames.CheckedSubtractionOperatorName or WellKnownMemberNames.UnaryNegationOperatorName or WellKnownMemberNames.CheckedUnaryNegationOperatorName => PredefinedOperator.Subtraction,
                _ => PredefinedOperator.None,
            };

        public static bool IsEntryPoint(this IMethodSymbol methodSymbol, INamedTypeSymbol? taskType, INamedTypeSymbol? genericTaskType)
            => methodSymbol.Name is WellKnownMemberNames.EntryPointMethodName or WellKnownMemberNames.TopLevelStatementsEntryPointMethodName &&
               methodSymbol.IsStatic &&
               (methodSymbol.ReturnsVoid ||
                methodSymbol.ReturnType.SpecialType == SpecialType.System_Int32 ||
                methodSymbol.ReturnType.OriginalDefinition.Equals(taskType) ||
                methodSymbol.ReturnType.OriginalDefinition.Equals(genericTaskType));

        /// <summary>
        /// Tells if an async method returns a task-like type, awaiting for which produces <see langword="void"/> result
        /// </summary>
        public static bool IsAsyncReturningVoidTask(this IMethodSymbol method, Compilation compilation)
        {
            if (!method.IsAsync)
                return false;

            if (method.ReturnType is not INamedTypeSymbol { Arity: 0 })
                return false;

            // `Task` type doesn't have an `AsyncMethodBuilder` attribute, so we need to check for it separately
            return method.ReturnType.Equals(compilation.TaskType()) ||
                   method.ReturnType.GetAttributes().Any(a => a.AttributeClass?.Equals(compilation.AsyncMethodBuilderAttribute()) ?? false);
        }
    }
}
