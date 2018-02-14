// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class Binder
    {
        /// <summary>
        /// If the left and right are tuples of matching cardinality, we'll try to bind the operator element-wise.
        /// When that succeeds, the element-wise conversions are collected and their result type is applied as tuple conversions.
        /// The element-wise binary operators are collected and stored as a tree for lowering.
        /// </summary>
        private BoundTupleBinaryOperator BindTupleBinaryOperator(BinaryExpressionSyntax node, BinaryOperatorKind kind,
            BoundExpression left, BoundExpression right, DiagnosticBag diagnostics)
        {
            TupleBinaryOperatorInfo operators = BindTupleBinaryOperatorInfo(node, kind, left, right, diagnostics);

            BoundExpression convertedLeft = GenerateConversionForAssignment(operators.LeftConvertedType, left, diagnostics);
            BoundExpression convertedRight = GenerateConversionForAssignment(operators.RightConvertedType, right, diagnostics);

            TypeSymbol resultType = GetSpecialType(SpecialType.System_Boolean, diagnostics, node);
            return new BoundTupleBinaryOperator(node, convertedLeft, convertedRight, kind, operators, resultType);
        }

        private TupleBinaryOperatorInfo BindTupleBinaryOperatorInfo(BinaryExpressionSyntax node, BinaryOperatorKind kind,
            BoundExpression left, BoundExpression right, DiagnosticBag diagnostics)
        {
            int? leftCardinality = GetTupleCardinality(left);
            int? rightCardinality = GetTupleCardinality(right);

            if (leftCardinality.HasValue && rightCardinality.HasValue)
            {
                TypeSymbol leftType = left.Type;
                TypeSymbol rightType = right.Type;

                if (leftCardinality.Value != rightCardinality.Value)
                {
                    Error(diagnostics, ErrorCode.ERR_TupleSizesMismatchForBinOps, node, leftCardinality, rightCardinality);

                    return new TupleBinaryOperatorInfo.Single(leftType ?? CreateErrorType(), rightType ?? CreateErrorType(),
                        BinaryOperatorKind.Error, methodSymbolOpt: null, boolConversion: Conversion.NoConversion, boolOperator: default);
                }

                // typeless tuple literals are not nullable
                bool leftNullable = leftType?.IsNullableType() == true;
                bool rightNullable = rightType?.IsNullableType() == true;

                return BindTupleBinaryOperatorNestedInfo(node, kind,
                    GetTupleArgumentsOrPlaceholders(left), GetTupleArgumentsOrPlaceholders(right),
                    leftNullable || rightNullable, diagnostics);
            }

            return BindTupleBinaryOperatorSingleInfo(node, kind, left, right, diagnostics);
        }

        private TupleBinaryOperatorInfo BindTupleBinaryOperatorSingleInfo(BinaryExpressionSyntax node, BinaryOperatorKind kind,
            BoundExpression left, BoundExpression right, DiagnosticBag diagnostics)
        {
            TypeSymbol leftType = left.Type;
            TypeSymbol rightType = right.Type;

            if ((object)leftType != null && leftType.IsDynamic() || (object)rightType != null && rightType.IsDynamic())
            {
                return BindTupleDynamicBinaryOperatorSingleInfo(node, kind, left, right, diagnostics);
            }

            LookupResultKind resultKind;
            BinaryOperatorAnalysisResult best = this.BinaryOperatorOverloadResolution(kind, left, right, node, diagnostics, out resultKind, out var _);

            MethodSymbol resultMethod = null;
            BinaryOperatorKind resultOperatorKind;
            TypeSymbol convertedLeftType;
            TypeSymbol convertedRightType;
            TypeSymbol returnType;
            bool hasErrors;

            if (!best.HasValue)
            {
                resultOperatorKind = kind;
                convertedLeftType = leftType ?? CreateErrorType();
                convertedRightType = rightType ?? CreateErrorType();
                returnType = CreateErrorType();
                hasErrors = true;
            }
            else
            {
                bool leftNull = left.IsLiteralNull();
                bool rightNull = right.IsLiteralNull();

                var signature = best.Signature;

                bool isObjectEquality = signature.Kind == BinaryOperatorKind.ObjectEqual || signature.Kind == BinaryOperatorKind.ObjectNotEqual;

                resultOperatorKind = signature.Kind;
                resultMethod = signature.Method;
                convertedLeftType = signature.LeftType;
                convertedRightType = signature.RightType;
                returnType = signature.ReturnType; // PROTOTYPE(tuple-equality) Do we need a special case for nullable equality? (see BindSimpleBinaryOperator)

                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                hasErrors = isObjectEquality && !BuiltInOperators.IsValidObjectEquality(Conversions, leftType, leftNull, rightType, rightNull, ref useSiteDiagnostics);
                diagnostics.Add(node, useSiteDiagnostics);
            }

            if (hasErrors)
            {
                ReportBinaryOperatorError(node, diagnostics, node.OperatorToken, left, right, resultKind);
            }

            PrepareBoolConversionAndTruthOperator(returnType, node, kind, diagnostics, out Conversion boolConversion, out UnaryOperatorSignature boolOperator);
            return new TupleBinaryOperatorInfo.Single(convertedLeftType, convertedRightType, resultOperatorKind, resultMethod, boolConversion, boolOperator);
        }

        /// <summary>
        /// If a element-wise binary operator returns a non-bool type, we will either:
        /// - prepare a conversion to bool if one exists
        /// - prepare a truth operator: op_false in the case of an equality (`a == b` will be lowered to `!((a == b).op_false)) or op_true in the case of inequality
        /// </summary>
        private void PrepareBoolConversionAndTruthOperator(TypeSymbol type, BinaryExpressionSyntax node, BinaryOperatorKind binaryOperator, DiagnosticBag diagnostics,
            out Conversion boolConversion, out UnaryOperatorSignature boolOperator)
        {
            BoundExpression comparisonResult = new BoundTupleOperandPlaceholder(node, type);
            var boolean = GetSpecialType(SpecialType.System_Boolean, diagnostics, node);

            // Is the operand implicitly convertible to bool?

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            var conversion = this.Conversions.ClassifyConversionFromExpression(comparisonResult, boolean, ref useSiteDiagnostics);
            diagnostics.Add(node, useSiteDiagnostics);

            if (conversion.IsImplicit)
            {
                boolConversion = conversion;
                boolOperator = default;
                return;
            }

            // It was not. Does it implement operator true (or false)?

            UnaryOperatorKind boolOpKind;
            switch (binaryOperator)
            {
                case BinaryOperatorKind.Equal:
                    boolOpKind = UnaryOperatorKind.False;
                    break;
                case BinaryOperatorKind.NotEqual:
                    boolOpKind = UnaryOperatorKind.True;
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(binaryOperator);
            }

            LookupResultKind resultKind;
            ImmutableArray<MethodSymbol> originalUserDefinedOperators;
            var best = this.UnaryOperatorOverloadResolution(boolOpKind, comparisonResult, node, diagnostics, out resultKind, out originalUserDefinedOperators);
            if (best.HasValue)
            {
                boolConversion = best.Conversion;
                boolOperator = best.Signature;
                return;
            }

            // It did not. Give a "not convertible to bool" error.

            GenerateImplicitConversionError(diagnostics, node, conversion, comparisonResult, boolean);
            boolConversion = Conversion.NoConversion;
            boolOperator = default;
            return;
        }

        private TupleBinaryOperatorInfo BindTupleDynamicBinaryOperatorSingleInfo(BinaryExpressionSyntax node, BinaryOperatorKind kind,
            BoundExpression left, BoundExpression right, DiagnosticBag diagnostics)
        {
            // This method binds binary == and != operators where one or both of the operands are dynamic.
            Debug.Assert((object)left.Type != null && left.Type.IsDynamic() || (object)right.Type != null && right.Type.IsDynamic());

            bool hasError = false;
            if (!IsLegalDynamicOperand(left) || !IsLegalDynamicOperand(right))
            {
                // Operator '{0}' cannot be applied to operands of type '{1}' and '{2}'
                Error(diagnostics, ErrorCode.ERR_BadBinaryOps, node, node.OperatorToken.Text, left.Display, right.Display);
                hasError = true;
            }

            BinaryOperatorKind elementOperatorKind = hasError ? kind : kind.WithType(BinaryOperatorKind.Dynamic);
            TypeSymbol dynamicType = Compilation.DynamicType;

            // We'll want to dynamically invoke operators op_true (/op_false) for equality (/inequality) comparison, but we don't need
            // to prepare either a conversion or a truth operator. Those can just be synthesized during lowering.
            return new TupleBinaryOperatorInfo.Single(dynamicType, dynamicType, elementOperatorKind,
                methodSymbolOpt: null, boolConversion: Conversion.Identity, boolOperator: default);
        }

        private TupleBinaryOperatorInfo BindTupleBinaryOperatorNestedInfo(BinaryExpressionSyntax node, BinaryOperatorKind kind,
            ImmutableArray<BoundExpression> left, ImmutableArray<BoundExpression> right,
            bool isNullable, DiagnosticBag diagnostics)
        {
            int length = left.Length;
            Debug.Assert(length == right.Length);

            var operatorsBuilder = ArrayBuilder<TupleBinaryOperatorInfo>.GetInstance(length);

            for (int i = 0; i < length; i++)
            {
                operatorsBuilder.Add(BindTupleBinaryOperatorInfo(node, kind, left[i], right[i], diagnostics));
            }

            var compilation = this.Compilation;
            var operators = operatorsBuilder.ToImmutableAndFree();
            TypeSymbol leftTupleType = MakeConvertedType(operators, node.Left, left, isNullable, compilation, diagnostics, isRight: false);
            TypeSymbol rightTupleType = MakeConvertedType(operators, node.Right, right, isNullable, compilation, diagnostics, isRight: true);

            return new TupleBinaryOperatorInfo.Multiple(operators, leftTupleType, rightTupleType);
        }

        private static int? GetTupleCardinality(BoundExpression expr)
        {
            if (expr.Kind == BoundKind.TupleLiteral)
            {
                var tuple = (BoundTupleLiteral)expr;
                return tuple.Arguments.Length;
            }

            TypeSymbol type = expr.Type;
            if (type is null)
            {
                return null;
            }

            type = type.StrippedType();

            if (type.IsTupleType)
            {
                return type.TupleElementTypes.Length;
            }

            return null;
        }

        private static ImmutableArray<BoundExpression> GetTupleArgumentsOrPlaceholders(BoundExpression expr)
        {
            if (expr.Kind == BoundKind.TupleLiteral)
            {
                return ((BoundTupleLiteral)expr).Arguments;
            }

            // placeholder bound nodes with the proper types are sufficient to bind the element-wise binary operators
            return expr.Type.StrippedType().TupleElementTypes
                .SelectAsArray((t, s) => (BoundExpression)new BoundTupleOperandPlaceholder(s, t), expr.Syntax);
        }

        /// <summary>
        /// Make a tuple type (with appropriate nesting) from the types (on the left or on the right) collected
        /// from binding element-wise binary operators.
        /// </summary>
        private TypeSymbol MakeConvertedType(ImmutableArray<TupleBinaryOperatorInfo> operators, CSharpSyntaxNode syntax,
            ImmutableArray<BoundExpression> elements, bool isNullable, CSharpCompilation compilation, DiagnosticBag diagnostics, bool isRight)
        {
            ImmutableArray<TypeSymbol> convertedTypes = operators.SelectAsArray((o, r) => r ? o.RightConvertedType : o.LeftConvertedType, isRight);
            ImmutableArray<Location> elementLocations = elements.SelectAsArray(e => e.Syntax.Location);

            var tuple = TupleTypeSymbol.Create(locationOpt: null, elementTypes: convertedTypes,
                elementLocations, elementNames: default, compilation,
                shouldCheckConstraints: true, errorPositions: default, syntax, diagnostics);

            if (!isNullable)
            {
                return tuple;
            }

            NamedTypeSymbol nullableT = GetSpecialType(SpecialType.System_Nullable_T, diagnostics, syntax);
            return nullableT.Construct(tuple);
        }
    }
}
