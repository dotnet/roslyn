// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class LocalRewriter
    {
        public override BoundNode VisitConversion(BoundConversion node)
        {
            var rewrittenType = VisitType(node.Type);
            if (node.ConversionKind == ConversionKind.InterpolatedString)
            {
                return RewriteInterpolatedStringConversion(node);
            }

            bool wasInExpressionLambda = _inExpressionLambda;
            _inExpressionLambda = _inExpressionLambda || (node.ConversionKind == ConversionKind.AnonymousFunction && !wasInExpressionLambda && rewrittenType.IsExpressionTree());
            var rewrittenOperand = VisitExpression(node.Operand);
            _inExpressionLambda = wasInExpressionLambda;

            var result = MakeConversion(node, node.Syntax, rewrittenOperand, node.ConversionKind, node.SymbolOpt, node.Checked,
                node.ExplicitCastInCode, node.IsExtensionMethod, node.IsArrayIndex, node.ConstantValue, rewrittenType);

            var toType = node.Type;
            Debug.Assert(result.Type == toType);

            // 4.1.6 C# spec: To force a value of a floating point type to the exact precision of its type, an explicit cast can be used.
            // It means that explicit casts to (double) or (float) should be preserved on the node.
            // If original conversion has become something else with unknown precision, add an explicit identity cast.
            if (!_inExpressionLambda &&
                node.ExplicitCastInCode &&
                IsFloatPointExpressionOfUnknownPrecision(result))
            {
                result = MakeConversion(
                    node.Syntax,
                    result,
                    Conversion.Identity,
                    toType,
                    @checked: false,
                    explicitCastInCode: true,
                    constantValueOpt: result.ConstantValue);
            }

            return result;
        }

        private static bool IsFloatPointExpressionOfUnknownPrecision(BoundExpression rewrittenNode)
        {
            if (rewrittenNode == null)
            {
                return false;
            }

            if (rewrittenNode.ConstantValue != null)
            {
                return false;
            }

            var type = rewrittenNode.Type;
            if (type.SpecialType != SpecialType.System_Double && type.SpecialType != SpecialType.System_Single)
            {
                return false;
            }

            switch (rewrittenNode.Kind)
            {
                // ECMA-335   I.12.1.3 Handling of floating-point data types.
                //    ... the value might be retained in the internal representation
                //   for future use, if it is reloaded from the storage location without having been modified ...
                //
                // Unfortunately, the above means that precision is not guaranteed even when loading from storage.
                //
                //case BoundKind.FieldAccess:
                //case BoundKind.ArrayAccess:
                //  return true;

                case BoundKind.Sequence:
                    var sequence = (BoundSequence)rewrittenNode;
                    return IsFloatPointExpressionOfUnknownPrecision(sequence.Value);

                case BoundKind.Conversion:
                    // lowered conversions have definite precision unless they are implicit identity casts
                    var conversion = (BoundConversion)rewrittenNode;
                    return conversion.ConversionKind == ConversionKind.Identity && !conversion.ExplicitCastInCode;
            }

            // it is a float/double expression and we have no idea ...
            return true;
        }

        /// <summary>
        /// Helper method to generate a lowered conversion.
        /// </summary>
        private BoundExpression MakeConversion(
            BoundConversion oldNode,
            CSharpSyntaxNode syntax,
            BoundExpression rewrittenOperand,
            ConversionKind conversionKind,
            MethodSymbol symbolOpt,
            bool @checked,
            bool explicitCastInCode,
            bool isExtensionMethod,
            bool isArrayIndex,
            ConstantValue constantValueOpt,
            TypeSymbol rewrittenType)
        {
            Debug.Assert(oldNode == null || oldNode.Syntax == syntax);
            Debug.Assert((object)rewrittenType != null);
            @checked = @checked &&
                (_inExpressionLambda && (explicitCastInCode || DistinctSpecialTypes(rewrittenOperand.Type, rewrittenType)) ||
                NeedsChecked(rewrittenOperand.Type, rewrittenType));

            switch (conversionKind)
            {
                case ConversionKind.Identity:

                    // Spec 6.1.1:
                    //   An identity conversion converts from any type to the same type. 
                    //   This conversion exists such that an entity that already has a required type can be said to be convertible to that type.
                    //   Because object and dynamic are considered equivalent there is an identity conversion between object and dynamic, 
                    //   and between constructed types that are the same when replacing all occurrences of dynamic with object.

                    // Why ignoreDynamic: false?
                    // Lowering phase treats object and dynamic as equivalent types. So we don't need to produce any conversion here,
                    // but we need to change the Type property on the resulting BoundExpression to match the rewrittenType.
                    // This is necessary so that subsequent lowering transformations see that the expression is dynamic.

                    if (_inExpressionLambda || !rewrittenOperand.Type.Equals(rewrittenType, ignoreCustomModifiersAndArraySizesAndLowerBounds: false, ignoreDynamic: false))
                    {
                        break;
                    }

                    // 4.1.6 C# spec: To force a value of a floating point type to the exact precision of its type, an explicit cast can be used.
                    // If this is not an identity conversion of a float with unknown precision, strip away the identity conversion.
                    if (!(explicitCastInCode && IsFloatPointExpressionOfUnknownPrecision(rewrittenOperand)))
                    {
                        return rewrittenOperand;
                    }

                    break;

                case ConversionKind.ExplicitUserDefined:
                case ConversionKind.ImplicitUserDefined:
                    return RewriteUserDefinedConversion(
                        syntax: syntax,
                        rewrittenOperand: rewrittenOperand,
                        method: symbolOpt,
                        rewrittenType: rewrittenType,
                        conversionKind: conversionKind);

                case ConversionKind.IntPtr:
                    return RewriteIntPtrConversion(oldNode, syntax, rewrittenOperand, conversionKind, symbolOpt, @checked,
                        explicitCastInCode, isExtensionMethod, isArrayIndex, constantValueOpt, rewrittenType);

                case ConversionKind.ImplicitNullable:
                case ConversionKind.ExplicitNullable:
                    return RewriteNullableConversion(
                        syntax: syntax,
                        rewrittenOperand: rewrittenOperand,
                        conversionKind: conversionKind,
                        @checked: @checked,
                        explicitCastInCode: explicitCastInCode,
                        rewrittenType: rewrittenType);

                case ConversionKind.Boxing:

                    if (!_inExpressionLambda)
                    {
                        // We can perform some optimizations if we have a nullable value type
                        // as the operand and we know its nullability:

                        // * (object)new int?() is the same as (object)null
                        // * (object)new int?(123) is the same as (object)123

                        if (NullableNeverHasValue(rewrittenOperand))
                        {
                            return new BoundDefaultOperator(syntax, rewrittenType);
                        }

                        BoundExpression nullableValue = NullableAlwaysHasValue(rewrittenOperand);
                        if (nullableValue != null)
                        {
                            // Recurse, eliminating the unnecessary ctor.
                            return MakeConversion(oldNode, syntax, nullableValue, conversionKind,
                                symbolOpt, @checked, explicitCastInCode, isExtensionMethod, isArrayIndex,
                                constantValueOpt, rewrittenType);
                        }
                    }
                    break;

                case ConversionKind.NullLiteral:
                    if (!_inExpressionLambda || !explicitCastInCode)
                    {
                        return new BoundDefaultOperator(syntax, rewrittenType);
                    }

                    break;

                case ConversionKind.ImplicitReference:
                case ConversionKind.ExplicitReference:
                    if (rewrittenOperand.IsDefaultValue() && (!_inExpressionLambda || !explicitCastInCode))
                    {
                        return new BoundDefaultOperator(syntax, rewrittenType);
                    }

                    break;

                case ConversionKind.ImplicitNumeric:
                case ConversionKind.ExplicitNumeric:
                    if (rewrittenOperand.IsDefaultValue() && (!_inExpressionLambda || !explicitCastInCode))
                    {
                        return new BoundDefaultOperator(syntax, rewrittenType);
                    }

                    if (rewrittenType.SpecialType == SpecialType.System_Decimal || rewrittenOperand.Type.SpecialType == SpecialType.System_Decimal)
                    {
                        return RewriteDecimalConversion(oldNode, syntax, rewrittenOperand, rewrittenOperand.Type, rewrittenType);
                    }
                    break;

                case ConversionKind.ImplicitThrow:
                    {
                        // the operand must be a bound throw expression
                        var operand = (BoundThrowExpression)rewrittenOperand;
                        return _factory.ThrowExpression(operand.Expression, rewrittenType);
                    }

                case ConversionKind.ImplicitEnumeration:
                    // A conversion from constant zero to nullable is actually classified as an 
                    // implicit enumeration conversion, not an implicit nullable conversion. 
                    // Lower it to (E?)(E)0.
                    if (rewrittenType.IsNullableType())
                    {
                        var operand = MakeConversion(
                            oldNode,
                            syntax,
                            rewrittenOperand,
                            ConversionKind.ImplicitEnumeration,
                            symbolOpt,
                            @checked,
                            explicitCastInCode,
                            isExtensionMethod,
                            false,
                            constantValueOpt,
                            rewrittenType.GetNullableUnderlyingType());

                        return MakeConversion(
                            oldNode,
                            syntax,
                            operand,
                            ConversionKind.ImplicitNullable,
                            symbolOpt,
                            @checked,
                            explicitCastInCode,
                            isExtensionMethod,
                            isArrayIndex,
                            constantValueOpt,
                            rewrittenType);
                    }

                    goto case ConversionKind.ExplicitEnumeration;

                case ConversionKind.ExplicitEnumeration:
                    if (!rewrittenType.IsNullableType() &&
                        rewrittenOperand.IsDefaultValue() &&
                        (!_inExpressionLambda || !explicitCastInCode))
                    {
                        return new BoundDefaultOperator(syntax, rewrittenType);
                    }

                    if (rewrittenType.SpecialType == SpecialType.System_Decimal)
                    {
                        Debug.Assert(rewrittenOperand.Type.IsEnumType());
                        var underlyingTypeFrom = rewrittenOperand.Type.GetEnumUnderlyingType();
                        rewrittenOperand = MakeConversion(rewrittenOperand, underlyingTypeFrom, false);
                        return RewriteDecimalConversion(oldNode, syntax, rewrittenOperand, underlyingTypeFrom, rewrittenType);
                    }
                    else if (rewrittenOperand.Type.SpecialType == SpecialType.System_Decimal)
                    {
                        // This is where we handle conversion from Decimal to Enum: e.g., E e = (E) d;
                        // where 'e' is of type Enum E and 'd' is of type Decimal.
                        // Conversion can be simply done by applying its underlying numeric type to RewriteDecimalConversion(). 

                        Debug.Assert(rewrittenType.IsEnumType());
                        var underlyingTypeTo = rewrittenType.GetEnumUnderlyingType();
                        var rewrittenNode = RewriteDecimalConversion(oldNode, syntax, rewrittenOperand, rewrittenOperand.Type, underlyingTypeTo);

                        // However, the type of the rewritten node becomes underlying numeric type, not Enum type,
                        // which violates the overall constraint saying the type cannot be changed during rewriting (see LocalRewriter.cs).

                        // Instead of loosening this constraint, we return BoundConversion from underlying numeric type to Enum type,
                        // which will be eliminated during emitting (see EmitEnumConversion): e.g., E e = (E)(int) d;

                        return new BoundConversion(
                            syntax,
                            rewrittenNode,
                            conversionKind,
                            LookupResultKind.Viable,
                            isBaseConversion: false,
                            symbolOpt: symbolOpt,
                            @checked: false,
                            explicitCastInCode: explicitCastInCode,
                            isExtensionMethod: isExtensionMethod,
                            isArrayIndex: false,
                            constantValueOpt: constantValueOpt,
                            type: rewrittenType);
                    }

                    break;

                case ConversionKind.ImplicitDynamic:
                case ConversionKind.ExplicitDynamic:
                    Debug.Assert((object)symbolOpt == null);
                    Debug.Assert(!isExtensionMethod);
                    Debug.Assert(constantValueOpt == null);
                    return _dynamicFactory.MakeDynamicConversion(rewrittenOperand, explicitCastInCode || conversionKind == ConversionKind.ExplicitDynamic, isArrayIndex, @checked, rewrittenType).ToExpression();

                default:
                    break;
            }

            return oldNode != null ?
                oldNode.Update(
                    rewrittenOperand,
                    conversionKind,
                    oldNode.ResultKind,
                    isBaseConversion: oldNode.IsBaseConversion,
                    symbolOpt: symbolOpt,
                    @checked: @checked,
                    explicitCastInCode: explicitCastInCode,
                    isExtensionMethod: isExtensionMethod,
                    isArrayIndex: isArrayIndex,
                    constantValueOpt: constantValueOpt,
                    type: rewrittenType) :
                new BoundConversion(
                    syntax,
                    rewrittenOperand,
                    conversionKind,
                    LookupResultKind.Viable,
                    isBaseConversion: false,
                    symbolOpt: symbolOpt,
                    @checked: @checked,
                    explicitCastInCode: explicitCastInCode,
                    isExtensionMethod: isExtensionMethod,
                    isArrayIndex: isArrayIndex,
                    constantValueOpt: constantValueOpt,
                    type: rewrittenType);
        }

        private static bool DistinctSpecialTypes(TypeSymbol source, TypeSymbol target)
        {
            Debug.Assert((object)target != null);

            if ((object)source == null)
            {
                return false;
            }

            SpecialType sourceST = source.StrippedType().EnumUnderlyingType().SpecialType;
            SpecialType targetST = target.StrippedType().EnumUnderlyingType().SpecialType;
            return sourceST != targetST;
        }

        private const bool y = true;
        private const bool n = false;

        private static readonly bool[,] s_needsChecked =
            {   //         chri08u08i16u16i32u32i64u64
            /* chr */
                          { n, y, y, y, n, n, n, n, n },
            /* i08 */
                          { y, n, y, n, y, n, y, n, y },
            /* u08 */
                          { n, y, n, n, n, n, n, n, n },
            /* i16 */
                          { y, y, y, n, y, n, y, n, y },
            /* u16 */
                          { n, y, y, y, n, n, n, n, n },
            /* i32 */
                          { y, y, y, y, y, n, y, n, y },
            /* u32 */
                          { y, y, y, y, y, y, n, n, n },
            /* i64 */
                          { y, y, y, y, y, y, y, n, y },
            /* u64 */
                          { y, y, y, y, y, y, y, y, n },
            /* dec */
                          { n, n, n, n, n, n, n, n, n },
            /* r32 */
                          { y, y, y, y, y, y, y, y, y },
            /* r64 */
                          { y, y, y, y, y, y, y, y, y },
            };

        // Determine if the conversion can actually overflow at runtime.  If not, no need to generate a checked instruction.
        private static bool NeedsChecked(TypeSymbol source, TypeSymbol target)
        {
            Debug.Assert((object)target != null);

            if ((object)source == null)
            {
                return false;
            }

            if (source.IsDynamic())
            {
                return true;
            }

            SpecialType sourceST = source.StrippedType().EnumUnderlyingType().SpecialType;
            SpecialType targetST = target.StrippedType().EnumUnderlyingType().SpecialType;

            // integral to double or float is never checked, but float/double to integral 
            // may be checked.
            bool sourceIsNumeric = SpecialType.System_Char <= sourceST && sourceST <= SpecialType.System_Double;
            bool targetIsNumeric = SpecialType.System_Char <= targetST && targetST <= SpecialType.System_UInt64;
            return
                sourceIsNumeric && target.IsPointerType() ||
                targetIsNumeric && source.IsPointerType() ||
                sourceIsNumeric && targetIsNumeric && s_needsChecked[sourceST - SpecialType.System_Char, targetST - SpecialType.System_Char];
        }

        /// <summary>
        /// Helper method to generate a lowered conversion from the given <paramref name="rewrittenOperand"/> to the given <paramref name="rewrittenType"/>.
        /// </summary>
        /// <remarks>
        /// If we're converting a default parameter value to the parameter type, then the conversion can actually fail
        /// (e.g. if the default value was specified by an attribute and was, therefore, not checked by the compiler).
        /// Set acceptFailingConversion if you want to see default(rewrittenType) in such cases.
        /// The error will be suppressed only for conversions from <see cref="decimal"/> or <see cref="DateTime"/>.
        /// </remarks>
        private BoundExpression MakeConversion(BoundExpression rewrittenOperand, TypeSymbol rewrittenType, bool @checked, bool acceptFailingConversion = false)
        {
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            Conversion conversion = _compilation.Conversions.ClassifyConversion(rewrittenOperand.Type, rewrittenType, ref useSiteDiagnostics);
            _diagnostics.Add(rewrittenOperand.Syntax, useSiteDiagnostics);

            if (!conversion.IsValid)
            {
                if (!acceptFailingConversion ||
                     rewrittenOperand.Type.SpecialType != SpecialType.System_Decimal &&
                     rewrittenOperand.Type.SpecialType != SpecialType.System_DateTime)
                {
                    // error CS0029: Cannot implicitly convert type '{0}' to '{1}'
                    _diagnostics.Add(
                        ErrorCode.ERR_NoImplicitConv,
                        rewrittenOperand.Syntax.Location,
                        rewrittenOperand.Type,
                        rewrittenType);
                }

                return _factory.NullOrDefault(rewrittenType);
            }

            return MakeConversion(rewrittenOperand.Syntax, rewrittenOperand, conversion, rewrittenType, @checked);
        }

        /// <summary>
        /// Helper method to generate a lowered conversion from the given <paramref name="rewrittenOperand"/> to the given <paramref name="rewrittenType"/>.
        /// </summary>
        /// <remarks>
        /// If we're converting a default parameter value to the parameter type, then the conversion can actually fail
        /// (e.g. if the default value was specified by an attribute and was, therefore, not checked by the compiler).
        /// Set acceptFailingConversion if you want to see default(rewrittenType) in such cases.
        /// The error will be suppressed only for conversions from <see cref="decimal"/> or <see cref="DateTime"/>.
        /// </remarks>
        private BoundExpression MakeImplicitConversion(BoundExpression rewrittenOperand, TypeSymbol rewrittenType)
        {
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            Conversion conversion = _compilation.Conversions.ClassifyConversion(rewrittenOperand.Type, rewrittenType, ref useSiteDiagnostics);
            _diagnostics.Add(rewrittenOperand.Syntax, useSiteDiagnostics);
            if (!conversion.IsImplicit)
            {
                // error CS0029: Cannot implicitly convert type '{0}' to '{1}'
                _diagnostics.Add(
                    ErrorCode.ERR_NoImplicitConv,
                    rewrittenOperand.Syntax.Location,
                    rewrittenOperand.Type,
                    rewrittenType);

                return _factory.NullOrDefault(rewrittenType);
            }

            return MakeConversion(rewrittenOperand.Syntax, rewrittenOperand, conversion, rewrittenType, @checked: false);
        }

        /// <summary>
        /// Helper method to generate a lowered conversion from the given rewrittenOperand to the given rewrittenType with the given conversion kind.
        /// </summary>
        /// <remarks>
        /// Conversion kind must not be a user defined conversion, use the other overload which takes a 'Conversion' parameter for generating synthesized user defined conversions.
        /// </remarks>
        private BoundExpression MakeConversion(
            CSharpSyntaxNode syntax,
            BoundExpression rewrittenOperand,
            ConversionKind conversionKind,
            TypeSymbol rewrittenType,
            bool @checked,
            ConstantValue constantValueOpt = null)
        {
            // We need more information than just the conversion kind for creating a synthesized user defined conversion.
            Debug.Assert(!conversionKind.IsUserDefinedConversion(), "Use the other overload which takes a 'Conversion' parameter for generating synthesized user defined conversions.");
            return MakeConversion(syntax, rewrittenOperand, new Conversion(conversionKind), rewrittenType, @checked, explicitCastInCode: false, constantValueOpt: constantValueOpt);
        }

        private BoundExpression MakeConversion(
            CSharpSyntaxNode syntax,
            BoundExpression rewrittenOperand,
            Conversion conversion,
            TypeSymbol rewrittenType,
            bool @checked,
            bool explicitCastInCode = false,
            ConstantValue constantValueOpt = null)
        {
            Debug.Assert(conversion.IsValid);

            // Typically by the time we get here, a user-defined conversion has been realized as a sequence of
            // conversions that convert to the exact parameter type of the user-defined conversion,
            // and then from the exact return type of the user-defined conversion to the desired type.
            // However, it is possible that we have cached a conversion (for example, to be used
            // in an increment or decrement operator) and are only just realizing it now.
            //
            // Due to an oddity in the way we create a non-lifted user-defined conversion from A to D? 
            // (required backwards compatibility with the native compiler) we can end up in a situation 
            // where we have:
            //
            // a standard conversion from A to B?
            // then a standard conversion from B? to B
            // then a user-defined  conversion from B to C
            // then a standard conversion from C to C? 
            // then a standard conversion from C? to D?
            //
            // In that scenario, the "from type" of the conversion will be B? and the "from conversion" will be 
            // from A to B?. Similarly the "to type" of the conversion will be C? and the "to conversion"
            // of the conversion will be from C? to D?. We still need to induce the conversions from B? to B
            // and from C to C?.

            if (conversion.Kind.IsUserDefinedConversion())
            {
                if (rewrittenOperand.Type != conversion.BestUserDefinedConversionAnalysis.FromType)
                {
                    rewrittenOperand = MakeConversion(
                        syntax,
                        rewrittenOperand,
                        conversion.UserDefinedFromConversion,
                        conversion.BestUserDefinedConversionAnalysis.FromType,
                        @checked);
                }

                if (rewrittenOperand.Type != conversion.Method.ParameterTypes[0])
                {
                    rewrittenOperand = MakeConversion(
                        rewrittenOperand,
                        conversion.BestUserDefinedConversionAnalysis.FromType,
                        @checked);
                }

                TypeSymbol userDefinedConversionRewrittenType = conversion.Method.ReturnType;

                if (rewrittenOperand.Type != conversion.Method.ParameterTypes[0])
                {
                    Debug.Assert(rewrittenOperand.Type.IsNullableType());
                    Debug.Assert(rewrittenOperand.Type.GetNullableUnderlyingType() == conversion.Method.ParameterTypes[0]);
                    Debug.Assert(!userDefinedConversionRewrittenType.IsNullableType());

                    // Lifted conversion, wrap return type in Nullable
                    userDefinedConversionRewrittenType = ((NamedTypeSymbol)rewrittenOperand.Type.OriginalDefinition).Construct(userDefinedConversionRewrittenType);
                }

                BoundExpression userDefined = RewriteUserDefinedConversion(
                    syntax,
                    rewrittenOperand,
                    conversion.Method,
                    userDefinedConversionRewrittenType,
                    conversion.Kind);

                if (userDefined.Type != conversion.BestUserDefinedConversionAnalysis.ToType)
                {
                    userDefined = MakeConversion(
                        userDefined,
                        conversion.BestUserDefinedConversionAnalysis.ToType,
                        @checked);
                }

                if (userDefined.Type != rewrittenType)
                {
                    userDefined = MakeConversion(
                        syntax,
                        userDefined,
                        conversion.UserDefinedToConversion,
                        rewrittenType,
                        @checked);
                }

                return userDefined;
            }

            return MakeConversion(
                oldNode: null,
                syntax: syntax,
                rewrittenOperand: rewrittenOperand,
                conversionKind: conversion.Kind,
                symbolOpt: conversion.Method,
                @checked: @checked,
                explicitCastInCode: explicitCastInCode,
                isExtensionMethod: false,
                isArrayIndex: false,
                constantValueOpt: constantValueOpt,
                rewrittenType: rewrittenType);
        }

        private BoundExpression RewriteNullableConversion(CSharpSyntaxNode syntaxNode, BoundExpression rewrittenOperand, TypeSymbol rewrittenType)
        {
            return RewriteNullableConversion(syntaxNode, rewrittenOperand, ConversionKind.ExplicitNullable, false, false, rewrittenType);
        }

        private static bool NullableNeverHasValue(BoundExpression expression)
        {
            // CONSIDER: A sequence of side effects with an always-null expression as its value
            // CONSIDER: can be optimized also. Should we?

            if (expression.IsLiteralNull())
            {
                return true;
            }

            if (!expression.Type.IsNullableType())
            {
                return false;
            }

            // default(int?) never has a value.
            if (expression.Kind == BoundKind.DefaultOperator)
            {
                return true;
            }

            // new int?() never has a value if there is no argument.
            if (expression.Kind == BoundKind.ObjectCreationExpression)
            {
                if (((BoundObjectCreationExpression)expression).Arguments.Length == 0)
                {
                    return true;
                }
            }

            return false;
        }

        // If the nullable expression always has a value, returns the value, otherwise null.
        private static BoundExpression NullableAlwaysHasValue(BoundExpression expression)
        {
            if (expression.Type.IsNullableType() && expression.Kind == BoundKind.ObjectCreationExpression)
            {
                BoundObjectCreationExpression objectCreation = (BoundObjectCreationExpression)expression;
                if (objectCreation.Arguments.Length == 1)
                {
                    return objectCreation.Arguments[0];
                }
            }
            return null;
        }

        private BoundExpression RewriteNullableConversion(
            CSharpSyntaxNode syntax,
            BoundExpression rewrittenOperand,
            ConversionKind conversionKind,
            bool @checked,
            bool explicitCastInCode,
            TypeSymbol rewrittenType)
        {
            Debug.Assert((object)rewrittenType != null);

            if (_inExpressionLambda)
            {
                return RewriteLiftedConversionInExpressionTree(syntax, rewrittenOperand, conversionKind, @checked, explicitCastInCode, rewrittenType);
            }

            TypeSymbol rewrittenOperandType = rewrittenOperand.Type;
            Debug.Assert(rewrittenType.IsNullableType() || rewrittenOperandType.IsNullableType());

            if (rewrittenOperandType.IsNullableType() && rewrittenType.IsNullableType())
            {
                return RewriteFullyLiftedBuiltInConversion(syntax, rewrittenOperand, conversionKind, @checked, rewrittenType);
            }
            else if (rewrittenType.IsNullableType())
            {
                // SPEC: If the nullable conversion is from S to T?, the conversion is 
                // SPEC: evaluated as the underlying conversion from S to T followed
                // SPEC: by a wrapping from T to T?.

                BoundExpression rewrittenConversion = MakeConversion(rewrittenOperand, rewrittenType.GetNullableUnderlyingType(), @checked);
                MethodSymbol ctor = GetNullableMethod(syntax, rewrittenType, SpecialMember.System_Nullable_T__ctor);
                return new BoundObjectCreationExpression(syntax, ctor, rewrittenConversion);
            }
            else
            {
                // SPEC: if the nullable conversion is from S? to T, the conversion is
                // SPEC: evaluated as an unwrapping from S? to S followed by the underlying
                // SPEC: conversion from S to T.

                // We can do a simple optimization here if we know that the source is never null:

                BoundExpression nonNullValue = NullableAlwaysHasValue(rewrittenOperand);
                if (nonNullValue != null)
                {
                    return MakeConversion(nonNullValue, rewrittenType, @checked);
                }

                // (If the source is known to be null then we need to keep the call to get Value 
                // in place so that it throws at runtime.)

                MethodSymbol get_Value = GetNullableMethod(syntax, rewrittenOperandType, SpecialMember.System_Nullable_T_get_Value);
                return MakeConversion(BoundCall.Synthesized(syntax, rewrittenOperand, get_Value), rewrittenType, @checked);
            }
        }

        private BoundExpression RewriteLiftedConversionInExpressionTree(
            CSharpSyntaxNode syntax,
            BoundExpression rewrittenOperand,
            ConversionKind conversionKind,
            bool @checked,
            bool explicitCastInCode,
            TypeSymbol rewrittenType)
        {
            Debug.Assert((object)rewrittenType != null);
            TypeSymbol rewrittenOperandType = rewrittenOperand.Type;
            Debug.Assert(rewrittenType.IsNullableType() || rewrittenOperandType.IsNullableType());

            TypeSymbol typeFrom = rewrittenOperandType.StrippedType();
            TypeSymbol typeTo = rewrittenType.StrippedType();
            if (typeFrom != typeTo && (typeFrom.SpecialType == SpecialType.System_Decimal || typeTo.SpecialType == SpecialType.System_Decimal))
            {
                // take special care if the underlying conversion is a decimal conversion
                TypeSymbol typeFromUnderlying = typeFrom;
                TypeSymbol typeToUnderlying = typeTo;

                // They can't both be enums, since one of them is decimal.
                if (typeFrom.IsEnumType())
                {
                    typeFromUnderlying = typeFrom.GetEnumUnderlyingType();

                    // NOTE: Dev10 converts enum? to underlying?, rather than directly to underlying.
                    rewrittenOperandType = rewrittenOperandType.IsNullableType() ? ((NamedTypeSymbol)rewrittenOperandType.OriginalDefinition).Construct(typeFromUnderlying) : typeFromUnderlying;
                    rewrittenOperand = BoundConversion.SynthesizedNonUserDefined(syntax, rewrittenOperand, ConversionKind.ImplicitEnumeration, rewrittenOperandType);
                }
                else if (typeTo.IsEnumType())
                {
                    typeToUnderlying = typeTo.GetEnumUnderlyingType();
                }

                var method = (MethodSymbol)_compilation.Assembly.GetSpecialTypeMember(DecimalConversionMethod(typeFromUnderlying, typeToUnderlying));
                conversionKind = conversionKind.IsImplicitConversion() ? ConversionKind.ImplicitUserDefined : ConversionKind.ExplicitUserDefined;
                var result = new BoundConversion(syntax, rewrittenOperand, new Conversion(conversionKind, method, false), @checked, explicitCastInCode, default(ConstantValue), rewrittenType);
                return result;
            }
            else
            {
                return new BoundConversion(syntax, rewrittenOperand, new Conversion(conversionKind), @checked, explicitCastInCode, default(ConstantValue), rewrittenType);
            }
        }

        private BoundExpression RewriteFullyLiftedBuiltInConversion(
            CSharpSyntaxNode syntax,
            BoundExpression operand,
            ConversionKind kind,
            bool @checked,
            TypeSymbol type)
        {
            // SPEC: If the nullable conversion is from S? to T?:
            // SPEC: * If the source HasValue property is false the result
            // SPEC:   is a null value of type T?.
            // SPEC: * Otherwise the conversion is evaluated as an unwrapping
            // SPEC:   from S? to S, followed by the underlying conversion from
            // SPEC:   S to T, followed by a wrapping from T to T?

            BoundExpression optimized = OptimizeLiftedBuiltInConversion(syntax, operand, kind, @checked, type);
            if (optimized != null)
            {
                return optimized;
            }

            // We are unable to optimize the conversion. "(T?)s" is generated as:
            // S? temp = s;
            // temp.HasValue ? new T?((T)temp.GetValueOrDefault()) : default(T?)

            BoundAssignmentOperator tempAssignment;
            var boundTemp = _factory.StoreToTemp(operand, out tempAssignment);
            MethodSymbol getValueOrDefault = GetNullableMethod(syntax, boundTemp.Type, SpecialMember.System_Nullable_T_GetValueOrDefault);
            BoundExpression condition = MakeNullableHasValue(syntax, boundTemp);
            BoundExpression consequence = new BoundObjectCreationExpression(
                syntax,
                GetNullableMethod(syntax, type, SpecialMember.System_Nullable_T__ctor),
                MakeConversion(
                    BoundCall.Synthesized(syntax, boundTemp, getValueOrDefault),
                    type.GetNullableUnderlyingType(),
                    @checked));
            BoundExpression alternative = new BoundDefaultOperator(syntax, null, type);
            BoundExpression conditionalExpression = RewriteConditionalOperator(
                syntax: syntax,
                rewrittenCondition: condition,
                rewrittenConsequence: consequence,
                rewrittenAlternative: alternative,
                constantValueOpt: null,
                rewrittenType: type);

            return new BoundSequence(
                syntax: syntax,
                locals: ImmutableArray.Create(boundTemp.LocalSymbol),
                sideEffects: ImmutableArray.Create<BoundExpression>(tempAssignment),
                value: conditionalExpression,
                type: type);
        }

        private BoundExpression OptimizeLiftedUserDefinedConversion(
            CSharpSyntaxNode syntax,
            BoundExpression operand,
            ConversionKind kind,
            MethodSymbol method,
            TypeSymbol type)
        {
            // We begin with some optimizations: if the converted expression is known to always be null
            // then we can skip the whole thing and simply return the alternative:

            if (NullableNeverHasValue(operand))
            {
                return new BoundDefaultOperator(syntax, type);
            }

            // If the converted expression is known to never be null then we can return 
            // new R?(op_Whatever(nonNullableValue))
            BoundExpression nonNullValue = NullableAlwaysHasValue(operand);
            if (nonNullValue != null)
            {
                return MakeLiftedUserDefinedConversionConsequence(BoundCall.Synthesized(syntax, null, method, nonNullValue), type);
            }

            return DistributeLiftedConversionIntoLiftedOperand(syntax, operand, kind, false, method, type);
        }

        private BoundExpression OptimizeLiftedBuiltInConversion(
            CSharpSyntaxNode syntax,
            BoundExpression operand,
            ConversionKind kind,
            bool @checked,
            TypeSymbol type)
        {
            Debug.Assert(operand != null);
            Debug.Assert((object)type != null);

            // First, an optimization. If the source is known to always be null then
            // we can simply return the alternative.

            if (NullableNeverHasValue(operand))
            {
                return new BoundDefaultOperator(syntax, null, type);
            }

            // Second, a trickier optimization. If the conversion is "(T?)(new S?(x))" then
            // we generate "new T?((T)x)"

            BoundExpression nonNullValue = NullableAlwaysHasValue(operand);
            if (nonNullValue != null)
            {
                return new BoundObjectCreationExpression(
                    syntax,
                    GetNullableMethod(syntax, type, SpecialMember.System_Nullable_T__ctor),
                    MakeConversion(
                        nonNullValue,
                        type.GetNullableUnderlyingType(),
                        @checked));
            }

            // Third, a very tricky optimization.
            return DistributeLiftedConversionIntoLiftedOperand(syntax, operand, kind, @checked, null, type);
        }

        private BoundExpression DistributeLiftedConversionIntoLiftedOperand(
            CSharpSyntaxNode syntax,
            BoundExpression operand,
            ConversionKind kind,
            bool @checked,
            MethodSymbol method,
            TypeSymbol type)
        {
            // Third, an even trickier optimization. Suppose we have a lifted conversion on top of
            // a lifted operation. Say, "decimal? d = M() + N()" where M() and N() return nullable ints.
            // We can codegen this naively as:
            //
            // int? m = M();
            // int? n = N();
            // int? r = m.HasValue && n.HasValue ? new int?(m.Value + n.Value) : new int?();
            // decimal? d = r.HasValue ? new decimal?((decimal)r.Value) : new decimal?();
            //
            // However, we also observe that we could do the conversion on both branches of the conditional:
            //
            // int? m = M();
            // int? n = N();
            // decimal? d = m.HasValue && n.HasValue ? (decimal?)(new int?(m.Value + n.Value)) : (decimal?)(new int?());
            //
            // And we already optimize those, above! So we could reduce this to:
            //
            // int? m = M();
            // int? n = N();
            // decimal? d = m.HasValue && n.HasValue ? new decimal?((decimal)(m.Value + n.Value)) : new decimal?());
            //
            // which avoids entirely the creation of the unnecessary nullable int!

            if (operand.Kind == BoundKind.Sequence)
            {
                BoundSequence seq = (BoundSequence)operand;
                if (seq.Value.Kind == BoundKind.ConditionalOperator)
                {
                    BoundConditionalOperator conditional = (BoundConditionalOperator)seq.Value;
                    Debug.Assert(seq.Type == conditional.Type);
                    Debug.Assert(conditional.Type == conditional.Consequence.Type);
                    Debug.Assert(conditional.Type == conditional.Alternative.Type);

                    if (NullableAlwaysHasValue(conditional.Consequence) != null && NullableNeverHasValue(conditional.Alternative))
                    {
                        return new BoundSequence(
                            seq.Syntax,
                            seq.Locals,
                            seq.SideEffects,
                            RewriteConditionalOperator(
                                conditional.Syntax,
                                conditional.Condition,
                                MakeConversion(null, syntax, conditional.Consequence, kind, method, @checked, false, false, false, ConstantValue.NotAvailable, type),
                                MakeConversion(null, syntax, conditional.Alternative, kind, method, @checked, false, false, false, ConstantValue.NotAvailable, type),
                                ConstantValue.NotAvailable,
                                type),
                            type);
                    }
                }
            }

            return null;
        }

        private BoundExpression RewriteUserDefinedConversion(
            CSharpSyntaxNode syntax,
            BoundExpression rewrittenOperand,
            MethodSymbol method,
            TypeSymbol rewrittenType,
            ConversionKind conversionKind)
        {
            Debug.Assert((object)method != null && !method.ReturnsVoid && method.ParameterCount == 1);
            if (rewrittenOperand.Type != method.ParameterTypes[0])
            {
                Debug.Assert(rewrittenOperand.Type.IsNullableType());
                Debug.Assert(rewrittenOperand.Type.GetNullableUnderlyingType() == method.ParameterTypes[0]);
                return RewriteLiftedUserDefinedConversion(syntax, rewrittenOperand, method, rewrittenType, conversionKind);
            }

            var conv = new Conversion(ConversionKind.ExplicitUserDefined, method, false);
            Debug.Assert(conv.IsValid);
            BoundExpression result =
                _inExpressionLambda
                ? BoundConversion.Synthesized(syntax, rewrittenOperand, MakeConversion(syntax, conv, rewrittenOperand.Type, rewrittenType), false, true, default(ConstantValue), rewrittenType)
                : (BoundExpression)BoundCall.Synthesized(syntax, null, method, rewrittenOperand);
            Debug.Assert(result.Type == rewrittenType);
            return result;
        }

        private BoundExpression MakeLiftedUserDefinedConversionConsequence(BoundCall call, TypeSymbol resultType)
        {
            if (call.Method.ReturnType.IsNonNullableValueType())
            {
                Debug.Assert(resultType.IsNullableType() && resultType.GetNullableUnderlyingType() == call.Method.ReturnType);
                MethodSymbol ctor = GetNullableMethod(call.Syntax, resultType, SpecialMember.System_Nullable_T__ctor);
                return new BoundObjectCreationExpression(call.Syntax, ctor, call);
            }

            return call;
        }

        private BoundExpression RewriteLiftedUserDefinedConversion(
            CSharpSyntaxNode syntax,
            BoundExpression rewrittenOperand,
            MethodSymbol method,
            TypeSymbol rewrittenType,
            ConversionKind conversionKind)
        {
            if (_inExpressionLambda)
            {
                Conversion conv = MakeConversion(syntax, new Conversion(conversionKind, method, false), rewrittenOperand.Type, rewrittenType);
                return BoundConversion.Synthesized(syntax, rewrittenOperand, conv, false, true, default(ConstantValue), rewrittenType);
            }

            // DELIBERATE SPEC VIOLATION: 
            // The native compiler allows for a "lifted" conversion even when the return type of the conversion
            // not a non-nullable value type. For example, if we have a conversion from struct S to string,
            // then a "lifted" conversion from S? to string is considered by the native compiler to exist,
            // with the semantics of "s.HasValue ? (string)s.Value : (string)null".  The Roslyn compiler
            // perpetuates this error for the sake of backwards compatibility.

            Debug.Assert((object)rewrittenType != null);
            Debug.Assert(rewrittenOperand.Type.IsNullableType());

            BoundExpression optimized = OptimizeLiftedUserDefinedConversion(syntax, rewrittenOperand, conversionKind, method, rewrittenType);
            if (optimized != null)
            {
                return optimized;
            }

            // We have no optimizations we can perform. If the return type of the 
            // conversion method is a non-nullable value type R then we lower this as:
            //
            // temp = operand
            // temp.HasValue ? new R?(op_Whatever(temp.GetValueOrDefault())) : default(R?)
            //
            // Otherwise, if the return type of the conversion is a nullable value type, reference type
            // or pointer type P, then we lower this as:
            //
            // temp = operand
            // temp.HasValue ? op_Whatever(temp.GetValueOrDefault()) : default(P)

            BoundAssignmentOperator tempAssignment;
            BoundLocal boundTemp = _factory.StoreToTemp(rewrittenOperand, out tempAssignment);
            MethodSymbol getValueOrDefault = GetNullableMethod(syntax, boundTemp.Type, SpecialMember.System_Nullable_T_GetValueOrDefault);

            // temp.HasValue
            BoundExpression condition = MakeNullableHasValue(syntax, boundTemp);

            // temp.GetValueOrDefault()
            BoundCall callGetValueOrDefault = BoundCall.Synthesized(syntax, boundTemp, getValueOrDefault);

            // op_Whatever(temp.GetValueOrDefault())
            BoundCall userDefinedCall = BoundCall.Synthesized(syntax, null, method, callGetValueOrDefault);

            // new R?(op_Whatever(temp.GetValueOrDefault())
            BoundExpression consequence = MakeLiftedUserDefinedConversionConsequence(userDefinedCall, rewrittenType);

            // default(R?)
            BoundExpression alternative = new BoundDefaultOperator(syntax, rewrittenType);

            // temp.HasValue ? new R?(op_Whatever(temp.GetValueOrDefault())) : default(R?)
            BoundExpression conditionalExpression = RewriteConditionalOperator(
                syntax: syntax,
                rewrittenCondition: condition,
                rewrittenConsequence: consequence,
                rewrittenAlternative: alternative,
                constantValueOpt: null,
                rewrittenType: rewrittenType);

            // temp = operand
            // temp.HasValue ? new R?(op_Whatever(temp.GetValueOrDefault())) : default(R?)
            return new BoundSequence(
                syntax: syntax,
                locals: ImmutableArray.Create(boundTemp.LocalSymbol),
                sideEffects: ImmutableArray.Create<BoundExpression>(tempAssignment),
                value: conditionalExpression,
                type: rewrittenType);
        }

        private BoundExpression RewriteIntPtrConversion(
            BoundConversion oldNode,
            CSharpSyntaxNode syntax,
            BoundExpression rewrittenOperand,
            ConversionKind conversionKind,
            MethodSymbol symbolOpt,
            bool @checked,
            bool explicitCastInCode,
            bool isExtensionMethod,
            bool isArrayIndex,
            ConstantValue constantValueOpt,
            TypeSymbol rewrittenType)
        {
            Debug.Assert(rewrittenOperand != null);
            Debug.Assert((object)rewrittenType != null);

            TypeSymbol source = rewrittenOperand.Type;
            TypeSymbol target = rewrittenType;

            SpecialMember member = GetIntPtrConversionMethod(source: rewrittenOperand.Type, target: rewrittenType);
            MethodSymbol method = GetSpecialTypeMethod(syntax, member);
            Debug.Assert(!method.ReturnsVoid);
            Debug.Assert(method.ParameterCount == 1);

            if (source.IsNullableType() && target.IsNullableType())
            {
                Debug.Assert(target.IsNullableType());
                return RewriteLiftedUserDefinedConversion(syntax, rewrittenOperand, method, rewrittenType, ConversionKind.IntPtr);
            }
            else if (source.IsNullableType())
            {
                rewrittenOperand = MakeConversion(rewrittenOperand, source.StrippedType(), @checked);
            }

            rewrittenOperand = MakeConversion(rewrittenOperand, method.ParameterTypes[0], @checked);

            var returnType = method.ReturnType;
            Debug.Assert((object)returnType != null);

            if (_inExpressionLambda)
            {
                return BoundConversion.Synthesized(syntax, rewrittenOperand, new Conversion(conversionKind, method, false), @checked, explicitCastInCode, constantValueOpt, rewrittenType);
            }

            var rewrittenCall = MakeCall(
                    syntax: syntax,
                    rewrittenReceiver: null,
                    method: method,
                    rewrittenArguments: ImmutableArray.Create(rewrittenOperand),
                    type: returnType);

            return MakeConversion(rewrittenCall, rewrittenType, @checked);
        }

        public static SpecialMember GetIntPtrConversionMethod(TypeSymbol source, TypeSymbol target)
        {
            Debug.Assert((object)source != null);
            Debug.Assert((object)target != null);

            TypeSymbol t0 = target.StrippedType();
            TypeSymbol s0 = source.StrippedType();

            SpecialType t0Type = t0.IsEnumType() ? t0.GetEnumUnderlyingType().SpecialType : t0.SpecialType;
            SpecialType s0Type = s0.IsEnumType() ? s0.GetEnumUnderlyingType().SpecialType : s0.SpecialType;

            if (t0Type == SpecialType.System_IntPtr)
            {
                if (source.TypeKind == TypeKind.Pointer)
                {
                    return SpecialMember.System_IntPtr__op_Explicit_FromPointer;
                }

                switch (s0Type)
                {
                    case SpecialType.System_Byte:
                    case SpecialType.System_SByte:
                    case SpecialType.System_Int16:
                    case SpecialType.System_UInt16:
                    case SpecialType.System_Char:
                    case SpecialType.System_Int32:
                        return SpecialMember.System_IntPtr__op_Explicit_FromInt32;
                    case SpecialType.System_UInt32:
                    case SpecialType.System_UInt64:
                    case SpecialType.System_Int64:
                    case SpecialType.System_Single:
                    case SpecialType.System_Double:
                    case SpecialType.System_Decimal:
                        return SpecialMember.System_IntPtr__op_Explicit_FromInt64;
                }
            }
            else if (t0Type == SpecialType.System_UIntPtr)
            {
                if (source.TypeKind == TypeKind.Pointer)
                {
                    return SpecialMember.System_UIntPtr__op_Explicit_FromPointer;
                }

                switch (s0Type)
                {
                    case SpecialType.System_Byte:
                    case SpecialType.System_UInt16:
                    case SpecialType.System_Char:
                    case SpecialType.System_UInt32:
                        return SpecialMember.System_UIntPtr__op_Explicit_FromUInt32;
                    case SpecialType.System_SByte:
                    case SpecialType.System_Int16:
                    case SpecialType.System_Int32:
                    case SpecialType.System_UInt64:
                    case SpecialType.System_Int64:
                    case SpecialType.System_Single:
                    case SpecialType.System_Double:
                    case SpecialType.System_Decimal:
                        return SpecialMember.System_UIntPtr__op_Explicit_FromUInt64;
                }
            }
            else if (s0Type == SpecialType.System_IntPtr)
            {
                if (target.TypeKind == TypeKind.Pointer)
                {
                    return SpecialMember.System_IntPtr__op_Explicit_ToPointer;
                }

                switch (t0Type)
                {
                    case SpecialType.System_Byte:
                    case SpecialType.System_SByte:
                    case SpecialType.System_Int16:
                    case SpecialType.System_UInt16:
                    case SpecialType.System_Char:
                    case SpecialType.System_UInt32:
                    case SpecialType.System_Int32:
                        return SpecialMember.System_IntPtr__op_Explicit_ToInt32;
                    case SpecialType.System_UInt64:
                    case SpecialType.System_Int64:
                    case SpecialType.System_Single:
                    case SpecialType.System_Double:
                    case SpecialType.System_Decimal:
                        return SpecialMember.System_IntPtr__op_Explicit_ToInt64;
                }
            }
            else if (s0Type == SpecialType.System_UIntPtr)
            {
                if (target.TypeKind == TypeKind.Pointer)
                {
                    return SpecialMember.System_UIntPtr__op_Explicit_ToPointer;
                }

                switch (t0Type)
                {
                    case SpecialType.System_SByte:
                    case SpecialType.System_Int16:
                    case SpecialType.System_Int32:
                    case SpecialType.System_Byte:
                    case SpecialType.System_UInt16:
                    case SpecialType.System_Char:
                    case SpecialType.System_UInt32:
                        return SpecialMember.System_UIntPtr__op_Explicit_ToUInt32;
                    case SpecialType.System_UInt64:
                    case SpecialType.System_Int64:
                    case SpecialType.System_Single:
                    case SpecialType.System_Double:
                    case SpecialType.System_Decimal:
                        return SpecialMember.System_UIntPtr__op_Explicit_ToUInt64;
                }
            }

            throw ExceptionUtilities.Unreachable;
        }

        internal static SpecialMember DecimalConversionMethod(TypeSymbol typeFrom, TypeSymbol typeTo)
        {
            if (typeFrom.SpecialType == SpecialType.System_Decimal)
            {
                // Rewrite Decimal to Numeric
                switch (typeTo.SpecialType)
                {
                    case SpecialType.System_Char: return SpecialMember.System_Decimal__op_Explicit_ToChar;
                    case SpecialType.System_SByte: return SpecialMember.System_Decimal__op_Explicit_ToSByte;
                    case SpecialType.System_Byte: return SpecialMember.System_Decimal__op_Explicit_ToByte;
                    case SpecialType.System_Int16: return SpecialMember.System_Decimal__op_Explicit_ToInt16;
                    case SpecialType.System_UInt16: return SpecialMember.System_Decimal__op_Explicit_ToUInt16;
                    case SpecialType.System_Int32: return SpecialMember.System_Decimal__op_Explicit_ToInt32;
                    case SpecialType.System_UInt32: return SpecialMember.System_Decimal__op_Explicit_ToUInt32;
                    case SpecialType.System_Int64: return SpecialMember.System_Decimal__op_Explicit_ToInt64;
                    case SpecialType.System_UInt64: return SpecialMember.System_Decimal__op_Explicit_ToUInt64;
                    case SpecialType.System_Single: return SpecialMember.System_Decimal__op_Explicit_ToSingle;
                    case SpecialType.System_Double: return SpecialMember.System_Decimal__op_Explicit_ToDouble;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(typeTo.SpecialType);
                }
            }
            else
            {
                // Rewrite Numeric to Decimal
                switch (typeFrom.SpecialType)
                {
                    case SpecialType.System_Char: return SpecialMember.System_Decimal__op_Implicit_FromChar;
                    case SpecialType.System_SByte: return SpecialMember.System_Decimal__op_Implicit_FromSByte;
                    case SpecialType.System_Byte: return SpecialMember.System_Decimal__op_Implicit_FromByte;
                    case SpecialType.System_Int16: return SpecialMember.System_Decimal__op_Implicit_FromInt16;
                    case SpecialType.System_UInt16: return SpecialMember.System_Decimal__op_Implicit_FromUInt16;
                    case SpecialType.System_Int32: return SpecialMember.System_Decimal__op_Implicit_FromInt32;
                    case SpecialType.System_UInt32: return SpecialMember.System_Decimal__op_Implicit_FromUInt32;
                    case SpecialType.System_Int64: return SpecialMember.System_Decimal__op_Implicit_FromInt64;
                    case SpecialType.System_UInt64: return SpecialMember.System_Decimal__op_Implicit_FromUInt64;
                    case SpecialType.System_Single: return SpecialMember.System_Decimal__op_Explicit_FromSingle;
                    case SpecialType.System_Double: return SpecialMember.System_Decimal__op_Explicit_FromDouble;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(typeFrom.SpecialType);
                }
            }
        }

        private BoundExpression RewriteDecimalConversion(BoundConversion oldNode, CSharpSyntaxNode syntax, BoundExpression operand, TypeSymbol fromType, TypeSymbol toType)
        {
            Debug.Assert(fromType.SpecialType == SpecialType.System_Decimal || toType.SpecialType == SpecialType.System_Decimal);

            // call the method
            SpecialMember member = DecimalConversionMethod(fromType, toType);
            var method = (MethodSymbol)_compilation.Assembly.GetSpecialTypeMember(member);
            Debug.Assert((object)method != null); // Should have been checked during Warnings pass

            if (_inExpressionLambda && oldNode != null)
            {
                ConversionKind conversionKind = oldNode.ConversionKind.IsImplicitConversion() ? ConversionKind.ImplicitUserDefined : ConversionKind.ExplicitUserDefined;
                return oldNode.Update(
                    operand,
                    conversionKind,
                    oldNode.ResultKind,
                    isBaseConversion: oldNode.IsBaseConversion,
                    symbolOpt: method,
                    @checked: oldNode.Checked,
                    explicitCastInCode: oldNode.ExplicitCastInCode,
                    isExtensionMethod: oldNode.IsExtensionMethod,
                    isArrayIndex: oldNode.IsArrayIndex,
                    constantValueOpt: oldNode.ConstantValueOpt,
                    type: toType);
            }
            else
            {
                Debug.Assert(method.ReturnType == toType);
                return BoundCall.Synthesized(syntax, null, method, operand);
            }
        }

        private Conversion MakeConversion(CSharpSyntaxNode syntax, Conversion conversion, TypeSymbol fromType, TypeSymbol toType)
        {
            switch (conversion.Kind)
            {
                case ConversionKind.ExplicitUserDefined:
                case ConversionKind.ImplicitUserDefined:
                    {
                        var meth = conversion.Method;
                        Conversion fromConversion = MakeConversion(syntax, conversion.UserDefinedFromConversion, fromType, meth.Parameters[0].Type);
                        Conversion toConversion = MakeConversion(syntax, conversion.UserDefinedToConversion, meth.ReturnType, toType);
                        if (fromConversion == conversion.UserDefinedFromConversion && toConversion == conversion.UserDefinedToConversion)
                        {
                            return conversion;
                        }
                        else
                        {
                            // TODO: how do we distinguish from normal and lifted conversions here?
                            var analysis = UserDefinedConversionAnalysis.Normal(meth, fromConversion, toConversion, fromType, toType);
                            var result = UserDefinedConversionResult.Valid(ImmutableArray.Create<UserDefinedConversionAnalysis>(analysis), 0);
                            return new Conversion(result, conversion.IsImplicit);
                        }
                    }
                case ConversionKind.IntPtr:
                    {
                        SpecialMember member = GetIntPtrConversionMethod(fromType, toType);
                        MethodSymbol method = GetSpecialTypeMethod(syntax, member);
                        return MakeUserDefinedConversion(syntax, method, fromType, toType, conversion.IsImplicit);
                    }
                case ConversionKind.ImplicitNumeric:
                case ConversionKind.ExplicitNumeric:
                    // TODO: what about nullable?
                    if (fromType.SpecialType == SpecialType.System_Decimal || toType.SpecialType == SpecialType.System_Decimal)
                    {
                        SpecialMember member = DecimalConversionMethod(fromType, toType);
                        MethodSymbol method = GetSpecialTypeMethod(syntax, member);
                        return MakeUserDefinedConversion(syntax, method, fromType, toType, conversion.IsImplicit);
                    }
                    return conversion;
                case ConversionKind.ImplicitEnumeration:
                case ConversionKind.ExplicitEnumeration:
                    // TODO: what about nullable?
                    if (fromType.SpecialType == SpecialType.System_Decimal)
                    {
                        SpecialMember member = DecimalConversionMethod(fromType, toType.GetEnumUnderlyingType());
                        MethodSymbol method = GetSpecialTypeMethod(syntax, member);
                        return MakeUserDefinedConversion(syntax, method, fromType, toType, conversion.IsImplicit);
                    }
                    else if (toType.SpecialType == SpecialType.System_Decimal)
                    {
                        SpecialMember member = DecimalConversionMethod(fromType.GetEnumUnderlyingType(), toType);
                        MethodSymbol method = GetSpecialTypeMethod(syntax, member);
                        return MakeUserDefinedConversion(syntax, method, fromType, toType, conversion.IsImplicit);
                    }
                    return conversion;
                default:
                    return conversion;
            }
        }

        private Conversion MakeConversion(CSharpSyntaxNode syntax, TypeSymbol fromType, TypeSymbol toType)
        {
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            var result = MakeConversion(syntax, _compilation.Conversions.ClassifyConversion(fromType, toType, ref useSiteDiagnostics), fromType, toType);
            _diagnostics.Add(syntax, useSiteDiagnostics);
            return result;
        }

        private Conversion MakeUserDefinedConversion(CSharpSyntaxNode syntax, MethodSymbol meth, TypeSymbol fromType, TypeSymbol toType, bool isImplicit = true)
        {
            Conversion fromConversion = MakeConversion(syntax, fromType, meth.Parameters[0].Type);
            Conversion toConversion = MakeConversion(syntax, meth.ReturnType, toType);
            // TODO: distinguish between normal and lifted conversions here
            var analysis = UserDefinedConversionAnalysis.Normal(meth, fromConversion, toConversion, fromType, toType);
            var result = UserDefinedConversionResult.Valid(ImmutableArray.Create<UserDefinedConversionAnalysis>(analysis), 0);
            return new Conversion(result, isImplicit);
        }
    }
}
