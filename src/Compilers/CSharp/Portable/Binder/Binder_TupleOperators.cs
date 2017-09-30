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

            TypeSymbol leftConvertedType = operators.GetConvertedType(isRight: false);
            TypeSymbol rightConvertedType = operators.GetConvertedType(isRight: true);

            BoundExpression convertedLeft = GenerateConversionForAssignment(leftConvertedType, left, diagnostics);
            BoundExpression convertedRight = GenerateConversionForAssignment(rightConvertedType, right, diagnostics);

            TypeSymbol resultType = GetSpecialType(SpecialType.System_Boolean, diagnostics, node);
            return new BoundTupleBinaryOperator(node, convertedLeft, convertedRight, kind, operators, resultType);
        }

        private TupleBinaryOperatorInfo BindTupleBinaryOperatorInfo(BinaryExpressionSyntax node, BinaryOperatorKind kind,
            BoundExpression left, BoundExpression right, DiagnosticBag diagnostics)
        {
            TypeSymbol leftType = left.Type;
            TypeSymbol rightType = right.Type;
            int? leftCardinality = GetTupleCardinality(left);
            int? rightCardinality = GetTupleCardinality(right);

            if (leftCardinality.HasValue && rightCardinality.HasValue)
            {
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
            TypeSymbol convertedLeftType = null;
            TypeSymbol convertedRightType = null;
            TypeSymbol returnType;
            bool hasErrors;

            if (!best.HasValue)
            {
                resultOperatorKind = kind;
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

            if (convertedLeftType is null || convertedRightType is null)
            {
                convertedLeftType = convertedLeftType ?? leftType ?? CreateErrorType();
                convertedRightType = convertedRightType ?? rightType ?? CreateErrorType();
                hasErrors = true;
            }

            if (hasErrors)
            {
                ReportBinaryOperatorError(node, diagnostics, node.OperatorToken, left, right, resultKind);
                //resultOperatorKind &= ~BinaryOperatorKind.TypeMask; // PROTOTYPE(tuple-equality) Not sure what this is for
            }

            ConvertToBool(returnType, node, kind, diagnostics, out Conversion boolConversion, out UnaryOperatorSignature boolOperator);
            return new TupleBinaryOperatorInfo.Single(convertedLeftType, convertedRightType, resultOperatorKind, resultMethod, boolConversion, boolOperator);
        }

        private void ConvertToBool(TypeSymbol type, BinaryExpressionSyntax node, BinaryOperatorKind binaryOperator, DiagnosticBag diagnostics,
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
            bool leftValidOperand = IsLegalDynamicOperand(left);
            bool rightValidOperand = IsLegalDynamicOperand(right);

            if (!leftValidOperand || !rightValidOperand)
            {
                // Operator '{0}' cannot be applied to operands of type '{1}' and '{2}'
                Error(diagnostics, ErrorCode.ERR_BadBinaryOps, node, node.OperatorToken.Text, left.Display, right.Display);
                hasError = true;
            }

            BinaryOperatorKind elementOperatorKind = hasError ? kind : kind.WithType(BinaryOperatorKind.Dynamic);
            TypeSymbol dynamicType = Compilation.DynamicType;

            // We'll end up dynamically invoking operators true and false, so no result conversion. We'll deal with that during lowering.
            return new TupleBinaryOperatorInfo.Single(dynamicType, dynamicType, elementOperatorKind,
                methodSymbolOpt: null, boolConversion: Conversion.Identity, boolOperator: default);
        }

        private TupleBinaryOperatorInfo BindTupleBinaryOperatorNestedInfo(BinaryExpressionSyntax node, BinaryOperatorKind kind,
            ImmutableArray<BoundExpression> left, ImmutableArray<BoundExpression> right,
            bool nullable, DiagnosticBag diagnostics)
        {
            int length = left.Length;
            Debug.Assert(length == right.Length);

            var operatorsBuilder = ArrayBuilder<TupleBinaryOperatorInfo>.GetInstance(length);

            for (int i = 0; i < length; i++)
            {
                operatorsBuilder.Add(BindTupleBinaryOperatorInfo(node, kind, left[i], right[i], diagnostics));
            }

            var compilation = this.Compilation;
            TypeSymbol leftTupleType = MakeConvertedType(operatorsBuilder, node.Left, left, nullable, compilation, diagnostics, isRight: false);
            TypeSymbol rightTupleType = MakeConvertedType(operatorsBuilder, node.Right, right, nullable, compilation, diagnostics, isRight: true);

            return new TupleBinaryOperatorInfo.Nested(operatorsBuilder.ToImmutableAndFree(), leftTupleType, rightTupleType);
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
                .SelectAsArray(t => (BoundExpression)new BoundTupleOperandPlaceholder(expr.Syntax, t));
        }

        /// <summary>
        /// Make a tuple type (with appropriate nesting) from the types (on the left or on the right) collected
        /// from binding element-wise binary operators.
        /// </summary>
        private TypeSymbol MakeConvertedType(ArrayBuilder<TupleBinaryOperatorInfo> operators, CSharpSyntaxNode syntax,
            ImmutableArray<BoundExpression> elements, bool nullable, CSharpCompilation compilation, DiagnosticBag diagnostics, bool isRight)
        {
            ImmutableArray<TypeSymbol> convertedTypes = operators.SelectAsArray(o => o.GetConvertedType(isRight));
            ImmutableArray<Location> elementLocations = elements.SelectAsArray(e => e.Syntax.Location);

            var tuple = TupleTypeSymbol.Create(locationOpt: null, elementTypes: convertedTypes,
                elementLocations, elementNames: default, compilation,
                shouldCheckConstraints: true, errorPositions: default, syntax, diagnostics);

            if (!nullable)
            {
                return tuple;
            }

            NamedTypeSymbol nullableT = GetSpecialType(SpecialType.System_Nullable_T, diagnostics, syntax);
            return nullableT.Construct(tuple);
        }
    }

    /// <summary>
    /// A tree of binary operators
    /// </summary>
    internal abstract class TupleBinaryOperatorInfo
    {
        internal abstract TypeSymbol GetConvertedType(bool isRight);
        internal abstract bool IsSingle();
#if DEBUG
        internal abstract TreeDumperNode DumpCore();
        internal string Dump() => TreeDumper.DumpCompact(DumpCore());
#endif

        internal class Single : TupleBinaryOperatorInfo
        {
            internal TypeSymbol LeftConvertedType { get; }
            internal TypeSymbol RightConvertedType { get; }
            internal BinaryOperatorKind Kind { get; }
            internal MethodSymbol MethodSymbolOpt { get; } // User-defined comparison operator, if applicable

            // To convert the result of comparison to bool
            internal Conversion BoolConversion { get; }
            internal UnaryOperatorSignature BoolOperator { get; } // Information for op_true or op_false

            internal Single(TypeSymbol leftConvertedType, TypeSymbol rightConvertedType, BinaryOperatorKind kind,
                MethodSymbol methodSymbolOpt, Conversion boolConversion, UnaryOperatorSignature boolOperator)
            {
                // If a user-defined comparison operator is present, then the operator kind must be user-defined
                Debug.Assert(Kind.IsUserDefined() || (object)MethodSymbolOpt == null);

                // If the return type of methodSymbolOpt is not bool, then there must be a boolConversion or boolOperator
                Debug.Assert(BoolConversion != default || !Kind.IsDynamic() || (Kind.IsUserDefined() && MethodSymbolOpt.ReturnType.SpecialType == SpecialType.System_Boolean));
                Debug.Assert((object)BoolOperator != null || !Kind.IsDynamic() || (Kind.IsUserDefined() && MethodSymbolOpt.ReturnType.SpecialType == SpecialType.System_Boolean));

                LeftConvertedType = leftConvertedType;
                RightConvertedType = rightConvertedType;
                Kind = kind;
                MethodSymbolOpt = methodSymbolOpt;
                BoolConversion = boolConversion;
                BoolOperator = boolOperator;
            }

            internal override TypeSymbol GetConvertedType(bool isRight)
                => isRight ? RightConvertedType : LeftConvertedType;

            internal override bool IsSingle()
                => true;

            public override string ToString()
                => $"binaryOperatorKind: {Kind}";

#if DEBUG
            internal override TreeDumperNode DumpCore()
            {
                var sub = new List<TreeDumperNode>();
                if ((object)MethodSymbolOpt != null)
                {
                    sub.Add(new TreeDumperNode("methodSymbolOpt", MethodSymbolOpt.ToDisplayString(), null));
                }
                sub.Add(new TreeDumperNode("leftConversion", LeftConvertedType.ToDisplayString(), null));
                sub.Add(new TreeDumperNode("rightConversion", RightConvertedType.ToDisplayString(), null));

                return new TreeDumperNode("nested", Kind, sub);
            }
#endif
        }

        internal class Nested : TupleBinaryOperatorInfo
        {
            internal ImmutableArray<TupleBinaryOperatorInfo> NestedOperators { get; }
            internal TypeSymbol LeftConvertedType { get; }
            internal TypeSymbol RightConvertedType { get; }

            internal Nested(ImmutableArray<TupleBinaryOperatorInfo> nestedOperators,
                TypeSymbol leftConvertedType, TypeSymbol rightConvertedType)
            {
                Debug.Assert(!nestedOperators.IsDefaultOrEmpty);
                NestedOperators = nestedOperators;
                LeftConvertedType = leftConvertedType;
                RightConvertedType = rightConvertedType;
            }

            internal override TypeSymbol GetConvertedType(bool isRight)
                => isRight ? RightConvertedType : LeftConvertedType;

            internal override bool IsSingle()
                => false;

#if DEBUG
            internal override TreeDumperNode DumpCore()
            {
                var sub = new List<TreeDumperNode>();
                sub.Add(new TreeDumperNode($"nestedOperators[{NestedOperators.Length}]", null,
                    NestedOperators.SelectAsArray(c => c.DumpCore())));

                return new TreeDumperNode("nested", null, sub);
            }
#endif
        }
    }
}
