// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class LocalRewriter
    {
        public override BoundNode VisitConversion(BoundConversion node)
        {
            return VisitConversion(node, passedAsParameter: null);
        }

        public BoundExpression VisitConversion(BoundConversion node, ParameterSymbol? passedAsParameter)
        {
            switch (node.ConversionKind)
            {
                case ConversionKind.InterpolatedString:
                    return RewriteInterpolatedStringConversion(node);
                case ConversionKind.InterpolatedStringHandler:
                    Debug.Assert(node.Type is NamedTypeSymbol { IsInterpolatedStringHandlerType: true });

                    var (data, parts) = node.Operand switch
                    {
                        BoundInterpolatedString { InterpolationData: { BuilderType: not null } d, Parts: { } p } => (d, p),
                        BoundBinaryOperator { InterpolatedStringHandlerData: { } d } binary => (d, CollectBinaryOperatorInterpolatedStringParts(binary)),
                        _ => throw ExceptionUtilities.UnexpectedValue(node.Operand.Kind)
                    };

                    InterpolationHandlerResult interpolationResult = RewriteToInterpolatedStringHandlerPattern(data, parts, node.Operand.Syntax);
                    return interpolationResult.WithFinalResult(interpolationResult.HandlerTemp);

                case ConversionKind.SwitchExpression:
                    // Skip through target-typed switches
                    Debug.Assert(node.Operand is BoundConvertedSwitchExpression { WasTargetTyped: true });
                    return (BoundExpression)Visit(node.Operand)!;
                case ConversionKind.ConditionalExpression:
                    // Skip through target-typed conditionals
                    Debug.Assert(node.Operand is BoundConditionalOperator { WasTargetTyped: true });
                    return (BoundExpression)Visit(node.Operand)!;
                case ConversionKind.ObjectCreation:
                    // Skip through target-typed new
                    Debug.Assert(node.Operand is not null);
                    var objectCreation = VisitExpression(node.Operand);

                    if (node.Type.IsNullableType())
                    {
                        Debug.Assert(node.Operand is BoundObjectCreationExpressionBase { WasTargetTyped: true });
                        return ConvertToNullable(node.Syntax, node.Type, objectCreation);
                    }

                    Debug.Assert(node.Operand is BoundObjectCreationExpressionBase { WasTargetTyped: true } or
                                                 BoundDelegateCreationExpression { WasTargetTyped: true });

                    return objectCreation;

                case ConversionKind.ImplicitNullable when node.Conversion.UnderlyingConversions[0].Kind is ConversionKind.CollectionExpression:
                    var rewrittenCollection = RewriteCollectionExpressionConversion(node.Conversion.UnderlyingConversions[0], (BoundCollectionExpression)node.Operand, passedAsParameter);
                    return ConvertToNullable(node.Syntax, node.Type, rewrittenCollection);

                case ConversionKind.CollectionExpression:
                    return RewriteCollectionExpressionConversion(node.Conversion, (BoundCollectionExpression)node.Operand, passedAsParameter);
            }

            var rewrittenType = VisitType(node.Type);

            bool wasInExpressionLambda = _inExpressionLambda;
            _inExpressionLambda = _inExpressionLambda || (node.ConversionKind == ConversionKind.AnonymousFunction && !wasInExpressionLambda && rewrittenType.IsExpressionTree());
            InstrumentationState.IsSuppressed = _inExpressionLambda;

            var rewrittenOperand = VisitExpression(node.Operand);
            _inExpressionLambda = wasInExpressionLambda;
            InstrumentationState.IsSuppressed = _inExpressionLambda;

            var result = MakeConversionNode(node, node.Syntax, rewrittenOperand, node.Conversion, node.Checked, node.ExplicitCastInCode, node.ConstantValueOpt, rewrittenType);

            var toType = node.Type;
            Debug.Assert(result.Type!.Equals(toType, TypeCompareKind.IgnoreDynamicAndTupleNames | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes));

            return result;
        }

        public override BoundNode VisitUtf8String(BoundUtf8String node)
        {
            return MakeUtf8Span(node, GetUtf8ByteRepresentation(node));
        }

        private BoundExpression MakeUtf8Span(BoundExpression node, IReadOnlyList<byte>? bytes)
        {
            Debug.Assert(node.Type is not null);
            Debug.Assert(_compilation.IsReadOnlySpanType(node.Type));
            var byteType = ((NamedTypeSymbol)node.Type).TypeArgumentsWithAnnotationsNoUseSiteDiagnostics.Single().Type;
            Debug.Assert(byteType.SpecialType == SpecialType.System_Byte);

            var save_Syntax = _factory.Syntax;
            _factory.Syntax = node.Syntax;

            int length = 0;
            BoundExpression result;
            var byteArray = ArrayTypeSymbol.CreateSZArray(_compilation.Assembly, TypeWithAnnotations.Create(byteType));
            BoundExpression utf8Bytes = bytes is null ?
                                            BadExpression(node.Syntax, byteArray, ImmutableArray<BoundExpression>.Empty) :
                                            MakeUnderlyingArrayForUtf8Span(node.Syntax, byteArray, bytes, out length);

            if (!TryGetWellKnownTypeMember<MethodSymbol>(node.Syntax, WellKnownMember.System_ReadOnlySpan_T__ctor_Array_Start_Length, out MethodSymbol? ctor))
            {
                result = BadExpression(node.Syntax, node.Type, ImmutableArray<BoundExpression>.Empty);
            }
            else
            {
                result = new BoundObjectCreationExpression(node.Syntax, ctor.AsMember((NamedTypeSymbol)node.Type), utf8Bytes, _factory.Literal(0), _factory.Literal(length));
            }

            _factory.Syntax = save_Syntax;

            return result;
        }

        private byte[]? GetUtf8ByteRepresentation(BoundUtf8String node)
        {
            if (node.Value.TryGetUtf8ByteRepresentation(out byte[]? result, out string? error))
            {
                return result;
            }
            else
            {
                _diagnostics.Add(
                    ErrorCode.ERR_CannotBeConvertedToUtf8,
                    node.Syntax.Location,
                    error);

                return null;
            }
        }

        private BoundArrayCreation MakeUnderlyingArrayForUtf8Span(SyntaxNode syntax, ArrayTypeSymbol byteArray, IReadOnlyList<byte> bytes, out int length)
        {
            Debug.Assert(byteArray.IsSZArray);
            Debug.Assert(byteArray.ElementType.SpecialType == SpecialType.System_Byte);

            var builder = ArrayBuilder<BoundExpression>.GetInstance(bytes.Count + 1);
            foreach (byte b in bytes)
            {
                builder.Add(_factory.Literal(b));
            }

            length = builder.Count;

            // Zero terminate memory
            builder.Add(_factory.Literal((byte)0));

            var utf8Bytes = new BoundArrayCreation(
                                    syntax,
                                    ImmutableArray.Create<BoundExpression>(_factory.Literal(builder.Count)),
                                    new BoundArrayInitialization(syntax, isInferred: false, builder.ToImmutableAndFree()),
                                    byteArray);
            return utf8Bytes;
        }

        private BoundExpression VisitUtf8Addition(BoundBinaryOperator node)
        {
            Debug.Assert(node.OperatorKind is BinaryOperatorKind.Utf8Addition);

            var bytesBuilder = ArrayBuilder<byte>.GetInstance();
            bool haveRepresentationError = false;
            var stack = ArrayBuilder<BoundExpression>.GetInstance();

            stack.Add(node);

            while (stack.Count != 0)
            {
                var current = stack.Pop();

                switch (current)
                {
                    case BoundUtf8String literal:
                        byte[]? bytes = GetUtf8ByteRepresentation(literal);

                        if (bytes is null)
                        {
                            haveRepresentationError = true;
                        }
                        else if (!haveRepresentationError)
                        {
                            bytesBuilder.AddRange(bytes);
                        }
                        break;

                    case BoundBinaryOperator utf8Addition:
                        Debug.Assert(utf8Addition.OperatorKind is BinaryOperatorKind.Utf8Addition);
                        stack.Push(utf8Addition.Right);
                        stack.Push(utf8Addition.Left);
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(current);
                }
            }

            stack.Free();

            BoundExpression result = MakeUtf8Span(node, haveRepresentationError ? null : bytesBuilder);

            bytesBuilder.Free();
            return result;
        }

        private static bool IsFloatingPointExpressionOfUnknownPrecision(BoundExpression rewrittenNode)
        {
            if (rewrittenNode == null)
            {
                return false;
            }

            if (rewrittenNode.ConstantValueOpt != null)
            {
                return false;
            }

            Debug.Assert(rewrittenNode.Type is { });
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
                    return IsFloatingPointExpressionOfUnknownPrecision(sequence.Value);

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
        private BoundExpression MakeConversionNode(
            BoundConversion? oldNodeOpt,
            SyntaxNode syntax,
            BoundExpression rewrittenOperand,
            Conversion conversion,
            bool @checked,
            bool explicitCastInCode,
            ConstantValue? constantValueOpt,
            TypeSymbol rewrittenType)
        {
            var result = MakeConversionNodeCore(oldNodeOpt, syntax, rewrittenOperand, conversion, @checked, explicitCastInCode, constantValueOpt, rewrittenType);
            Debug.Assert(result.Type is { } rt && rt.Equals(rewrittenType, TypeCompareKind.AllIgnoreOptions));

            // 4.1.6 C# spec: To force a value of a floating point type to the exact precision of its type, an explicit cast can be used.
            // It means that explicit casts to (double) or (float) should be preserved on the node.
            // If original conversion has become something else with unknown precision, add an explicit identity cast.
            if (!_inExpressionLambda &&
                explicitCastInCode &&
                IsFloatingPointExpressionOfUnknownPrecision(result))
            {
                result = new BoundConversion(
                    syntax,
                    result,
                    Conversion.Identity,
                    isBaseConversion: false,
                    @checked: false,
                    explicitCastInCode: true,
                    conversionGroupOpt: null,
                    inConversionGroupFlags: InConversionGroupFlags.Unspecified,
                    constantValueOpt: null,
                    type: result.Type);
            }

            return result;
        }

        private BoundExpression MakeConversionNodeCore(
            BoundConversion? oldNodeOpt,
            SyntaxNode syntax,
            BoundExpression rewrittenOperand,
            Conversion conversion,
            bool @checked,
            bool explicitCastInCode,
            ConstantValue? constantValueOpt,
            TypeSymbol rewrittenType)
        {
            Debug.Assert(oldNodeOpt == null || oldNodeOpt.Syntax == syntax);
            Debug.Assert(rewrittenType is { });
            Debug.Assert(_factory.ModuleBuilderOpt is { });
            Debug.Assert(_diagnostics.DiagnosticBag is { });

            if (_inExpressionLambda && !conversion.IsUserDefined)
            {
                @checked = @checked && NeedsCheckedConversionInExpressionTree(rewrittenOperand.Type, rewrittenType, explicitCastInCode);
            }

            switch (conversion.Kind)
            {
                case ConversionKind.Identity:
                    Debug.Assert(rewrittenOperand.Type is { });

                    // Spec 6.1.1:
                    //   An identity conversion converts from any type to the same type.
                    //   This conversion exists such that an entity that already has a required type can be said to be convertible to that type.
                    //   Because object and dynamic are considered equivalent there is an identity conversion between object and dynamic,
                    //   and between constructed types that are the same when replacing all occurrences of dynamic with object.

                    // Why ignoreDynamic: false?
                    // Lowering phase treats object and dynamic as equivalent types. So we don't need to produce any conversion here,
                    // but we need to change the Type property on the resulting BoundExpression to match the rewrittenType.
                    // This is necessary so that subsequent lowering transformations see that the expression is dynamic.

                    if (_inExpressionLambda || !rewrittenOperand.Type.Equals(rewrittenType, TypeCompareKind.ConsiderEverything))
                    {
                        break;
                    }

                    if (!explicitCastInCode)
                    {
                        return rewrittenOperand;
                    }

                    // 4.1.6 C# spec: To force a value of a floating point type to the exact precision of its type, an explicit cast can be used.
                    // If this is not an identity conversion of a float with unknown precision, strip away the identity conversion.
                    if (!IsFloatingPointExpressionOfUnknownPrecision(rewrittenOperand))
                    {
                        return rewrittenOperand;
                    }

                    break;

                case ConversionKind.ExplicitUserDefined:
                case ConversionKind.ImplicitUserDefined:
                    return RewriteUserDefinedConversion(
                        syntax: syntax,
                        rewrittenOperand: rewrittenOperand,
                        conversion: conversion,
                        @checked: @checked,
                        rewrittenType: rewrittenType);

                case ConversionKind.IntPtr:
                    return RewriteIntPtrConversion(syntax, rewrittenOperand, conversion, @checked,
                        explicitCastInCode, constantValueOpt, rewrittenType);

                case ConversionKind.ImplicitNullable:
                case ConversionKind.ExplicitNullable:
                    return RewriteNullableConversion(
                        syntax: syntax,
                        rewrittenOperand: rewrittenOperand,
                        conversion: conversion,
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
                            return new BoundDefaultExpression(syntax, rewrittenType);
                        }

                        BoundExpression? nullableValue = NullableAlwaysHasValue(rewrittenOperand);
                        if (nullableValue != null)
                        {
                            // Recurse, eliminating the unnecessary ctor.
                            return MakeConversionNode(oldNodeOpt, syntax, nullableValue, conversion, @checked, explicitCastInCode, constantValueOpt, rewrittenType);
                        }
                    }
                    break;

                case ConversionKind.NullLiteral:
                case ConversionKind.DefaultLiteral:
                    if (!_inExpressionLambda || !explicitCastInCode)
                    {
                        return new BoundDefaultExpression(syntax, rewrittenType);
                    }

                    break;

                case ConversionKind.ImplicitReference:
                case ConversionKind.ExplicitReference:
                    if (rewrittenOperand.IsDefaultValue() && (!_inExpressionLambda || !explicitCastInCode))
                    {
                        return new BoundDefaultExpression(syntax, rewrittenType);
                    }

                    break;

                case ConversionKind.ImplicitConstant:
                    // implicit constant conversions under nullable conversions like "byte? x = 1;
                    // are not folded since a constant cannot be nullable.
                    // As a result these conversions can reach here.
                    // Consider them same as unchecked explicit numeric conversions
                    conversion = Conversion.ExplicitNumeric;
                    @checked = false;
                    goto case ConversionKind.ImplicitNumeric;

                case ConversionKind.ImplicitNumeric:
                case ConversionKind.ExplicitNumeric:
                    Debug.Assert(rewrittenOperand.Type is { });
                    if (rewrittenOperand.IsDefaultValue() && (!_inExpressionLambda || !explicitCastInCode))
                    {
                        return new BoundDefaultExpression(syntax, rewrittenType);
                    }

                    if (rewrittenType.SpecialType == SpecialType.System_Decimal || rewrittenOperand.Type.SpecialType == SpecialType.System_Decimal)
                    {
                        return RewriteDecimalConversion(syntax, rewrittenOperand, rewrittenOperand.Type, rewrittenType, @checked, conversion.Kind.IsImplicitConversion(), constantValueOpt);
                    }
                    break;

                case ConversionKind.ImplicitTupleLiteral:
                case ConversionKind.ExplicitTupleLiteral:
                    {
                        Debug.Assert(rewrittenOperand.Type is { });
                        // we keep tuple literal conversions in the tree for the purpose of semantic model (for example when they are casts in the source)
                        // for the purpose of lowering/codegeneration they are identity conversions.
                        Debug.Assert(rewrittenOperand.Type.Equals(rewrittenType, TypeCompareKind.IgnoreDynamicAndTupleNames | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes));
                        return rewrittenOperand;
                    }

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
                        var operand = MakeConversionNode(
                            oldNodeOpt,
                            syntax,
                            rewrittenOperand,
                            conversion,
                            @checked,
                            explicitCastInCode,
                            constantValueOpt,
                            rewrittenType.GetNullableUnderlyingType());

                        var outerConversion = Conversion.ImplicitNullableWithIdentityUnderlying;
                        outerConversion.MarkUnderlyingConversionsChecked();
                        return MakeConversionNode(
                            oldNodeOpt,
                            syntax,
                            operand,
                            outerConversion,
                            @checked,
                            explicitCastInCode,
                            constantValueOpt,
                            rewrittenType);
                    }

                    goto case ConversionKind.ExplicitEnumeration;

                case ConversionKind.ExplicitEnumeration:
                    Debug.Assert(rewrittenOperand.Type is { });
                    if (!rewrittenType.IsNullableType() &&
                        rewrittenOperand.IsDefaultValue() &&
                        (!_inExpressionLambda || !explicitCastInCode))
                    {
                        return new BoundDefaultExpression(syntax, rewrittenType);
                    }

                    if (rewrittenType.SpecialType == SpecialType.System_Decimal)
                    {
                        Debug.Assert(rewrittenOperand.Type.IsEnumType());
                        var underlyingTypeFrom = rewrittenOperand.Type.GetEnumUnderlyingType()!;
                        rewrittenOperand = MakeConversionNode(rewrittenOperand, underlyingTypeFrom, false);
                        return RewriteDecimalConversion(syntax, rewrittenOperand, underlyingTypeFrom, rewrittenType, @checked, isImplicit: false, constantValueOpt: constantValueOpt);
                    }
                    else if (rewrittenOperand.Type.SpecialType == SpecialType.System_Decimal)
                    {
                        // This is where we handle conversion from Decimal to Enum: e.g., E e = (E) d;
                        // where 'e' is of type Enum E and 'd' is of type Decimal.
                        // Conversion can be simply done by applying its underlying numeric type to RewriteDecimalConversion().

                        Debug.Assert(rewrittenType.IsEnumType());
                        var underlyingTypeTo = rewrittenType.GetEnumUnderlyingType()!;
                        var rewrittenNode = RewriteDecimalConversion(syntax, rewrittenOperand, rewrittenOperand.Type, underlyingTypeTo, @checked, isImplicit: false, constantValueOpt: constantValueOpt);

                        // However, the type of the rewritten node becomes underlying numeric type, not Enum type,
                        // which violates the overall constraint saying the type cannot be changed during rewriting (see LocalRewriter.cs).

                        // Instead of loosening this constraint, we return BoundConversion from underlying numeric type to Enum type,
                        // which will be eliminated during emitting (see EmitEnumConversion): e.g., E e = (E)(int) d;
                        return new BoundConversion(
                            syntax,
                            rewrittenNode,
                            conversion,
                            isBaseConversion: false,
                            @checked: false,
                            explicitCastInCode: explicitCastInCode,
                            conversionGroupOpt: null,
                            inConversionGroupFlags: InConversionGroupFlags.Unspecified,
                            constantValueOpt: constantValueOpt,
                            type: rewrittenType);
                    }

                    break;

                case ConversionKind.ImplicitDynamic:
                case ConversionKind.ExplicitDynamic:
                    Debug.Assert(conversion.Method is null);
                    Debug.Assert(!conversion.IsExtensionMethod);
                    Debug.Assert(constantValueOpt == null);
                    return _dynamicFactory.MakeDynamicConversion(rewrittenOperand, explicitCastInCode || conversion.Kind == ConversionKind.ExplicitDynamic, conversion.IsArrayIndex, @checked, rewrittenType).ToExpression();

                case ConversionKind.ImplicitTuple:
                case ConversionKind.ExplicitTuple:
                    return RewriteTupleConversion(
                        syntax: syntax,
                        rewrittenOperand: rewrittenOperand,
                        conversion: conversion,
                        @checked: @checked,
                        explicitCastInCode: explicitCastInCode,
                        rewrittenType: (NamedTypeSymbol)rewrittenType);

                case ConversionKind.MethodGroup when oldNodeOpt is { Type: { TypeKind: TypeKind.FunctionPointer } funcPtrType }:
                    {
                        var mg = (BoundMethodGroup)rewrittenOperand;
                        MethodSymbol? symbolOpt = oldNodeOpt.SymbolOpt;
                        Debug.Assert(symbolOpt is { });
                        return new BoundFunctionPointerLoad(oldNodeOpt.Syntax, symbolOpt,
                                                            constrainedToTypeOpt: symbolOpt.IsStatic &&
                                                                                  (symbolOpt.IsAbstract || symbolOpt.IsVirtual) ? mg.ReceiverOpt?.Type : null,
                                                            type: funcPtrType, hasErrors: false);
                    }

                case ConversionKind.MethodGroup:
                    {
                        // we eliminate the method group conversion entirely from the bound nodes following local lowering
                        Debug.Assert(oldNodeOpt is { });
                        var mg = (BoundMethodGroup)rewrittenOperand;
                        var method = oldNodeOpt.SymbolOpt;
                        Debug.Assert(method is { });
                        var oldSyntax = _factory.Syntax;
                        _factory.Syntax = (mg.ReceiverOpt ?? mg).Syntax;
                        var receiver = (!method.RequiresInstanceReceiver && !oldNodeOpt.IsExtensionMethod && !method.IsAbstract && !method.IsVirtual) ? _factory.Type(method.ContainingType) : mg.ReceiverOpt;
                        Debug.Assert(receiver is { });
                        _factory.Syntax = oldSyntax;

                        var boundDelegateCreation = new BoundDelegateCreationExpression(syntax, argument: receiver, methodOpt: method,
                                                                                        isExtensionMethod: oldNodeOpt.IsExtensionMethod, wasTargetTyped: false, type: rewrittenType);

                        EnsureParamCollectionAttributeExists(rewrittenOperand.Syntax, rewrittenType);
                        Debug.Assert(_factory.TopLevelMethod is { });

                        if (_factory.Compilation.LanguageVersion >= MessageID.IDS_FeatureCacheStaticMethodGroupConversion.RequiredVersion()
                            && !_inExpressionLambda // The tree structure / meaning for expression trees should remain untouched.
                            && _factory.TopLevelMethod.MethodKind != MethodKind.StaticConstructor // Avoid caching twice if people do it manually.
                            && DelegateCacheRewriter.CanRewrite(boundDelegateCreation))
                        {
                            var rewriter = _lazyDelegateCacheRewriter ??= new DelegateCacheRewriter(_factory, _topLevelMethodOrdinal);
                            return rewriter.Rewrite(boundDelegateCreation);
                        }
                        else
                        {
                            return boundDelegateCreation;
                        }
                    }

                case ConversionKind.InlineArray:
                    {
                        Debug.Assert(rewrittenOperand.Type is not null);

                        NamedTypeSymbol spanType = (NamedTypeSymbol)rewrittenType;
                        MethodSymbol createSpan;

                        if (spanType.OriginalDefinition.Equals(_compilation.GetWellKnownType(WellKnownType.System_ReadOnlySpan_T), TypeCompareKind.AllIgnoreOptions))
                        {
                            createSpan = _factory.ModuleBuilderOpt.EnsureInlineArrayAsReadOnlySpanExists(syntax, spanType.OriginalDefinition, _factory.SpecialType(SpecialType.System_Int32), _diagnostics.DiagnosticBag);
                        }
                        else
                        {
                            Debug.Assert(spanType.OriginalDefinition.Equals(_compilation.GetWellKnownType(WellKnownType.System_Span_T), TypeCompareKind.AllIgnoreOptions));
                            createSpan = _factory.ModuleBuilderOpt.EnsureInlineArrayAsSpanExists(syntax, spanType.OriginalDefinition, _factory.SpecialType(SpecialType.System_Int32), _diagnostics.DiagnosticBag);
                        }

                        createSpan = createSpan.Construct(rewrittenOperand.Type, spanType.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics.Single().Type);
                        _ = rewrittenOperand.Type.HasInlineArrayAttribute(out int length);

                        return _factory.Call(null, createSpan, rewrittenOperand, _factory.Literal(length), useStrictArgumentRefKinds: true);
                    }

                case ConversionKind.ImplicitSpan:
                case ConversionKind.ExplicitSpan:
                    {
                        var sourceType = rewrittenOperand.Type;
                        var destinationType = (NamedTypeSymbol)rewrittenType;

                        Debug.Assert(sourceType is not null);

                        // array to Span/ReadOnlySpan (implicit or explicit)
                        if (sourceType is ArrayTypeSymbol)
                        {
                            if (Binder.TryFindImplicitOperatorFromArray(destinationType.OriginalDefinition) is not { } methodDefinition)
                            {
                                throw ExceptionUtilities.Unreachable();
                            }

                            MethodSymbol method = methodDefinition.AsMember(destinationType);

                            TypeSymbol parameterType = method.ParameterTypesWithAnnotations[0].Type;
                            Debug.Assert(parameterType.IsSZArray());
                            Debug.Assert(rewrittenOperand.Type?.IsSZArray() == true);
                            Conversion c = _factory.ClassifyEmitConversion(rewrittenOperand, parameterType);
                            Debug.Assert(c.IsImplicit || conversion.IsExplicit);
                            Debug.Assert(c.IsReference || c.IsIdentity);
                            rewrittenOperand = _factory.Convert(parameterType, rewrittenOperand, c);

                            if (!_inExpressionLambda && _compilation.IsReadOnlySpanType(destinationType))
                            {
                                return new BoundReadOnlySpanFromArray(syntax, rewrittenOperand, method, destinationType) { WasCompilerGenerated = true };
                            }

                            return _factory.Call(null, method, rewrittenOperand);
                        }

                        // Span to ReadOnlySpan (implicit only)
                        if (sourceType.IsSpan())
                        {
                            Debug.Assert(destinationType.IsReadOnlySpan());
                            Debug.Assert(conversion.Kind is ConversionKind.ImplicitSpan);

                            if (Binder.TryFindImplicitOperatorFromSpan(sourceType.OriginalDefinition, destinationType.OriginalDefinition) is not { } implicitOperatorDefinition)
                            {
                                throw ExceptionUtilities.Unreachable();
                            }

                            MethodSymbol implicitOperator = implicitOperatorDefinition.AsMember((NamedTypeSymbol)sourceType);

                            Debug.Assert(implicitOperator.ParameterTypesWithAnnotations[0].Type.Equals(rewrittenOperand.Type, TypeCompareKind.AllIgnoreOptions));
                            rewrittenOperand = _factory.Call(null, implicitOperator, rewrittenOperand);

                            if (Binder.NeedsSpanCastUp(sourceType, destinationType))
                            {
                                if (Binder.TryFindCastUpMethod(implicitOperator.ReturnType.OriginalDefinition, destinationType.OriginalDefinition) is not { } castUpMethodDefinition)
                                {
                                    throw ExceptionUtilities.Unreachable();
                                }

                                TypeWithAnnotations sourceElementType = ((NamedTypeSymbol)sourceType).TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0];
                                MethodSymbol castUpMethod = castUpMethodDefinition.AsMember(destinationType).Construct([sourceElementType]);

                                return _factory.Call(null, castUpMethod, rewrittenOperand);
                            }

                            return rewrittenOperand;
                        }

                        // ReadOnlySpan to ReadOnlySpan (implicit only)
                        if (sourceType.IsReadOnlySpan())
                        {
                            Debug.Assert(destinationType.IsReadOnlySpan());
                            Debug.Assert(conversion.Kind is ConversionKind.ImplicitSpan);
                            Debug.Assert(Binder.NeedsSpanCastUp(sourceType, destinationType));

                            if (Binder.TryFindCastUpMethod(sourceType.OriginalDefinition, destinationType.OriginalDefinition) is not { } methodDefinition)
                            {
                                throw ExceptionUtilities.Unreachable();
                            }

                            TypeWithAnnotations sourceElementType = ((NamedTypeSymbol)sourceType).TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0];
                            MethodSymbol method = methodDefinition.AsMember(destinationType).Construct([sourceElementType]);

                            Debug.Assert(method.ParameterTypesWithAnnotations[0].Type.Equals(rewrittenOperand.Type, TypeCompareKind.AllIgnoreOptions));
                            return _factory.Call(null, method, rewrittenOperand);
                        }

                        // string to ReadOnlySpan (implicit only)
                        if (sourceType.IsStringType())
                        {
                            Debug.Assert(destinationType.IsReadOnlySpan());
                            Debug.Assert(conversion.Kind is ConversionKind.ImplicitSpan);

                            if (Binder.TryFindAsSpanCharMethod(_compilation, destinationType) is not { } method)
                            {
                                throw ExceptionUtilities.Unreachable();
                            }

                            Debug.Assert(method.Parameters[0].Type.IsStringType());
                            Debug.Assert(rewrittenOperand.Type?.IsStringType() == true);
                            return _factory.Call(null, method, rewrittenOperand);
                        }

                        throw ExceptionUtilities.Unreachable();
                    }

                default:
                    break;
            }

            return oldNodeOpt != null ?
                oldNodeOpt.Update(
                    rewrittenOperand,
                    conversion,
                    isBaseConversion: oldNodeOpt.IsBaseConversion,
                    @checked: @checked,
                    explicitCastInCode: explicitCastInCode,
                    conversionGroupOpt: null,
                    inConversionGroupFlags: InConversionGroupFlags.Unspecified,
                    constantValueOpt: constantValueOpt,
                    type: rewrittenType) :
                new BoundConversion(
                    syntax,
                    rewrittenOperand,
                    conversion,
                    isBaseConversion: false,
                    @checked: @checked,
                    explicitCastInCode: explicitCastInCode,
                    conversionGroupOpt: null, // BoundConversion.ConversionGroup is not used in lowered tree
                    inConversionGroupFlags: InConversionGroupFlags.Unspecified,
                    constantValueOpt: constantValueOpt,
                    type: rewrittenType);
        }

        private void EnsureParamCollectionAttributeExists(SyntaxNode node, TypeSymbol delegateType)
        {
            Debug.Assert(_factory.ModuleBuilderOpt is { });

            if (delegateType.IsAnonymousType && delegateType.ContainingModule == _compilation.SourceModule &&
                delegateType.DelegateInvokeMethod() is MethodSymbol delegateInvoke &&
                delegateInvoke.Parameters.Any(static (p) => p.IsParamsCollection))
            {
                _factory.ModuleBuilderOpt.EnsureParamCollectionAttributeExists(_diagnostics, node.Location);
            }
        }

        // Determine if the conversion can actually overflow at runtime.  If not, no need to generate a checked instruction.
        private static bool NeedsCheckedConversionInExpressionTree(TypeSymbol? source, TypeSymbol target, bool explicitCastInCode)
        {
            Debug.Assert((object)target != null);

            if (source is null)
            {
                return false;
            }

            SpecialType GetUnderlyingSpecialType(TypeSymbol type) =>
                type.StrippedType().EnumUnderlyingTypeOrSelf().SpecialType;

            bool IsInRange(SpecialType type, SpecialType low, SpecialType high) =>
                low <= type && type <= high;

            SpecialType sourceST = GetUnderlyingSpecialType(source);
            SpecialType targetST = GetUnderlyingSpecialType(target);

            // integral to double or float is never checked, but float/double to integral
            // may be checked.
            return (explicitCastInCode || sourceST != targetST) &&
                IsInRange(sourceST, SpecialType.System_Char, SpecialType.System_Double) &&
                IsInRange(targetST, SpecialType.System_Char, SpecialType.System_UInt64);
        }

        /// <summary>
        /// Helper method to generate a lowered conversion from the given <paramref name="rewrittenOperand"/> to the given <paramref name="rewrittenType"/>.
        /// </summary>
        /// <remarks>
        /// If we're converting a default parameter value to the parameter type, then the conversion can actually fail
        /// (e.g. if the default value was specified by an attribute and was, therefore, not checked by the compiler).
        /// Set acceptFailingConversion if you want to see default(rewrittenType) in such cases.
        /// The error will be suppressed only for conversions from <see cref="decimal"/> or <see cref="DateTime"/>.
        ///
        /// Generally, conversions should be checked and BoundConversion nodes created during initial binding and re-used in lowering. But in some cases,
        /// we do not preserve the conversion node. In such case, <paramref name="markAsChecked"/> is set to true
        /// to indicate that the conversion has been previously checked during initial binding.
        /// </remarks>
        private BoundExpression MakeConversionNode(BoundExpression rewrittenOperand, TypeSymbol rewrittenType, bool @checked, bool acceptFailingConversion = false, bool markAsChecked = false)
        {
            Conversion conversion = MakeConversion(rewrittenOperand, rewrittenType, @checked: @checked, _compilation, _diagnostics, acceptFailingConversion);
            if (!conversion.IsValid)
            {
                return _factory.NullOrDefault(rewrittenType);
            }

#if DEBUG
            if (markAsChecked)
            {
                conversion.MarkUnderlyingConversionsCheckedRecursive();
            }
#endif

            return MakeConversionNode(rewrittenOperand.Syntax, rewrittenOperand, conversion, rewrittenType, @checked);
        }

        private static Conversion MakeConversion(
            BoundExpression rewrittenOperand,
            TypeSymbol rewrittenType,
            bool @checked,
            CSharpCompilation compilation,
            BindingDiagnosticBag diagnostics,
            bool acceptFailingConversion)
        {
            Debug.Assert(rewrittenOperand.Type is { });
            var useSiteInfo = new CompoundUseSiteInfo<AssemblySymbol>(diagnostics, compilation.Assembly);
            Conversion conversion = compilation.Conversions.ClassifyConversionFromType(rewrittenOperand.Type, rewrittenType, isChecked: @checked, ref useSiteInfo);
            diagnostics.Add(rewrittenOperand.Syntax, useSiteInfo);

            if (!conversion.IsValid)
            {
                if (!acceptFailingConversion ||
                     rewrittenOperand.Type.SpecialType != SpecialType.System_Decimal &&
                     rewrittenOperand.Type.SpecialType != SpecialType.System_DateTime)
                {
                    // error CS0029: Cannot implicitly convert type '{0}' to '{1}'
                    diagnostics.Add(
                        ErrorCode.ERR_NoImplicitConv,
                        rewrittenOperand.Syntax.Location,
                        rewrittenOperand.Type,
                        rewrittenType);
                }
            }

            return conversion;
        }

        internal BoundExpression MakeConversionNode(
            SyntaxNode syntax,
            BoundExpression rewrittenOperand,
            Conversion conversion,
            TypeSymbol rewrittenType,
            bool @checked,
            bool explicitCastInCode = false,
            ConstantValue? constantValueOpt = null)
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
                Debug.Assert(conversion.Method is { });
                Debug.Assert(conversion.BestUserDefinedConversionAnalysis is { });
                conversion.AssertUnderlyingConversionsCheckedRecursive();

                if (!TypeSymbol.Equals(rewrittenOperand.Type, conversion.BestUserDefinedConversionAnalysis.FromType, TypeCompareKind.ConsiderEverything2))
                {
                    rewrittenOperand = MakeConversionNode(
                        syntax,
                        rewrittenOperand,
                        conversion.UserDefinedFromConversion,
                        conversion.BestUserDefinedConversionAnalysis.FromType,
                        @checked);
                }

                if (!TypeSymbol.Equals(rewrittenOperand.Type, conversion.Method.GetParameterType(0), TypeCompareKind.ConsiderEverything2))
                {
                    rewrittenOperand = MakeConversionNode(
                        rewrittenOperand,
                        conversion.BestUserDefinedConversionAnalysis.FromType,
                        @checked,
                        markAsChecked: true);
                }

                TypeSymbol userDefinedConversionRewrittenType = conversion.Method.ReturnType;

                // Lifted conversion, wrap return type in Nullable
                // The conversion only needs to happen for non-nullable valuetypes
                Debug.Assert(rewrittenOperand.Type is { });
                if (rewrittenOperand.Type.IsNullableType() &&
                        conversion.Method.GetParameterType(0).Equals(rewrittenOperand.Type.GetNullableUnderlyingType(), TypeCompareKind.AllIgnoreOptions) &&
                        !userDefinedConversionRewrittenType.IsNullableType() &&
                        userDefinedConversionRewrittenType.IsValueType)
                {
                    userDefinedConversionRewrittenType = ((NamedTypeSymbol)rewrittenOperand.Type.OriginalDefinition).Construct(userDefinedConversionRewrittenType);
                }

                BoundExpression userDefined = RewriteUserDefinedConversion(
                    syntax,
                    rewrittenOperand,
                    conversion,
                    @checked: @checked,
                    userDefinedConversionRewrittenType);

                if (!TypeSymbol.Equals(userDefined.Type, conversion.BestUserDefinedConversionAnalysis.ToType, TypeCompareKind.ConsiderEverything2))
                {
                    userDefined = MakeConversionNode(
                        userDefined,
                        conversion.BestUserDefinedConversionAnalysis.ToType,
                        @checked,
                        markAsChecked: true);
                }

                if (!TypeSymbol.Equals(userDefined.Type, rewrittenType, TypeCompareKind.ConsiderEverything2))
                {
                    userDefined = MakeConversionNode(
                        syntax,
                        userDefined,
                        conversion.UserDefinedToConversion,
                        rewrittenType,
                        @checked);
                }

                return userDefined;
            }

            return MakeConversionNode(
                oldNodeOpt: null,
                syntax: syntax,
                rewrittenOperand: rewrittenOperand,
                conversion: conversion,
                @checked: @checked,
                explicitCastInCode: explicitCastInCode,
                constantValueOpt: constantValueOpt,
                rewrittenType: rewrittenType);
        }

        private BoundExpression RewriteTupleConversion(
            SyntaxNode syntax,
            BoundExpression rewrittenOperand,
            Conversion conversion,
            bool @checked,
            bool explicitCastInCode,
            NamedTypeSymbol rewrittenType)
        {
            Debug.Assert(rewrittenOperand.Type is { });
            var destElementTypes = rewrittenType.TupleElementTypesWithAnnotations;
            var numElements = destElementTypes.Length;

            var tupleTypeSymbol = (NamedTypeSymbol)rewrittenOperand.Type;
            var srcElementFields = tupleTypeSymbol.TupleElements;
            var fieldAccessorsBuilder = ArrayBuilder<BoundExpression>.GetInstance(numElements);

            BoundAssignmentOperator assignmentToTemp;
            var savedTuple = _factory.StoreToTemp(rewrittenOperand, out assignmentToTemp);
            var elementConversions = conversion.UnderlyingConversions;
            conversion.AssertUnderlyingConversionsChecked();

            for (int i = 0; i < numElements; i++)
            {
                var fieldAccess = MakeTupleFieldAccessAndReportUseSiteDiagnostics(savedTuple, syntax, srcElementFields[i]);
                var convertedFieldAccess = MakeConversionNode(syntax, fieldAccess, elementConversions[i], destElementTypes[i].Type, @checked, explicitCastInCode);
                fieldAccessorsBuilder.Add(convertedFieldAccess);
            }

            var result = MakeTupleCreationExpression(syntax, rewrittenType, fieldAccessorsBuilder.ToImmutableAndFree());
            return _factory.MakeSequence(savedTuple.LocalSymbol, assignmentToTemp, result);
        }

        internal static bool NullableNeverHasValue(BoundExpression expression)
        {
            // CONSIDER: A sequence of side effects with an always-null expression as its value
            // CONSIDER: can be optimized also. Should we?

            return expression.NullableNeverHasValue();
        }

        /// <summary>
        /// If the nullable expression always has a value, returns the value, otherwise null.
        /// This is normally performed on a lowered expression, however for the purpose of
        /// tuples and tuple equality operators, we do this on a partially lowered expression in
        /// which conversions appearing at the top of the expression have not been lowered.
        /// If this method is updated to recognize more complex patterns, callers should be reviewed.
        /// </summary>
        internal static BoundExpression? NullableAlwaysHasValue(BoundExpression expression)
        {
            Debug.Assert(expression.Type is { });
            if (!expression.Type.IsNullableType())
                return null;

            switch (expression)
            {
                // Detect the lowered nullable conversion from value type K to type Nullable<K>
                case BoundObjectCreationExpression { Arguments: { Length: 1 } args }:
                    return args[0];

                // Detect the unlowered nullable conversion from value type K to type Nullable<K>
                // This arises in lowering tuple equality operators
                case BoundConversion { Conversion: { Kind: ConversionKind.ImplicitNullable }, Operand: var convertedArgument }
                        when convertedArgument.Type!.Equals(expression.Type.StrippedType(), TypeCompareKind.AllIgnoreOptions):
                    return convertedArgument;

                // Detect the unlowered nullable conversion from a tuple type T1 to Nullable<T2> for a tuple type T2.
                case BoundConversion { Conversion: { Kind: ConversionKind.ImplicitNullable, UnderlyingConversions: var underlying }, Operand: var convertedArgument } conversion
                        when underlying.Length == 1 && underlying[0].Kind == ConversionKind.ImplicitTuple && !convertedArgument.Type!.IsNullableType():

                    conversion.Conversion.AssertUnderlyingConversionsChecked();
                    return new BoundConversion(
                        syntax: expression.Syntax,
                        operand: convertedArgument,
                        conversion: underlying[0],
                        @checked: conversion.Checked,
                        explicitCastInCode: conversion.ExplicitCastInCode,
                        conversionGroupOpt: null,
                        InConversionGroupFlags.TupleBinaryOperatorPendingLowering,
                        constantValueOpt: null,
                        type: conversion.Type.StrippedType(),
                        hasErrors: conversion.HasErrors);

                // No other cases are recognized
                default:
                    return null;
            }
        }

        private BoundExpression RewriteNullableConversion(
            SyntaxNode syntax,
            BoundExpression rewrittenOperand,
            Conversion conversion,
            bool @checked,
            bool explicitCastInCode,
            TypeSymbol rewrittenType)
        {
            Debug.Assert((object)rewrittenType != null);

            if (_inExpressionLambda)
            {
                return RewriteLiftedConversionInExpressionTree(syntax, rewrittenOperand, conversion, @checked, explicitCastInCode, rewrittenType);
            }

            TypeSymbol? rewrittenOperandType = rewrittenOperand.Type;
            Debug.Assert(rewrittenOperandType is { });
            Debug.Assert(rewrittenType.IsNullableType() || rewrittenOperandType.IsNullableType());

            if (rewrittenOperandType.IsNullableType() && rewrittenType.IsNullableType())
            {
                return RewriteFullyLiftedBuiltInConversion(syntax, rewrittenOperand, conversion, @checked, rewrittenType);
            }
            else if (rewrittenType.IsNullableType())
            {
                // SPEC: If the nullable conversion is from S to T?, the conversion is
                // SPEC: evaluated as the underlying conversion from S to T followed
                // SPEC: by a wrapping from T to T?.

                conversion.AssertUnderlyingConversionsChecked();
                BoundExpression rewrittenConversion = MakeConversionNode(syntax, rewrittenOperand, conversion.UnderlyingConversions[0], rewrittenType.GetNullableUnderlyingType(), @checked);
                MethodSymbol ctor = UnsafeGetNullableMethod(syntax, rewrittenType, SpecialMember.System_Nullable_T__ctor);
                return new BoundObjectCreationExpression(syntax, ctor, rewrittenConversion);
            }
            else
            {
                // SPEC: if the nullable conversion is from S? to T, the conversion is
                // SPEC: evaluated as an unwrapping from S? to S followed by the underlying
                // SPEC: conversion from S to T.

                // We can do a simple optimization here if we know that the source is never null:

                BoundExpression? value = NullableAlwaysHasValue(rewrittenOperand);
                if (value == null)
                {
                    // (If the source is known to be possibly null then we need to keep the call to get Value
                    // in place so that it throws at runtime.)
                    MethodSymbol get_Value = UnsafeGetNullableMethod(syntax, rewrittenOperandType, SpecialMember.System_Nullable_T_get_Value);
                    value = BoundCall.Synthesized(syntax, rewrittenOperand, initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown, get_Value);
                }

                conversion.AssertUnderlyingConversionsChecked();
                return MakeConversionNode(syntax, value, conversion.UnderlyingConversions[0], rewrittenType, @checked);
            }
        }

        private BoundExpression RewriteLiftedConversionInExpressionTree(
            SyntaxNode syntax,
            BoundExpression rewrittenOperand,
            Conversion conversion,
            bool @checked,
            bool explicitCastInCode,
            TypeSymbol rewrittenType)
        {
            Debug.Assert((object)rewrittenType != null);
            Debug.Assert(rewrittenOperand.Type is { });
            TypeSymbol rewrittenOperandType = rewrittenOperand.Type;
            Debug.Assert(rewrittenType.IsNullableType() || rewrittenOperandType.IsNullableType());

            TypeSymbol typeFrom = rewrittenOperandType.StrippedType();
            TypeSymbol typeTo = rewrittenType.StrippedType();
            if (!TypeSymbol.Equals(typeFrom, typeTo, TypeCompareKind.ConsiderEverything2) && (typeFrom.SpecialType == SpecialType.System_Decimal || typeTo.SpecialType == SpecialType.System_Decimal))
            {
                // take special care if the underlying conversion is a decimal conversion
                TypeSymbol typeFromUnderlying = typeFrom;
                TypeSymbol typeToUnderlying = typeTo;

                // They can't both be enums, since one of them is decimal.
                if (typeFrom.IsEnumType())
                {
                    typeFromUnderlying = typeFrom.GetEnumUnderlyingType()!;

                    // NOTE: Dev10 converts enum? to underlying?, rather than directly to underlying.
                    rewrittenOperandType = rewrittenOperandType.IsNullableType() ? ((NamedTypeSymbol)rewrittenOperandType.OriginalDefinition).Construct(typeFromUnderlying) : typeFromUnderlying;
                    rewrittenOperand = BoundConversion.SynthesizedNonUserDefined(syntax, rewrittenOperand, Conversion.ImplicitEnumeration, rewrittenOperandType);
                }
                else if (typeTo.IsEnumType())
                {
                    typeToUnderlying = typeTo.GetEnumUnderlyingType()!;
                }

                if (!TryGetSpecialTypeMethod(syntax, DecimalConversionMethod(typeFromUnderlying, typeToUnderlying), out MethodSymbol method))
                {
                    return BadExpression(syntax, rewrittenType, rewrittenOperand);
                }

                var conversionKind = conversion.Kind.IsImplicitConversion() ? ConversionKind.ImplicitUserDefined : ConversionKind.ExplicitUserDefined;
                var userDefinedConversion = new Conversion(conversionKind, method, false);
                var result = new BoundConversion(
                    syntax, rewrittenOperand, userDefinedConversion, @checked, explicitCastInCode: explicitCastInCode,
                    conversionGroupOpt: null, InConversionGroupFlags.LoweredFormOfUserDefinedConversionForExpressionTree, constantValueOpt: null, rewrittenType);
                return result;
            }
            else
            {
                return new BoundConversion(syntax, rewrittenOperand, conversion, @checked, explicitCastInCode: explicitCastInCode, conversionGroupOpt: null, InConversionGroupFlags.Unspecified, constantValueOpt: null, rewrittenType);
            }
        }

        private BoundExpression RewriteFullyLiftedBuiltInConversion(
            SyntaxNode syntax,
            BoundExpression operand,
            Conversion conversion,
            bool @checked,
            TypeSymbol type)
        {
            // SPEC: If the nullable conversion is from S? to T?:
            // SPEC: * If the source HasValue property is false the result
            // SPEC:   is a null value of type T?.
            // SPEC: * Otherwise the conversion is evaluated as an unwrapping
            // SPEC:   from S? to S, followed by the underlying conversion from
            // SPEC:   S to T, followed by a wrapping from T to T?

            BoundExpression? optimized = OptimizeLiftedBuiltInConversion(syntax, operand, conversion, @checked, type);
            if (optimized != null)
            {
                return optimized;
            }

            // We are unable to optimize the conversion. "(T?)s" is generated as:
            // S? temp = s;
            // temp.HasValue ? new T?((T)temp.GetValueOrDefault()) : default(T?)

            BoundAssignmentOperator tempAssignment;
            var boundTemp = _factory.StoreToTemp(operand, out tempAssignment);
            MethodSymbol getValueOrDefault;

            if (!TryGetNullableMethod(syntax, boundTemp.Type, SpecialMember.System_Nullable_T_GetValueOrDefault, out getValueOrDefault))
            {
                return BadExpression(syntax, type, operand);
            }

            BoundExpression condition = MakeNullableHasValue(syntax, boundTemp);
            conversion.AssertUnderlyingConversionsChecked();
            BoundExpression consequence = new BoundObjectCreationExpression(
                syntax,
                UnsafeGetNullableMethod(syntax, type, SpecialMember.System_Nullable_T__ctor),
                MakeConversionNode(
                    syntax,
                    BoundCall.Synthesized(syntax, boundTemp, initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown, getValueOrDefault),
                    conversion.UnderlyingConversions[0],
                    type.GetNullableUnderlyingType(),
                    @checked));
            BoundExpression alternative = new BoundDefaultExpression(syntax, type);
            BoundExpression conditionalExpression = RewriteConditionalOperator(
                syntax: syntax,
                rewrittenCondition: condition,
                rewrittenConsequence: consequence,
                rewrittenAlternative: alternative,
                constantValueOpt: null,
                rewrittenType: type,
                isRef: false);

            return new BoundSequence(
                syntax: syntax,
                locals: ImmutableArray.Create(boundTemp.LocalSymbol),
                sideEffects: ImmutableArray.Create<BoundExpression>(tempAssignment),
                value: conditionalExpression,
                type: type);
        }

        private BoundExpression? OptimizeLiftedUserDefinedConversion(
            SyntaxNode syntax,
            BoundExpression operand,
            Conversion conversion,
            TypeSymbol type)
        {
            // We begin with some optimizations: if the converted expression is known to always be null
            // then we can skip the whole thing and simply return the alternative:

            if (NullableNeverHasValue(operand))
            {
                return new BoundDefaultExpression(syntax, type);
            }

            // If the converted expression is known to never be null then we can return
            // new R?(op_Whatever(nonNullableValue))
            BoundExpression? nonNullValue = NullableAlwaysHasValue(operand);
            if (nonNullValue != null)
            {
                Debug.Assert(conversion.Method is { });
                var constrainedToTypeOpt = conversion.ConstrainedToTypeOpt;
                return MakeLiftedUserDefinedConversionConsequence(BoundCall.Synthesized(
                    syntax,
                    receiverOpt: constrainedToTypeOpt is null ? null : new BoundTypeExpression(syntax, aliasOpt: null, constrainedToTypeOpt),
                    initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown,
                    conversion.Method,
                    nonNullValue), type);
            }

            return DistributeLiftedConversionIntoLiftedOperand(syntax, operand, conversion, false, type);
        }

        private BoundExpression? OptimizeLiftedBuiltInConversion(
            SyntaxNode syntax,
            BoundExpression operand,
            Conversion conversion,
            bool @checked,
            TypeSymbol type)
        {
            Debug.Assert(operand != null);
            Debug.Assert((object)type != null);

            // First, an optimization. If the source is known to always be null then
            // we can simply return the alternative.

            if (NullableNeverHasValue(operand))
            {
                return new BoundDefaultExpression(syntax, type);
            }

            // Second, a trickier optimization. If the conversion is "(T?)(new S?(x))" then
            // we generate "new T?((T)x)"

            BoundExpression? nonNullValue = NullableAlwaysHasValue(operand);
            if (nonNullValue != null)
            {
                conversion.AssertUnderlyingConversionsChecked();
                return new BoundObjectCreationExpression(
                    syntax,
                    UnsafeGetNullableMethod(syntax, type, SpecialMember.System_Nullable_T__ctor),
                    MakeConversionNode(
                        syntax,
                        nonNullValue,
                        conversion.UnderlyingConversions[0],
                        type.GetNullableUnderlyingType(),
                        @checked));
            }

            // Third, a very tricky optimization.
            return DistributeLiftedConversionIntoLiftedOperand(syntax, operand, conversion, @checked, type);
        }

        private BoundExpression? DistributeLiftedConversionIntoLiftedOperand(
            SyntaxNode syntax,
            BoundExpression operand,
            Conversion conversion,
            bool @checked,
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
                    Debug.Assert(TypeSymbol.Equals(seq.Type, conditional.Type, TypeCompareKind.ConsiderEverything2));
                    Debug.Assert(TypeSymbol.Equals(conditional.Type, conditional.Consequence.Type, TypeCompareKind.ConsiderEverything2));
                    Debug.Assert(TypeSymbol.Equals(conditional.Type, conditional.Alternative.Type, TypeCompareKind.ConsiderEverything2));

                    if (NullableAlwaysHasValue(conditional.Consequence) != null && NullableNeverHasValue(conditional.Alternative))
                    {
                        return new BoundSequence(
                            seq.Syntax,
                            seq.Locals,
                            seq.SideEffects,
                            RewriteConditionalOperator(
                                conditional.Syntax,
                                conditional.Condition,
                                MakeConversionNode(null, syntax, conditional.Consequence, conversion, @checked, explicitCastInCode: false, constantValueOpt: ConstantValue.NotAvailable, rewrittenType: type),
                                MakeConversionNode(null, syntax, conditional.Alternative, conversion, @checked, explicitCastInCode: false, constantValueOpt: ConstantValue.NotAvailable, rewrittenType: type),
                                ConstantValue.NotAvailable,
                                type,
                                isRef: false),
                            type);
                    }
                }
            }

            return null;
        }

        private BoundExpression RewriteUserDefinedConversion(
            SyntaxNode syntax,
            BoundExpression rewrittenOperand,
            Conversion conversion,
            bool @checked,
            TypeSymbol rewrittenType)
        {
            Debug.Assert(conversion.Method is { } && !conversion.Method.ReturnsVoid && conversion.Method.ParameterCount == 1);
            Debug.Assert(rewrittenOperand.Type is { });
            if (rewrittenOperand.Type.IsNullableType())
            {
                var parameterType = conversion.Method.GetParameterType(0);
                if (parameterType.Equals(rewrittenOperand.Type.GetNullableUnderlyingType(), TypeCompareKind.AllIgnoreOptions) &&
                    !parameterType.IsNullableType() &&
                    parameterType.IsValueType)
                {
                    return RewriteLiftedUserDefinedConversion(syntax, rewrittenOperand, conversion, @checked: @checked, rewrittenType);
                }
            }

            // do not rewrite user defined conversion in expression trees
            if (_inExpressionLambda)
            {
                return BoundConversion.Synthesized(
                    syntax, rewrittenOperand, conversion, @checked: @checked, explicitCastInCode: true,
                    conversionGroupOpt: null, InConversionGroupFlags.LoweredFormOfUserDefinedConversionForExpressionTree, constantValueOpt: null, rewrittenType);
            }

            if ((rewrittenOperand.Type.IsArray()) && _compilation.IsReadOnlySpanType(rewrittenType))
            {
                return new BoundReadOnlySpanFromArray(syntax, rewrittenOperand, conversion.Method, rewrittenType) { WasCompilerGenerated = true };
            }

            var constrainedToTypeOpt = conversion.ConstrainedToTypeOpt;
            BoundExpression result = BoundCall.Synthesized(
                syntax,
                receiverOpt: constrainedToTypeOpt is null ? null : new BoundTypeExpression(syntax, aliasOpt: null, constrainedToTypeOpt),
                initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown,
                conversion.Method,
                rewrittenOperand);
            Debug.Assert(TypeSymbol.Equals(result.Type, rewrittenType, TypeCompareKind.ConsiderEverything2));
            return result;
        }

        private BoundExpression MakeLiftedUserDefinedConversionConsequence(BoundCall call, TypeSymbol resultType)
        {
            if (call.Method.ReturnType.IsValidNullableTypeArgument())
            {
                Debug.Assert(resultType.IsNullableType() && TypeSymbol.Equals(resultType.GetNullableUnderlyingType(), call.Method.ReturnType, TypeCompareKind.ConsiderEverything2));
                MethodSymbol ctor = UnsafeGetNullableMethod(call.Syntax, resultType, SpecialMember.System_Nullable_T__ctor);
                return new BoundObjectCreationExpression(call.Syntax, ctor, call);
            }

            return call;
        }

        private BoundExpression RewriteLiftedUserDefinedConversion(
            SyntaxNode syntax,
            BoundExpression rewrittenOperand,
            Conversion conversion,
            bool @checked,
            TypeSymbol rewrittenType)
        {
            Debug.Assert(rewrittenOperand.Type is { });
            if (_inExpressionLambda)
            {
                Conversion conv = TryMakeConversion(syntax, conversion, rewrittenOperand.Type, rewrittenType, @checked: @checked);
                return BoundConversion.Synthesized(
                    syntax, rewrittenOperand, conv, @checked: @checked, explicitCastInCode: true,
                    conversionGroupOpt: null, InConversionGroupFlags.LoweredFormOfUserDefinedConversionForExpressionTree, constantValueOpt: null, rewrittenType);
            }

            // DELIBERATE SPEC VIOLATION:
            // The native compiler allows for a "lifted" conversion even when the return type of the conversion
            // not a non-nullable value type. For example, if we have a conversion from struct S to string,
            // then a "lifted" conversion from S? to string is considered by the native compiler to exist,
            // with the semantics of "s.HasValue ? (string)s.Value : (string)null".  The Roslyn compiler
            // perpetuates this error for the sake of backwards compatibility.

            Debug.Assert((object)rewrittenType != null);
            Debug.Assert(rewrittenOperand.Type.IsNullableType());

            BoundExpression? optimized = OptimizeLiftedUserDefinedConversion(syntax, rewrittenOperand, conversion, rewrittenType);
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
            MethodSymbol getValueOrDefault = UnsafeGetNullableMethod(syntax, boundTemp.Type, SpecialMember.System_Nullable_T_GetValueOrDefault);

            // temp.HasValue
            BoundExpression condition = _factory.MakeNullableHasValue(syntax, boundTemp);

            // temp.GetValueOrDefault()
            BoundCall callGetValueOrDefault = BoundCall.Synthesized(syntax, boundTemp, initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown, getValueOrDefault);

            // op_Whatever(temp.GetValueOrDefault())
            Debug.Assert(conversion.Method is { });
            var constrainedToTypeOpt = conversion.ConstrainedToTypeOpt;
            BoundCall userDefinedCall = BoundCall.Synthesized(
                syntax,
                receiverOpt: constrainedToTypeOpt is null ? null : new BoundTypeExpression(syntax, aliasOpt: null, constrainedToTypeOpt),
                initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown,
                conversion.Method,
                callGetValueOrDefault);

            // new R?(op_Whatever(temp.GetValueOrDefault())
            BoundExpression consequence = MakeLiftedUserDefinedConversionConsequence(userDefinedCall, rewrittenType);

            // default(R?)
            BoundExpression alternative = new BoundDefaultExpression(syntax, rewrittenType);

            // temp.HasValue ? new R?(op_Whatever(temp.GetValueOrDefault())) : default(R?)
            BoundExpression conditionalExpression = RewriteConditionalOperator(
                syntax: syntax,
                rewrittenCondition: condition,
                rewrittenConsequence: consequence,
                rewrittenAlternative: alternative,
                constantValueOpt: null,
                rewrittenType: rewrittenType,
                isRef: false);

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
            SyntaxNode syntax,
            BoundExpression rewrittenOperand,
            Conversion conversion,
            bool @checked,
            bool explicitCastInCode,
            ConstantValue? constantValueOpt,
            TypeSymbol rewrittenType)
        {
            Debug.Assert(rewrittenOperand != null);
            Debug.Assert((object)rewrittenType != null);
            Debug.Assert(rewrittenOperand.Type is { });
            Debug.Assert(!_compilation.Assembly.RuntimeSupportsNumericIntPtr);

            TypeSymbol source = rewrittenOperand.Type;
            TypeSymbol target = rewrittenType;

            SpecialMember member = GetIntPtrConversionMethod(source: source, target: rewrittenType);
            MethodSymbol method;

            if (!TryGetSpecialTypeMethod(syntax, member, out method))
            {
                return BadExpression(syntax, rewrittenType, rewrittenOperand);
            }

            Debug.Assert(!method.ReturnsVoid);
            Debug.Assert(method.ParameterCount == 1);

            conversion = conversion.SetConversionMethod(method);

            if (source.IsNullableType() && target.IsNullableType())
            {
                Debug.Assert(target.IsNullableType());
                return RewriteLiftedUserDefinedConversion(syntax, rewrittenOperand, conversion, @checked: @checked, rewrittenType);
            }
            else if (source.IsNullableType())
            {
                rewrittenOperand = MakeConversionNode(rewrittenOperand, source.StrippedType(), @checked, markAsChecked: true);
            }

            rewrittenOperand = MakeConversionNode(rewrittenOperand, method.GetParameterType(0), @checked);

            if (_inExpressionLambda)
            {
                return BoundConversion.Synthesized(syntax, rewrittenOperand, conversion, @checked, explicitCastInCode: explicitCastInCode, conversionGroupOpt: null, InConversionGroupFlags.Unspecified, constantValueOpt, rewrittenType);
            }

            var rewrittenCall = MakeCall(
                    syntax: syntax,
                    rewrittenReceiver: null,
                    method: method,
                    rewrittenArguments: ImmutableArray.Create(rewrittenOperand));

            return MakeConversionNode(rewrittenCall, rewrittenType, @checked, markAsChecked: true);
        }

        public static SpecialMember GetIntPtrConversionMethod(TypeSymbol source, TypeSymbol target)
        {
            Debug.Assert((object)source != null);
            Debug.Assert((object)target != null);

            TypeSymbol t0 = target.StrippedType();
            TypeSymbol s0 = source.StrippedType();

            SpecialType t0Type = t0.IsEnumType() ? t0.GetEnumUnderlyingType()!.SpecialType : t0.SpecialType;
            SpecialType s0Type = s0.IsEnumType() ? s0.GetEnumUnderlyingType()!.SpecialType : s0.SpecialType;

            if (t0Type == SpecialType.System_IntPtr)
            {
                if (source.IsPointerOrFunctionPointer())
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
                if (source.IsPointerOrFunctionPointer())
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
                if (target.IsPointerOrFunctionPointer())
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
                if (target.IsPointerOrFunctionPointer())
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

            throw ExceptionUtilities.Unreachable();
        }

        // https://github.com/dotnet/roslyn/issues/42452: Test with native integers and expression trees.
        private static SpecialMember DecimalConversionMethod(TypeSymbol typeFrom, TypeSymbol typeTo)
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

        private BoundExpression RewriteDecimalConversion(SyntaxNode syntax, BoundExpression operand, TypeSymbol fromType, TypeSymbol toType, bool @checked, bool isImplicit, ConstantValue? constantValueOpt)
        {
            Debug.Assert(fromType.SpecialType == SpecialType.System_Decimal || toType.SpecialType == SpecialType.System_Decimal);

            if (fromType.SpecialType == SpecialType.System_Decimal)
            {
                switch (toType.SpecialType)
                {
                    case SpecialType.System_IntPtr:
                    case SpecialType.System_UIntPtr:
                        operand = RewriteDecimalConversionCore(syntax, operand, fromType, get64BitType(_compilation, signed: toType.SpecialType == SpecialType.System_IntPtr), isImplicit, constantValueOpt);
                        return MakeConversionNode(operand, toType, @checked);
                }
            }
            else
            {
                switch (fromType.SpecialType)
                {
                    case SpecialType.System_IntPtr:
                    case SpecialType.System_UIntPtr:
                        operand = MakeConversionNode(operand, get64BitType(_compilation, signed: fromType.SpecialType == SpecialType.System_IntPtr), @checked);
                        Debug.Assert(operand.Type is { });
                        return RewriteDecimalConversionCore(syntax, operand, operand.Type, toType, isImplicit, constantValueOpt);
                }
            }

            return RewriteDecimalConversionCore(syntax, operand, fromType, toType, isImplicit, constantValueOpt);

            static TypeSymbol get64BitType(CSharpCompilation compilation, bool signed) => compilation.GetSpecialType(signed ? SpecialType.System_Int64 : SpecialType.System_UInt64);
        }

        private BoundExpression RewriteDecimalConversionCore(SyntaxNode syntax, BoundExpression operand, TypeSymbol fromType, TypeSymbol toType, bool isImplicit, ConstantValue? constantValueOpt)
        {
            // call the method
            SpecialMember member = DecimalConversionMethod(fromType, toType);
            if (!TryGetSpecialTypeMethod(syntax, member, out MethodSymbol method))
            {
                return BadExpression(syntax, toType, operand);
            }

            if (_inExpressionLambda)
            {
                ConversionKind conversionKind = isImplicit ? ConversionKind.ImplicitUserDefined : ConversionKind.ExplicitUserDefined;
                var conversion = new Conversion(conversionKind, method, isExtensionMethod: false);

                return new BoundConversion(
                    syntax, operand, conversion, @checked: false, explicitCastInCode: false,
                    conversionGroupOpt: null, InConversionGroupFlags.LoweredFormOfUserDefinedConversionForExpressionTree,
                    constantValueOpt: constantValueOpt, type: toType);
            }
            else
            {
                Debug.Assert(TypeSymbol.Equals(method.ReturnType, toType, TypeCompareKind.ConsiderEverything2));
                return BoundCall.Synthesized(syntax, receiverOpt: null, initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown, method, operand);
            }
        }

        /// <summary>
        /// Reports diagnostics and returns Conversion.NoConversion in case of missing runtime helpers.
        /// </summary>
        private Conversion TryMakeConversion(SyntaxNode syntax, Conversion conversion, TypeSymbol fromType, TypeSymbol toType, bool @checked)
        {
            switch (conversion.Kind)
            {
                case ConversionKind.ExplicitUserDefined:
                case ConversionKind.ImplicitUserDefined:
                    {
                        conversion.AssertUnderlyingConversionsChecked();
                        var meth = conversion.Method;
                        Debug.Assert(meth is { });
                        Conversion fromConversion = TryMakeConversion(syntax, conversion.UserDefinedFromConversion, fromType, meth.Parameters[0].Type, @checked: @checked);
                        if (!fromConversion.Exists)
                        {
                            return Conversion.NoConversion;
                        }

                        Conversion toConversion = TryMakeConversion(syntax, conversion.UserDefinedToConversion, meth.ReturnType, toType, @checked: @checked);
                        if (!toConversion.Exists)
                        {
                            return Conversion.NoConversion;
                        }

                        if (fromConversion == conversion.UserDefinedFromConversion && toConversion == conversion.UserDefinedToConversion)
                        {
                            return conversion;
                        }
                        else
                        {
                            // TODO: how do we distinguish from normal and lifted conversions here?
                            var analysis = UserDefinedConversionAnalysis.Normal(conversion.ConstrainedToTypeOpt, meth, fromConversion, toConversion, fromType, toType);
                            var result = UserDefinedConversionResult.Valid(ImmutableArray.Create<UserDefinedConversionAnalysis>(analysis), 0);
                            var resultConversion = new Conversion(result, conversion.IsImplicit);
                            resultConversion.MarkUnderlyingConversionsChecked();
                            return resultConversion;
                        }
                    }
                case ConversionKind.IntPtr:
                    {
                        Debug.Assert(!_compilation.Assembly.RuntimeSupportsNumericIntPtr);
                        SpecialMember member = GetIntPtrConversionMethod(fromType, toType);
                        MethodSymbol method;
                        if (!TryGetSpecialTypeMethod(syntax, member, out method))
                        {
                            return Conversion.NoConversion;
                        }

                        return TryMakeUserDefinedConversion(syntax, method, fromType, toType, @checked: @checked, conversion.IsImplicit);
                    }
                case ConversionKind.ImplicitNumeric:
                case ConversionKind.ExplicitNumeric:
                    // TODO: what about nullable?
                    if (fromType.SpecialType == SpecialType.System_Decimal || toType.SpecialType == SpecialType.System_Decimal)
                    {
                        SpecialMember member = DecimalConversionMethod(fromType, toType);
                        MethodSymbol method;
                        if (!TryGetSpecialTypeMethod(syntax, member, out method))
                        {
                            return Conversion.NoConversion;
                        }

                        return TryMakeUserDefinedConversion(syntax, method, fromType, toType, @checked: @checked, conversion.IsImplicit);
                    }
                    return conversion;
                case ConversionKind.ImplicitEnumeration:
                case ConversionKind.ExplicitEnumeration:
                    // TODO: what about nullable?
                    if (fromType.SpecialType == SpecialType.System_Decimal)
                    {
                        var underlying = toType.GetEnumUnderlyingType();
                        Debug.Assert(underlying is { });
                        SpecialMember member = DecimalConversionMethod(fromType, underlying);
                        MethodSymbol method;
                        if (!TryGetSpecialTypeMethod(syntax, member, out method))
                        {
                            return Conversion.NoConversion;
                        }

                        return TryMakeUserDefinedConversion(syntax, method, fromType, toType, @checked: @checked, conversion.IsImplicit);
                    }
                    else if (toType.SpecialType == SpecialType.System_Decimal)
                    {
                        var underlying = fromType.GetEnumUnderlyingType();
                        Debug.Assert(underlying is { });
                        SpecialMember member = DecimalConversionMethod(underlying, toType);
                        MethodSymbol method;
                        if (!TryGetSpecialTypeMethod(syntax, member, out method))
                        {
                            return Conversion.NoConversion;
                        }

                        return TryMakeUserDefinedConversion(syntax, method, fromType, toType, @checked: @checked, conversion.IsImplicit);
                    }
                    return conversion;
                default:
                    return conversion;
            }
        }

        /// <summary>
        /// Reports diagnostics and returns Conversion.NoConversion in case of missing runtime helpers.
        /// </summary>
        private Conversion TryMakeConversion(SyntaxNode syntax, TypeSymbol fromType, TypeSymbol toType, bool @checked)
        {
            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo();
            var result = TryMakeConversion(syntax, _compilation.Conversions.ClassifyConversionFromType(fromType, toType, isChecked: @checked, ref useSiteInfo), fromType, toType, @checked: @checked);
            _diagnostics.Add(syntax, useSiteInfo);
            return result;
        }

        /// <summary>
        /// Reports diagnostics and returns Conversion.NoConversion in case of missing runtime helpers.
        /// </summary>
        private Conversion TryMakeUserDefinedConversion(SyntaxNode syntax, MethodSymbol meth, TypeSymbol fromType, TypeSymbol toType, bool @checked, bool isImplicit)
        {
            Debug.Assert(!meth.ContainingType.IsInterface);

            Conversion fromConversion = TryMakeConversion(syntax, fromType, meth.Parameters[0].Type, @checked: @checked);
            if (!fromConversion.Exists)
            {
                return Conversion.NoConversion;
            }

            Conversion toConversion = TryMakeConversion(syntax, meth.ReturnType, toType, @checked: @checked);
            if (!toConversion.Exists)
            {
                return Conversion.NoConversion;
            }

            // TODO: distinguish between normal and lifted conversions here
            var analysis = UserDefinedConversionAnalysis.Normal(constrainedToTypeOpt: null, meth, fromConversion, toConversion, fromType, toType);
            var result = UserDefinedConversionResult.Valid(ImmutableArray.Create<UserDefinedConversionAnalysis>(analysis), 0);
            return new Conversion(result, isImplicit);
        }
    }
}
