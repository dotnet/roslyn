// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
        /// When that succeeds, the element-wise conversions are collected. We keep them for semantic model, and use some to fix typeless elements.
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

            BoundExpression fixedLeft = FixTypelessElements(left, operators, diagnostics, isRight: false);
            BoundExpression fixedRight = FixTypelessElements(right, operators, diagnostics, isRight: true);

            TypeSymbol resultType = GetSpecialType(SpecialType.System_Boolean, diagnostics, node);
            return new BoundTupleBinaryOperator(node, fixedLeft, fixedRight, kind, operators, resultType);
        }

        /// <summary>
        /// Walk down tuple literals and give a type to all the terminal typeless elements that need one.
        /// Terminal elements are those that correspond to a Single in the tree of operators.
        /// Elements with constant value (such as `null`) don't get converted, to benefit from lowering optimizations.
        /// </summary>
        private BoundExpression FixTypelessElements(BoundExpression expr, TupleBinaryOperatorInfo op, DiagnosticBag diagnostics, bool isRight)
        {
            if (op.IsSingle())
            {
                if (expr.Type is null)
                {
                    Debug.Assert(((TupleBinaryOperatorInfo.Single)op) is var single && (isRight ? single.TypelessRight : single.TypelessLeft));

                    // Use the type from the other side
                    return GenerateConversionForAssignment(isRight ? op.LeftConvertedType : op.RightConvertedType, expr, diagnostics);
                }

                return expr;
            }

            var multiple = (TupleBinaryOperatorInfo.Multiple)op;
            ImmutableArray<TupleBinaryOperatorInfo> operators = multiple.Operators;

            if (expr.Kind == BoundKind.TupleLiteral)
            {
                var tuple = (BoundTupleLiteral)expr;
                ImmutableArray<BoundExpression> arguments = tuple.Arguments;
                Debug.Assert(tuple.Arguments.Length == operators.Length);

                ArrayBuilder<BoundExpression> builder = null;
                for (int i = 0; i < arguments.Length; i++)
                {
                    BoundExpression arg = arguments[i];

                    BoundExpression fixedArg = FixTypelessElements(arg, operators[i], diagnostics, isRight);
                    if ((object)fixedArg != arg)
                    {
                        add(ref builder, fixedArg, arguments, i);
                        continue;
                    }

                    if (builder != null)
                    {
                        builder.Add(arg);
                    }
                }

                if (builder != null)
                {
                    ImmutableArray<BoundExpression> newArguments = builder.ToImmutableAndFree();

                    var newTupleType = TupleTypeSymbol.Create(locationOpt: null, newArguments.SelectAsArray(a => a.Type),
                        elementLocations: default, elementNames: default, Compilation, shouldCheckConstraints: false, errorPositions: default);

                    return new BoundTupleLiteral(tuple.Syntax, argumentNamesOpt: default, inferredNamesOpt: default, newArguments, newTupleType);
                }
            }

            return expr;

            void add(ref ArrayBuilder<BoundExpression> result, BoundExpression value, ImmutableArray<BoundExpression> values, int n)
            {
                if (result is null)
                {
                    result = ArrayBuilder<BoundExpression>.GetInstance(values.Length);
                    result.AddRange(values, n);
                }
                result.Add(value);
            }
        }

        /// <summary>
        /// Binds:
        /// 1. dynamically, if either side is dynamic
        /// 2. as tuple binary operator, if both sides are tuples of matching cardinalities
        /// 3. as regular binary operator otherwise
        /// </summary>
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

            PrepareBoolConversionAndTruthOperator(signature.ReturnType, node, kind, diagnostics, out Conversion conversionIntoBoolOperator, out UnaryOperatorSignature boolOperator);

            return new TupleBinaryOperatorInfo.Single(convertedLeftType, convertedRightType, signature.Kind,
                analysisResult.LeftConversion, analysisResult.RightConversion, signature.Method,
                left.Type is null, right.Type is null, conversionIntoBoolOperator, boolOperator);
        }

        /// <summary>
        /// If a element-wise binary operator returns a non-bool type, we will either:
        /// - prepare a conversion to bool if one exists
        /// - prepare a truth operator: op_false in the case of an equality (`a == b` will be lowered to `!((a == b).op_false)) or op_true in the case of inequality,
        ///     with the conversion being used for its input.
        /// </summary>
        private void PrepareBoolConversionAndTruthOperator(TypeSymbol type, BinaryExpressionSyntax node, BinaryOperatorKind binaryOperator, DiagnosticBag diagnostics,
            out Conversion conversionForBool, out UnaryOperatorSignature boolOperator)
        {
            // Is the operand implicitly convertible to bool?

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            TypeSymbol boolean = GetSpecialType(SpecialType.System_Boolean, diagnostics, node);
            Conversion conversion = this.Conversions.ClassifyImplicitConversionFromType(type, boolean, ref useSiteDiagnostics);
            diagnostics.Add(node, useSiteDiagnostics);

            if (conversion.IsImplicit)
            {
                conversionForBool = conversion;
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
                conversionForBool = best.Conversion;
                boolOperator = best.Signature;
                return;
            }

            // It did not. Give a "not convertible to bool" error.

            GenerateImplicitConversionError(diagnostics, node, conversion, comparisonResult, boolean);
            conversionForBool = Conversion.NoConversion;
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
                methodSymbolOpt: null, typelessLeft: left.Type is null, typelessRight: right.Type is null,
                conversionForBool: Conversion.Identity, boolOperator: default);
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

            ImmutableArray<BoundExpression> leftParts = GetTupleArgumentsOrPlaceholders(left);
            ImmutableArray<BoundExpression> rightParts = GetTupleArgumentsOrPlaceholders(right);

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

            // PROTOTYPE(tuple-equality) Add test for violated tuple constraint
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
