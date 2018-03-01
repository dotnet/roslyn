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
            // PROTOTYPE(tuple-equality) Block in expression tree

            TupleBinaryOperatorInfo.Multiple operators = BindTupleBinaryOperatorNestedInfo(node, kind, left, right, diagnostics);

            // PROTOTYPE(tuple-equality) We'll save the converted nodes separately, for the semantic model
            //BoundExpression convertedLeft = GenerateConversionForAssignment(operators.LeftConvertedType, left, diagnostics);
            //BoundExpression convertedRight = GenerateConversionForAssignment(operators.RightConvertedType, right, diagnostics);
            BoundExpression convertedLeft = left;
            BoundExpression convertedRight = right;

            TypeSymbol resultType = GetSpecialType(SpecialType.System_Boolean, diagnostics, node);
            return new BoundTupleBinaryOperator(node, convertedLeft, convertedRight, kind, operators, resultType);
        }

        private TupleBinaryOperatorInfo BindTupleBinaryOperatorInfo(BinaryExpressionSyntax node, BinaryOperatorKind kind,
            BoundExpression left, BoundExpression right, DiagnosticBag diagnostics)
        {
            TypeSymbol leftType = left.Type;
            TypeSymbol rightType = right.Type;

            if ((object)leftType != null && leftType.IsDynamic() || (object)rightType != null && rightType.IsDynamic())
            {
                return BindTupleDynamicBinaryOperatorSingleInfo(node, kind, left, right, diagnostics);
            }

            if (GetTupleCardinality(left) > 1 && GetTupleCardinality(right) > 1)
            {
                return BindTupleBinaryOperatorNestedInfo(node, kind, left, right, diagnostics);
            }

            LookupResultKind resultKind;
            BinaryOperatorSignature signature;
            BinaryOperatorAnalysisResult analysisResult;

            bool foundOperator = BindSimpleBinaryOperatorParts(node, diagnostics, left, right, kind,
                out resultKind, originalUserDefinedOperators: out _, out signature, out analysisResult);

            if (!foundOperator)
            {
                ReportBinaryOperatorError(node, diagnostics, node.OperatorToken, left, right, resultKind);
            }

            TypeSymbol convertedLeftType = signature.LeftType;
            TypeSymbol convertedRightType = signature.RightType;

            if (convertedLeftType is null)
            {
                Debug.Assert(convertedRightType is null);

                convertedLeftType = leftType ?? CreateErrorType();
                convertedRightType = rightType ?? CreateErrorType();
            }

            PrepareBoolConversionAndTruthOperator(signature.ReturnType, node, kind, diagnostics, out Conversion boolConversion, out UnaryOperatorSignature boolOperator);
            return new TupleBinaryOperatorInfo.Single(convertedLeftType, convertedRightType, signature.Kind, analysisResult.LeftConversion, analysisResult.RightConversion, signature.Method, boolConversion, boolOperator);
        }

        /// <summary>
        /// If a element-wise binary operator returns a non-bool type, we will either:
        /// - prepare a conversion to bool if one exists
        /// - prepare a truth operator: op_false in the case of an equality (`a == b` will be lowered to `!((a == b).op_false)) or op_true in the case of inequality
        /// </summary>
        private void PrepareBoolConversionAndTruthOperator(TypeSymbol type, BinaryExpressionSyntax node, BinaryOperatorKind binaryOperator, DiagnosticBag diagnostics,
            out Conversion boolConversion, out UnaryOperatorSignature boolOperator)
        {
            // Is the operand implicitly convertible to bool?

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            var boolean = GetSpecialType(SpecialType.System_Boolean, diagnostics, node);
            var conversion = this.Conversions.ClassifyImplicitConversionFromType(type, boolean, ref useSiteDiagnostics);
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
            BoundExpression comparisonResult = new BoundTupleOperandPlaceholder(node, type);
            UnaryOperatorAnalysisResult best = this.UnaryOperatorOverloadResolution(boolOpKind, comparisonResult, node, diagnostics, out resultKind, out originalUserDefinedOperators);
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
                leftConversion: Conversion.NoConversion, rightConversion: Conversion.NoConversion,
                methodSymbolOpt: null, boolConversion: Conversion.Identity, boolOperator: default);
        }

        private TupleBinaryOperatorInfo.Multiple BindTupleBinaryOperatorNestedInfo(BinaryExpressionSyntax node, BinaryOperatorKind kind,
            BoundExpression left, BoundExpression right, DiagnosticBag diagnostics)
        {
            TypeSymbol leftType = left.Type;
            TypeSymbol rightType = right.Type;

            int leftCardinality = GetTupleCardinality(left);
            int rightCardinality = GetTupleCardinality(right);

            if (leftCardinality != rightCardinality)
            {
                Error(diagnostics, ErrorCode.ERR_TupleSizesMismatchForBinOps, node, leftCardinality, rightCardinality);

                return new TupleBinaryOperatorInfo.Multiple(ImmutableArray<TupleBinaryOperatorInfo>.Empty, leftType ?? CreateErrorType(), rightType ?? CreateErrorType());
            }

            // typeless tuple literals are not nullable
            bool leftNullable = leftType?.IsNullableType() == true;
            bool rightNullable = rightType?.IsNullableType() == true;

            var leftParts = GetTupleArgumentsOrPlaceholders(left);
            var rightParts = GetTupleArgumentsOrPlaceholders(right);

            int length = leftParts.Length;
            Debug.Assert(length == rightParts.Length);

            var operatorsBuilder = ArrayBuilder<TupleBinaryOperatorInfo>.GetInstance(length);

            for (int i = 0; i < length; i++)
            {
                operatorsBuilder.Add(BindTupleBinaryOperatorInfo(node, kind, leftParts[i], rightParts[i], diagnostics));
            }

            var compilation = this.Compilation;
            var operators = operatorsBuilder.ToImmutableAndFree();
            bool isNullable = leftNullable || rightNullable;
            TypeSymbol leftTupleType = MakeConvertedType(operators.SelectAsArray(o => o.LeftConvertedType), node.Left, leftParts, isNullable, compilation, diagnostics);
            TypeSymbol rightTupleType = MakeConvertedType(operators.SelectAsArray(o => o.RightConvertedType), node.Right, rightParts, isNullable, compilation, diagnostics);

            return new TupleBinaryOperatorInfo.Multiple(operators, leftTupleType, rightTupleType);
        }

        private static int GetTupleCardinality(BoundExpression expr)
        {
            if (expr.Kind == BoundKind.TupleLiteral)
            {
                var tuple = (BoundTupleLiteral)expr;
                return tuple.Arguments.Length;
            }

            TypeSymbol type = expr.Type;
            if (type is null)
            {
                return -1;
            }

            type = type.StrippedType();

            if (type.IsTupleType)
            {
                return type.TupleElementTypes.Length;
            }

            return -1;
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
        private TypeSymbol MakeConvertedType(ImmutableArray<TypeSymbol> convertedTypes, CSharpSyntaxNode syntax,
            ImmutableArray<BoundExpression> elements, bool isNullable, CSharpCompilation compilation, DiagnosticBag diagnostics)
        {
            ImmutableArray<Location> elementLocations = elements.SelectAsArray(e => e.Syntax.Location);

            var tuple = TupleTypeSymbol.Create(locationOpt: null, elementTypes: convertedTypes,
                elementLocations, elementNames: default, compilation,
                shouldCheckConstraints: true, errorPositions: default, syntax, diagnostics);

            if (!isNullable)
            {
                return tuple;
            }

            // PROTOTYPE(tuple-equality) check constraints
            NamedTypeSymbol nullableT = GetSpecialType(SpecialType.System_Nullable_T, diagnostics, syntax);
            return nullableT.Construct(tuple);
        }
    }
}
