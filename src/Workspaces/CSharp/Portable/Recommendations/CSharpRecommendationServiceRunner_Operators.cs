// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Recommendations
{
    internal partial class CSharpRecommendationServiceRunner
    {
        private void AddOperators(ExpressionSyntax originalExpression, ITypeSymbol _, ArrayBuilder<ISymbol> symbols)
        {
            var semanticModel = _context.SemanticModel;
            var container = semanticModel.GetTypeInfo(originalExpression, _cancellationToken).Type;
            if (container == null)
                return;

            var containerWithoutNullable = container.RemoveNullableIfPresent();

            if (IsExcludedOperator(container))
                return;

            var containerIsNullable = container.IsNullable();
            var compilation = semanticModel.Compilation;
            foreach (var type in containerWithoutNullable.GetBaseTypesAndThis())
            {
                foreach (var member in type.GetMembers())
                {
                    if (member is not IMethodSymbol method)
                        continue;

                    if (!method.IsUserDefinedOperator())
                        continue;

                    if (method.Name is WellKnownMemberNames.TrueOperatorName or WellKnownMemberNames.FalseOperatorName)
                        continue;

                    if (containerIsNullable && !IsLiftableOperator(method))
                        continue;

                    symbols.Add(containerIsNullable ? LiftOperator(compilation, method) : method);
                }
            }
        }

        private static bool IsExcludedOperator(ITypeSymbol container)
        {
            return container.IsSpecialType() ||
                container.SpecialType == SpecialType.System_IntPtr ||
                container.SpecialType == SpecialType.System_UIntPtr;
        }

        private static bool IsLiftableOperator(IMethodSymbol symbol)
        {
            // https://github.com/dotnet/csharplang/blob/main/spec/expressions.md#lifted-operators

            // Common for all:
            if (symbol.IsUserDefinedOperator() && symbol.Parameters.All(p => p.Type.IsValueType))
            {
                switch (symbol.Name)
                {
                    // Unary
                    case WellKnownMemberNames.UnaryPlusOperatorName:
                    case WellKnownMemberNames.IncrementOperatorName:
                    case WellKnownMemberNames.UnaryNegationOperatorName:
                    case WellKnownMemberNames.DecrementOperatorName:
                    case WellKnownMemberNames.LogicalNotOperatorName:
                    case WellKnownMemberNames.OnesComplementOperatorName:
                        return symbol.Parameters.Length == 1 && symbol.ReturnType.IsValueType;

                    // Binary 
                    case WellKnownMemberNames.AdditionOperatorName:
                    case WellKnownMemberNames.SubtractionOperatorName:
                    case WellKnownMemberNames.MultiplyOperatorName:
                    case WellKnownMemberNames.DivisionOperatorName:
                    case WellKnownMemberNames.ModulusOperatorName:
                    case WellKnownMemberNames.BitwiseAndOperatorName:
                    case WellKnownMemberNames.BitwiseOrOperatorName:
                    case WellKnownMemberNames.ExclusiveOrOperatorName:
                    case WellKnownMemberNames.LeftShiftOperatorName:
                    case WellKnownMemberNames.RightShiftOperatorName:
                        return symbol.Parameters.Length == 2 && symbol.ReturnType.IsValueType;

                    // Equality + Relational 
                    case WellKnownMemberNames.EqualityOperatorName:
                    case WellKnownMemberNames.InequalityOperatorName:
                    case WellKnownMemberNames.LessThanOperatorName:
                    case WellKnownMemberNames.GreaterThanOperatorName:
                    case WellKnownMemberNames.LessThanOrEqualOperatorName:
                    case WellKnownMemberNames.GreaterThanOrEqualOperatorName:
                        return symbol.Parameters.Length == 2 && symbol.ReturnType.SpecialType == SpecialType.System_Boolean;
                }
            }

            return false;
        }

        private IMethodSymbol LiftOperator(Compilation compilation, IMethodSymbol symbol)
        {
            var nullableType = compilation.GetSpecialType(SpecialType.System_Nullable_T);

            var returnType = symbol.Name switch
            {
                // Unary and binary:
                WellKnownMemberNames.UnaryPlusOperatorName or
                WellKnownMemberNames.IncrementOperatorName or
                WellKnownMemberNames.UnaryNegationOperatorName or
                WellKnownMemberNames.DecrementOperatorName or
                WellKnownMemberNames.LogicalNotOperatorName or
                WellKnownMemberNames.OnesComplementOperatorName or
                WellKnownMemberNames.AdditionOperatorName or
                WellKnownMemberNames.SubtractionOperatorName or
                WellKnownMemberNames.MultiplyOperatorName or
                WellKnownMemberNames.DivisionOperatorName or
                WellKnownMemberNames.ModulusOperatorName or
                WellKnownMemberNames.BitwiseAndOperatorName or
                WellKnownMemberNames.BitwiseOrOperatorName or
                WellKnownMemberNames.ExclusiveOrOperatorName or
                WellKnownMemberNames.LeftShiftOperatorName or
                WellKnownMemberNames.RightShiftOperatorName => nullableType.Construct(symbol.ReturnType),
                _ => symbol.ReturnType,
            };

            return CodeGenerationSymbolFactory.CreateOperatorSymbol(
                symbol.GetAttributes(),
                symbol.DeclaredAccessibility,
                DeclarationModifiers.From(symbol),
                nullableType.Construct(symbol.ReturnType),
                GetOperatorKind(symbol.Name),
                symbol.Parameters.SelectAsArray(
                    p => CodeGenerationSymbolFactory.CreateParameterSymbol(p, type: nullableType.Construct(p.Type))),
                returnTypeAttributes: symbol.GetReturnTypeAttributes(),
                documentationCommentXml: symbol.GetDocumentationCommentXml(cancellationToken: _cancellationToken));
        }

        private static CodeGenerationOperatorKind GetOperatorKind(string name)
        {
            return name switch
            {
                WellKnownMemberNames.AdditionOperatorName => CodeGenerationOperatorKind.Addition,
                WellKnownMemberNames.BitwiseAndOperatorName => CodeGenerationOperatorKind.BitwiseAnd,
                WellKnownMemberNames.BitwiseOrOperatorName => CodeGenerationOperatorKind.BitwiseOr,
                WellKnownMemberNames.ConcatenateOperatorName => CodeGenerationOperatorKind.Concatenate,
                WellKnownMemberNames.DecrementOperatorName => CodeGenerationOperatorKind.Decrement,
                WellKnownMemberNames.DivisionOperatorName => CodeGenerationOperatorKind.Division,
                WellKnownMemberNames.EqualityOperatorName => CodeGenerationOperatorKind.Equality,
                WellKnownMemberNames.ExclusiveOrOperatorName => CodeGenerationOperatorKind.ExclusiveOr,
                WellKnownMemberNames.ExponentOperatorName => CodeGenerationOperatorKind.Exponent,
                WellKnownMemberNames.FalseOperatorName => CodeGenerationOperatorKind.False,
                WellKnownMemberNames.GreaterThanOperatorName => CodeGenerationOperatorKind.GreaterThan,
                WellKnownMemberNames.GreaterThanOrEqualOperatorName => CodeGenerationOperatorKind.GreaterThanOrEqual,
                WellKnownMemberNames.IncrementOperatorName => CodeGenerationOperatorKind.Increment,
                WellKnownMemberNames.InequalityOperatorName => CodeGenerationOperatorKind.Inequality,
                WellKnownMemberNames.IntegerDivisionOperatorName => CodeGenerationOperatorKind.IntegerDivision,
                WellKnownMemberNames.LeftShiftOperatorName => CodeGenerationOperatorKind.LeftShift,
                WellKnownMemberNames.LessThanOperatorName => CodeGenerationOperatorKind.LessThan,
                WellKnownMemberNames.LessThanOrEqualOperatorName => CodeGenerationOperatorKind.LessThanOrEqual,
                WellKnownMemberNames.LikeOperatorName => CodeGenerationOperatorKind.Like,
                WellKnownMemberNames.LogicalNotOperatorName => CodeGenerationOperatorKind.LogicalNot,
                WellKnownMemberNames.ModulusOperatorName => CodeGenerationOperatorKind.Modulus,
                WellKnownMemberNames.MultiplyOperatorName => CodeGenerationOperatorKind.Multiplication,
                WellKnownMemberNames.OnesComplementOperatorName => CodeGenerationOperatorKind.OnesComplement,
                WellKnownMemberNames.RightShiftOperatorName => CodeGenerationOperatorKind.RightShift,
                WellKnownMemberNames.SubtractionOperatorName => CodeGenerationOperatorKind.Subtraction,
                WellKnownMemberNames.TrueOperatorName => CodeGenerationOperatorKind.True,
                WellKnownMemberNames.UnaryPlusOperatorName => CodeGenerationOperatorKind.UnaryPlus,
                WellKnownMemberNames.UnaryNegationOperatorName => CodeGenerationOperatorKind.UnaryNegation,
                _ => throw ExceptionUtilities.UnexpectedValue(name),
            };
        }
    }
}
