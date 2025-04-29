// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class Binder
    {
        internal BoundExpression CreateConversion(
            BoundExpression source,
            TypeSymbol destination,
            BindingDiagnosticBag diagnostics)
        {
            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
            var conversion = Conversions.ClassifyConversionFromExpression(source, destination, isChecked: CheckOverflowAtRuntime, ref useSiteInfo);

            diagnostics.Add(source.Syntax, useSiteInfo);
            return CreateConversion(source.Syntax, source, conversion, isCast: false, conversionGroupOpt: null, destination: destination, diagnostics: diagnostics);
        }

        internal BoundExpression CreateConversion(
            BoundExpression source,
            Conversion conversion,
            TypeSymbol destination,
            BindingDiagnosticBag diagnostics)
        {
            return CreateConversion(source.Syntax, source, conversion, isCast: false, conversionGroupOpt: null, destination: destination, diagnostics: diagnostics);
        }

        internal BoundExpression CreateConversion(
            SyntaxNode syntax,
            BoundExpression source,
            Conversion conversion,
            bool isCast,
            ConversionGroup? conversionGroupOpt,
            TypeSymbol destination,
            BindingDiagnosticBag diagnostics)
        {
            return CreateConversion(syntax, source, conversion, isCast: isCast, conversionGroupOpt, source.WasCompilerGenerated, destination, diagnostics);
        }

        protected BoundExpression CreateConversion(
            SyntaxNode syntax,
            BoundExpression source,
            Conversion conversion,
            bool isCast,
            ConversionGroup? conversionGroupOpt,
            bool wasCompilerGenerated,
            TypeSymbol destination,
            BindingDiagnosticBag diagnostics,
            bool hasErrors = false)
        {

            var result = createConversion(syntax, source, conversion, isCast, conversionGroupOpt, wasCompilerGenerated, destination, diagnostics, hasErrors);

            Debug.Assert(result is BoundConversion || (conversion.IsIdentity && ((object)result == source) || source.NeedsToBeConverted()) || hasErrors);

#if DEBUG
            if (source is BoundValuePlaceholder placeholder1)
            {
                Debug.Assert(filterConversion(conversion));
                Debug.Assert(BoundNode.GetConversion(result, placeholder1) == conversion);
            }
            else if (source.Type is not null && filterConversion(conversion))
            {
                var placeholder2 = new BoundValuePlaceholder(source.Syntax, source.Type);
                var result2 = createConversion(syntax, placeholder2, conversion, isCast, conversionGroupOpt, wasCompilerGenerated, destination, BindingDiagnosticBag.Discarded, hasErrors);
                Debug.Assert(BoundNode.GetConversion(result2, placeholder2) == conversion);
            }

            static bool filterConversion(Conversion conversion)
            {
                return !conversion.IsInterpolatedString &&
                       !conversion.IsInterpolatedStringHandler &&
                       !conversion.IsSwitchExpression &&
                       !conversion.IsCollectionExpression &&
                       !(conversion.IsTupleLiteralConversion || (conversion.IsNullable && conversion.UnderlyingConversions[0].IsTupleLiteralConversion)) &&
                       (!conversion.IsUserDefined || filterConversion(conversion.UserDefinedFromConversion));
            }
#endif

            return result;

            BoundExpression createConversion(
                SyntaxNode syntax,
                BoundExpression source,
                Conversion conversion,
                bool isCast,
                ConversionGroup? conversionGroupOpt,
                bool wasCompilerGenerated,
                TypeSymbol destination,
                BindingDiagnosticBag diagnostics,
                bool hasErrors = false)
            {
                RoslynDebug.Assert(source != null);
                RoslynDebug.Assert((object)destination != null);
                RoslynDebug.Assert(!isCast || conversionGroupOpt != null || wasCompilerGenerated);

                if (conversion.IsIdentity)
                {
                    if (source is BoundTupleLiteral sourceTuple)
                    {
                        NamedTypeSymbol.ReportTupleNamesMismatchesIfAny(destination, sourceTuple, diagnostics);
                    }

                    // identity tuple and switch conversions result in a converted expression
                    // to indicate that such conversions are no longer applicable.
                    source = BindToNaturalType(source, diagnostics);
                    RoslynDebug.Assert(source.Type is object);

                    // We need to preserve any conversion that changes the type (even identity conversions, like object->dynamic),
                    // or that was explicitly written in code (so that GetSemanticInfo can find the syntax in the bound tree).
                    if (!isCast && source.Type.Equals(destination, TypeCompareKind.IgnoreNullableModifiersForReferenceTypes))
                    {
                        return source;
                    }
                }

                if (conversion.IsMethodGroup)
                {
                    return CreateMethodGroupConversion(syntax, source, conversion, isCast: isCast, conversionGroupOpt, destination, diagnostics);
                }

                // Obsolete diagnostics for method group are reported as part of creating the method group conversion.
                reportUseSiteDiagnostics(syntax, conversion, source, destination, diagnostics);

                if (conversion.IsAnonymousFunction && source.Kind == BoundKind.UnboundLambda)
                {
                    return CreateAnonymousFunctionConversion(syntax, source, conversion, isCast: isCast, conversionGroupOpt, destination, diagnostics);
                }

                if (conversion.Kind == ConversionKind.FunctionType)
                {
                    return CreateFunctionTypeConversion(syntax, source, conversion, isCast: isCast, conversionGroupOpt, destination, diagnostics);
                }

                if (conversion.IsStackAlloc)
                {
                    return CreateStackAllocConversion(syntax, source, conversion, isCast, conversionGroupOpt, destination, diagnostics);
                }

                if (conversion.IsTupleLiteralConversion ||
                    (conversion.IsNullable && conversion.UnderlyingConversions[0].IsTupleLiteralConversion))
                {
                    return CreateTupleLiteralConversion(syntax, (BoundTupleLiteral)source, conversion, isCast: isCast, conversionGroupOpt, destination, diagnostics);
                }

                if (conversion.Kind == ConversionKind.SwitchExpression)
                {
                    var convertedSwitch = ConvertSwitchExpression((BoundUnconvertedSwitchExpression)source, destination, conversionIfTargetTyped: conversion, diagnostics);
                    return new BoundConversion(
                        syntax,
                        convertedSwitch,
                        conversion,
                        CheckOverflowAtRuntime,
                        explicitCastInCode: isCast && !wasCompilerGenerated,
                        conversionGroupOpt,
                        convertedSwitch.ConstantValueOpt,
                        destination,
                        hasErrors);
                }

                if (conversion.Kind == ConversionKind.ConditionalExpression)
                {
                    var convertedConditional = ConvertConditionalExpression((BoundUnconvertedConditionalOperator)source, destination, conversionIfTargetTyped: conversion, diagnostics);
                    return new BoundConversion(
                        syntax,
                        convertedConditional,
                        conversion,
                        CheckOverflowAtRuntime,
                        explicitCastInCode: isCast && !wasCompilerGenerated,
                        conversionGroupOpt,
                        convertedConditional.ConstantValueOpt,
                        destination,
                        hasErrors);
                }

                if (conversion.Kind == ConversionKind.InterpolatedString)
                {
                    Debug.Assert(destination.SpecialType != SpecialType.System_String);
                    var unconvertedSource = (BoundUnconvertedInterpolatedString)source;
                    source = BindUnconvertedInterpolatedExpressionToFormattableStringFactory(unconvertedSource, destination, diagnostics);
                }

                if (conversion.Kind == ConversionKind.InterpolatedStringHandler)
                {
                    return new BoundConversion(
                        syntax,
                        BindUnconvertedInterpolatedExpressionToHandlerType(source, (NamedTypeSymbol)destination, diagnostics),
                        conversion,
                        @checked: CheckOverflowAtRuntime,
                        explicitCastInCode: isCast && !wasCompilerGenerated,
                        conversionGroupOpt,
                        constantValueOpt: null,
                        destination);
                }

                if (source.Kind == BoundKind.UnconvertedSwitchExpression)
                {
                    TypeSymbol? type = source.Type;
                    if (type is null)
                    {
                        Debug.Assert(!conversion.Exists);
                        type = CreateErrorType();
                        hasErrors = true;
                    }

                    source = ConvertSwitchExpression((BoundUnconvertedSwitchExpression)source, type, conversionIfTargetTyped: null, diagnostics, hasErrors);
                    if (destination.Equals(type, TypeCompareKind.ConsiderEverything) && wasCompilerGenerated)
                    {
                        return source;
                    }
                }

                if (conversion.IsObjectCreation)
                {
                    return ConvertObjectCreationExpression(syntax, (BoundUnconvertedObjectCreationExpression)source, conversion, isCast, destination, conversionGroupOpt, wasCompilerGenerated, diagnostics);
                }

                if (source.Kind == BoundKind.UnconvertedCollectionExpression)
                {
                    Debug.Assert(conversion.IsCollectionExpression
                        || (conversion.IsNullable && conversion.UnderlyingConversions[0].IsCollectionExpression)
                        || !conversion.Exists);

                    var collectionExpression = ConvertCollectionExpression(
                        (BoundUnconvertedCollectionExpression)source,
                        destination,
                        conversion,
                        diagnostics);
                    return new BoundConversion(
                        syntax,
                        collectionExpression,
                        conversion,
                        @checked: CheckOverflowAtRuntime,
                        explicitCastInCode: isCast && !wasCompilerGenerated,
                        conversionGroupOpt,
                        constantValueOpt: null,
                        type: destination);
                }

                if (source.Kind == BoundKind.UnconvertedConditionalOperator)
                {
                    Debug.Assert(source.Type is null);
                    Debug.Assert(!conversion.Exists);
                    hasErrors = true;

                    source = ConvertConditionalExpression((BoundUnconvertedConditionalOperator)source, CreateErrorType(), conversionIfTargetTyped: null, diagnostics, hasErrors);
                }

                if (conversion.IsUserDefined)
                {
                    // User-defined conversions are likely to be represented as multiple
                    // BoundConversion instances so a ConversionGroup is necessary.
                    return CreateUserDefinedConversion(syntax, source, conversion, isCast: isCast, conversionGroupOpt ?? new ConversionGroup(conversion), destination, diagnostics, hasErrors);
                }

                ConstantValue? constantValue = this.FoldConstantConversion(syntax, source, conversion, destination, diagnostics);
                if (conversion.Kind == ConversionKind.DefaultLiteral)
                {
                    source = new BoundDefaultExpression(source.Syntax, targetType: null, constantValue, type: destination)
                        .WithSuppression(source.IsSuppressed);
                }

                if (!hasErrors && conversion.Exists)
                {
                    ensureAllUnderlyingConversionsChecked(syntax, source, conversion, wasCompilerGenerated, destination, diagnostics);

                    if (conversion.Kind is ConversionKind.ImplicitReference or ConversionKind.ExplicitReference &&
                        source.Type is { } sourceType &&
                        sourceType.IsWellKnownTypeLock())
                    {
                        diagnostics.Add(ErrorCode.WRN_ConvertingLock, source.Syntax);
                    }
                }

                return new BoundConversion(
                    syntax,
                    BindToNaturalType(source, diagnostics),
                    conversion,
                    @checked: CheckOverflowAtRuntime,
                    explicitCastInCode: isCast && !wasCompilerGenerated,
                    conversionGroupOpt,
                    constantValueOpt: constantValue,
                    type: destination,
                    hasErrors: hasErrors)
                { WasCompilerGenerated = wasCompilerGenerated };

                void reportUseSiteDiagnostics(SyntaxNode syntax, Conversion conversion, BoundExpression source, TypeSymbol destination, BindingDiagnosticBag diagnostics)
                {
                    // Obsolete diagnostics for method group are reported as part of creating the method group conversion.
                    Debug.Assert(!conversion.IsMethodGroup);
                    ReportDiagnosticsIfObsolete(diagnostics, conversion, syntax, hasBaseReceiver: false);
                    if (conversion.Method is not null)
                    {
                        ReportUseSite(conversion.Method, diagnostics, syntax.Location);
                    }

                    checkConstraintLanguageVersionAndRuntimeSupportForConversion(syntax, conversion, source, destination, diagnostics);
                }
            }

            void ensureAllUnderlyingConversionsChecked(SyntaxNode syntax, BoundExpression source, Conversion conversion, bool wasCompilerGenerated, TypeSymbol destination, BindingDiagnosticBag diagnostics)
            {
                if (conversion.IsNullable)
                {
                    Debug.Assert(conversion.UnderlyingConversions.Length == 1);

                    if (destination.IsNullableType())
                    {
                        switch (source.Type?.IsNullableType())
                        {
                            case true:
                                _ = CreateConversion(
                                        syntax,
                                        new BoundValuePlaceholder(source.Syntax, source.Type.GetNullableUnderlyingType()),
                                        conversion.UnderlyingConversions[0],
                                        isCast: false,
                                        conversionGroupOpt: null,
                                        wasCompilerGenerated,
                                        destination.GetNullableUnderlyingType(),
                                        diagnostics);
                                break;

                            case false:
                                _ = CreateConversion(
                                        syntax,
                                        source,
                                        conversion.UnderlyingConversions[0],
                                        isCast: false,
                                        conversionGroupOpt: null,
                                        wasCompilerGenerated,
                                        destination.GetNullableUnderlyingType(),
                                        diagnostics);
                                break;
                        }

                        conversion.UnderlyingConversions[0].AssertUnderlyingConversionsChecked();
                        conversion.MarkUnderlyingConversionsChecked();
                    }
                    else if (source.Type?.IsNullableType() == true)
                    {
                        _ = CreateConversion(
                                syntax,
                                new BoundValuePlaceholder(source.Syntax, source.Type.GetNullableUnderlyingType()),
                                conversion.UnderlyingConversions[0],
                                isCast: false,
                                conversionGroupOpt: null,
                                wasCompilerGenerated,
                                destination,
                                diagnostics);

                        conversion.UnderlyingConversions[0].AssertUnderlyingConversionsChecked();
                        conversion.MarkUnderlyingConversionsChecked();
                    }
                }
                else if (conversion.IsTupleConversion)
                {
                    ImmutableArray<TypeWithAnnotations> sourceTypes;
                    ImmutableArray<TypeWithAnnotations> destTypes;

                    if (source.Type?.TryGetElementTypesWithAnnotationsIfTupleType(out sourceTypes) == true &&
                        destination.TryGetElementTypesWithAnnotationsIfTupleType(out destTypes) &&
                        sourceTypes.Length == destTypes.Length)
                    {
                        var elementConversions = conversion.UnderlyingConversions;
                        Debug.Assert(elementConversions.Length == sourceTypes.Length);

                        for (int i = 0; i < sourceTypes.Length; i++)
                        {
                            _ = CreateConversion(
                                    syntax,
                                    new BoundValuePlaceholder(source.Syntax, sourceTypes[i].Type),
                                    elementConversions[i],
                                    isCast: false,
                                    conversionGroupOpt: null,
                                    wasCompilerGenerated,
                                    destTypes[i].Type,
                                    diagnostics);

                            elementConversions[i].AssertUnderlyingConversionsChecked();
                        }

                        conversion.MarkUnderlyingConversionsChecked();
                    }
                }
                else if (conversion.IsDynamic)
                {
                    Debug.Assert(conversion.UnderlyingConversions.IsDefault);
                    conversion.MarkUnderlyingConversionsChecked();
                }

                conversion.AssertUnderlyingConversionsCheckedRecursive();
            }

            void checkConstraintLanguageVersionAndRuntimeSupportForConversion(SyntaxNode syntax, Conversion conversion, BoundExpression source, TypeSymbol destination, BindingDiagnosticBag diagnostics)
            {
                Debug.Assert(syntax.SyntaxTree is object);

                if (conversion.IsUserDefined)
                {
                    if (conversion.Method is MethodSymbol method && method.IsStatic)
                    {
                        if (method.IsAbstract || method.IsVirtual)
                        {
                            Debug.Assert(conversion.ConstrainedToTypeOpt is TypeParameterSymbol);

                            if (Compilation.SourceModule != method.ContainingModule)
                            {
                                CheckFeatureAvailability(syntax, MessageID.IDS_FeatureStaticAbstractMembersInInterfaces, diagnostics);

                                if (!Compilation.Assembly.RuntimeSupportsStaticAbstractMembersInInterfaces)
                                {
                                    Error(diagnostics, ErrorCode.ERR_RuntimeDoesNotSupportStaticAbstractMembersInInterfaces, syntax);
                                }
                            }
                        }

                        if (SyntaxFacts.IsCheckedOperator(method.Name) &&
                            Compilation.SourceModule != method.ContainingModule)
                        {
                            CheckFeatureAvailability(syntax, MessageID.IDS_FeatureCheckedUserDefinedOperators, diagnostics);
                        }
                    }
                }
                else if (conversion.IsInlineArray)
                {
                    if (!Compilation.Assembly.RuntimeSupportsInlineArrayTypes)
                    {
                        Error(diagnostics, ErrorCode.ERR_RuntimeDoesNotSupportInlineArrayTypes, syntax);
                    }

                    CheckFeatureAvailability(syntax, MessageID.IDS_FeatureInlineArrays, diagnostics);

                    Debug.Assert(source.Type is { });

                    FieldSymbol? elementField = source.Type.TryGetInlineArrayElementField();
                    Debug.Assert(elementField is { });

                    diagnostics.ReportUseSite(elementField, syntax);

                    if (destination.OriginalDefinition.Equals(Compilation.GetWellKnownType(WellKnownType.System_ReadOnlySpan_T), TypeCompareKind.AllIgnoreOptions))
                    {
                        if (CheckValueKind(syntax, source, BindValueKind.RefersToLocation, checkingReceiver: false, BindingDiagnosticBag.Discarded))
                        {
                            _ = GetWellKnownTypeMember(WellKnownMember.System_Runtime_InteropServices_MemoryMarshal__CreateReadOnlySpan, diagnostics, syntax: syntax); // This also takes care of an 'int' type
                            _ = GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_Unsafe__AsRef_T, diagnostics, syntax: syntax);
                            _ = GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_Unsafe__As_T, diagnostics, syntax: syntax);
                        }
                        else
                        {
                            Error(diagnostics, ErrorCode.ERR_InlineArrayConversionToReadOnlySpanNotSupported, syntax, destination);
                        }
                    }
                    else
                    {
                        Debug.Assert(destination.OriginalDefinition.Equals(Compilation.GetWellKnownType(WellKnownType.System_Span_T), TypeCompareKind.AllIgnoreOptions));

                        if (CheckValueKind(syntax, source, BindValueKind.RefersToLocation | BindValueKind.Assignable, checkingReceiver: false, BindingDiagnosticBag.Discarded))
                        {
                            _ = GetWellKnownTypeMember(WellKnownMember.System_Runtime_InteropServices_MemoryMarshal__CreateSpan, diagnostics, syntax: syntax); // This also takes care of an 'int' type
                            _ = GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_Unsafe__As_T, diagnostics, syntax: syntax);
                        }
                        else
                        {
                            Error(diagnostics, ErrorCode.ERR_InlineArrayConversionToSpanNotSupported, syntax, destination);
                        }
                    }

                    CheckInlineArrayTypeIsSupported(syntax, source.Type, elementField.Type, diagnostics);
                }
                else if (conversion.IsSpan)
                {
                    Debug.Assert(source.Type is not null);
                    Debug.Assert(destination.IsSpan() || destination.IsReadOnlySpan());

                    CheckFeatureAvailability(syntax, MessageID.IDS_FeatureFirstClassSpan, diagnostics);

                    // NOTE: We cannot use well-known members because per the spec
                    // the Span types involved in the Span conversions can be any that match the type name.

                    // Span<T>.op_Implicit(T[]) or ReadOnlySpan<T>.op_Implicit(T[])
                    if (source.Type is ArrayTypeSymbol)
                    {
                        reportUseSiteOrMissing(
                            TryFindImplicitOperatorFromArray(destination.OriginalDefinition),
                            destination.OriginalDefinition,
                            WellKnownMemberNames.ImplicitConversionName,
                            syntax,
                            diagnostics);
                    }

                    // ReadOnlySpan<T> Span<T>.op_Implicit(Span<T>)
                    if (source.Type.IsSpan())
                    {
                        Debug.Assert(destination.IsReadOnlySpan());
                        reportUseSiteOrMissing(
                            TryFindImplicitOperatorFromSpan(source.Type.OriginalDefinition, destination.OriginalDefinition),
                            source.Type.OriginalDefinition,
                            WellKnownMemberNames.ImplicitConversionName,
                            syntax,
                            diagnostics);
                    }

                    // ReadOnlySpan<T> ReadOnlySpan<T>.CastUp<TDerived>(ReadOnlySpan<TDerived>)
                    if (source.Type.IsSpan() || source.Type.IsReadOnlySpan())
                    {
                        Debug.Assert(destination.IsReadOnlySpan());
                        if (NeedsSpanCastUp(source.Type, destination))
                        {
                            // If converting Span<TDerived> -> ROS<TDerived> -> ROS<T>,
                            // the source of the CastUp is the return type of the op_Implicit (i.e., the ROS<TDerived>)
                            // which has the same original definition as the destination ROS<T>.
                            TypeSymbol sourceForCastUp = source.Type.IsSpan()
                                ? destination.OriginalDefinition
                                : source.Type.OriginalDefinition;

                            MethodSymbol? castUpMethod = TryFindCastUpMethod(sourceForCastUp, destination.OriginalDefinition);
                            reportUseSiteOrMissing(
                                castUpMethod,
                                destination.OriginalDefinition,
                                WellKnownMemberNames.CastUpMethodName,
                                syntax,
                                diagnostics);
                            castUpMethod?
                                .AsMember((NamedTypeSymbol)destination)
                                .Construct([((NamedTypeSymbol)source.Type).TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0]])
                                .CheckConstraints(new ConstraintsHelper.CheckConstraintsArgs(Compilation, Conversions, includeNullability: false, syntax.Location, diagnostics));
                        }
                    }

                    // ReadOnlySpan<char> MemoryExtensions.AsSpan(string)
                    if (source.Type.IsStringType())
                    {
                        reportUseSiteOrMissing(
                            TryFindAsSpanCharMethod(Compilation, destination),
                            WellKnownMemberNames.MemoryExtensionsTypeFullName,
                            WellKnownMemberNames.AsSpanMethodName,
                            syntax,
                            diagnostics);
                    }
                }
            }

            static void reportUseSiteOrMissing(MethodSymbol? method, object containingType, string methodName, SyntaxNode syntax, BindingDiagnosticBag diagnostics)
            {
                if (method is not null)
                {
                    diagnostics.ReportUseSite(method, syntax);
                }
                else
                {
                    Error(diagnostics,
                        ErrorCode.ERR_MissingPredefinedMember,
                        syntax,
                        containingType,
                        methodName);
                }
            }
        }

        // {type}.op_Implicit(T[])
        internal static MethodSymbol? TryFindImplicitOperatorFromArray(TypeSymbol type)
        {
            Debug.Assert(type.IsSpan() || type.IsReadOnlySpan());
            Debug.Assert(type.IsDefinition);

            return TryFindImplicitOperator(type, 0, static (_, method) =>
                method.Parameters[0].Type is ArrayTypeSymbol { IsSZArray: true, ElementType: TypeParameterSymbol });
        }

        // ReadOnlySpan<T> Span<T>.op_Implicit(Span<T>)
        internal static MethodSymbol? TryFindImplicitOperatorFromSpan(TypeSymbol spanType, TypeSymbol readonlySpanType)
        {
            Debug.Assert(spanType.IsSpan() && readonlySpanType.IsReadOnlySpan());
            Debug.Assert(spanType.IsDefinition && readonlySpanType.IsDefinition);

            return TryFindImplicitOperator(spanType, readonlySpanType,
                static (readonlySpanType, method) => method.Parameters[0].Type.IsSpan() &&
                    readonlySpanType.Equals(method.ReturnType.OriginalDefinition, TypeCompareKind.ConsiderEverything));
        }

        private static MethodSymbol? TryFindImplicitOperator<TArg>(TypeSymbol type, TArg arg,
            Func<TArg, MethodSymbol, bool> predicate)
        {
            return TryFindSingleMethod(type, WellKnownMemberNames.ImplicitConversionName, (predicate, arg),
                static (arg, method) => method is
                {
                    ParameterCount: 1,
                    Arity: 0,
                    IsStatic: true,
                    DeclaredAccessibility: Accessibility.Public,
                } && arg.predicate(arg.arg, method));
        }

        internal static bool NeedsSpanCastUp(TypeSymbol source, TypeSymbol destination)
        {
            Debug.Assert(source.IsSpan() || source.IsReadOnlySpan());
            Debug.Assert(destination.IsReadOnlySpan());
            Debug.Assert(!source.IsDefinition && !destination.IsDefinition);

            var sourceElementType = ((NamedTypeSymbol)source).TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0].Type;
            var destinationElementType = ((NamedTypeSymbol)destination).TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0].Type;

            var sameElementTypes = sourceElementType.Equals(destinationElementType, TypeCompareKind.AllIgnoreOptions);

            Debug.Assert(!source.IsReadOnlySpan() || !sameElementTypes);

            return !sameElementTypes;
        }

        // ReadOnlySpan<T> ReadOnlySpan<T>.CastUp<TDerived>(ReadOnlySpan<TDerived>) where TDerived : class
        internal static MethodSymbol? TryFindCastUpMethod(TypeSymbol source, TypeSymbol destination)
        {
            Debug.Assert(source.IsReadOnlySpan() && destination.IsReadOnlySpan());
            Debug.Assert(source.IsDefinition && destination.IsDefinition);

            return TryFindSingleMethod(destination, WellKnownMemberNames.CastUpMethodName, (source, destination),
                static (arg, method) => method is
                {
                    ParameterCount: 1,
                    Arity: 1,
                    IsStatic: true,
                    DeclaredAccessibility: Accessibility.Public,
                    Parameters: [{ } parameter],
                    TypeArgumentsWithAnnotations: [{ } typeArgument],
                } &&
                    // parameter type is the source ReadOnlySpan<>
                    arg.source.Equals(parameter.Type.OriginalDefinition, TypeCompareKind.ConsiderEverything) &&
                    // return type is the destination ReadOnlySpan<>
                    arg.destination.Equals(method.ReturnType.OriginalDefinition, TypeCompareKind.ConsiderEverything) &&
                    // TDerived : class
                    typeArgument.Type.IsReferenceType &&
                    // parameter type argument is TDerived
                    ((NamedTypeSymbol)parameter.Type).TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0].Type.Equals(typeArgument.Type, TypeCompareKind.ConsiderEverything) &&
                    // return type argument is T
                    ((NamedTypeSymbol)method.ReturnType).TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0].Type.Equals(((NamedTypeSymbol)arg.destination).TypeParameters[0], TypeCompareKind.ConsiderEverything));
        }

        // ReadOnlySpan<char> MemoryExtensions.AsSpan(string)
        internal static MethodSymbol? TryFindAsSpanCharMethod(CSharpCompilation compilation, TypeSymbol readOnlySpanType)
        {
            Debug.Assert(readOnlySpanType.IsReadOnlySpan());
            Debug.Assert(!readOnlySpanType.IsDefinition);
            Debug.Assert(((NamedTypeSymbol)readOnlySpanType).TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0].SpecialType is SpecialType.System_Char);

            MethodSymbol? result = null;
            foreach (var memoryExtensionsType in compilation.GetTypesByMetadataName(WellKnownMemberNames.MemoryExtensionsTypeFullName))
            {
                if (memoryExtensionsType.DeclaredAccessibility == Accessibility.Public &&
                    TryFindSingleMethod(memoryExtensionsType.GetSymbol<NamedTypeSymbol>(), WellKnownMemberNames.AsSpanMethodName, 0,
                    static (_, method) => method is
                    {
                        ParameterCount: 1,
                        Arity: 0,
                        IsStatic: true,
                        DeclaredAccessibility: Accessibility.Public,
                        Parameters: [{ Type.SpecialType: SpecialType.System_String }]
                    }) is { } method &&
                    method.ReturnType.Equals(readOnlySpanType, TypeCompareKind.ConsiderEverything))
                {
                    if (result is not null)
                    {
                        // Ambiguous member found.
                        return null;
                    }

                    result = method;
                }
            }

            return result;
        }

        private static MethodSymbol? TryFindSingleMethod<TArg>(TypeSymbol type, string name, TArg arg, Func<TArg, MethodSymbol, bool> predicate)
        {
            var members = type.GetMembers(name);
            MethodSymbol? result = null;
            foreach (var member in members)
            {
                if (member is MethodSymbol method && predicate(arg, method))
                {
                    if (result is not null)
                    {
                        // Ambiguous member found.
                        return null;
                    }

                    result = method;
                }
            }

            return result;
        }

        private BoundExpression BindUnconvertedInterpolatedExpressionToFormattableStringFactory(BoundUnconvertedInterpolatedString unconvertedSource, TypeSymbol destination, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(destination.Equals(Compilation.GetWellKnownType(WellKnownType.System_IFormattable), TypeCompareKind.ConsiderEverything) ||
                         destination.Equals(Compilation.GetWellKnownType(WellKnownType.System_FormattableString), TypeCompareKind.ConsiderEverything));

            ImmutableArray<BoundExpression> parts = BindInterpolatedStringPartsForFactory(unconvertedSource, diagnostics, out bool haveErrors);
            var stringFactory = GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_FormattableStringFactory, diagnostics, unconvertedSource.Syntax);

            if (stringFactory.IsErrorType() || haveErrors)
            {
                return new BoundInterpolatedString(
                    unconvertedSource.Syntax,
                    interpolationData: null,
                    BindInterpolatedStringParts(unconvertedSource, diagnostics),
                    unconvertedSource.ConstantValueOpt,
                    unconvertedSource.Type,
                    unconvertedSource.HasErrors);
            }

            return BindUnconvertedInterpolatedExpressionToFactory(unconvertedSource, parts, stringFactory, factoryMethod: "Create", destination, diagnostics);
        }

        private static void CheckInlineArrayTypeIsSupported(SyntaxNode syntax, TypeSymbol inlineArrayType, TypeSymbol elementType, BindingDiagnosticBag diagnostics)
        {
            if (elementType.IsPointerOrFunctionPointer() || elementType.IsRestrictedType())
            {
                Error(diagnostics, ErrorCode.ERR_BadTypeArgument, syntax, elementType);
            }
            else if (inlineArrayType.IsRestrictedType())
            {
                Error(diagnostics, ErrorCode.ERR_BadTypeArgument, syntax, inlineArrayType);
            }
        }

        private static BoundExpression ConvertObjectCreationExpression(
            SyntaxNode syntax, BoundUnconvertedObjectCreationExpression node, Conversion conversion, bool isCast, TypeSymbol destination,
            ConversionGroup? conversionGroupOpt, bool wasCompilerGenerated, BindingDiagnosticBag diagnostics)
        {
            var arguments = AnalyzedArguments.GetInstance(node.Arguments, node.ArgumentRefKindsOpt, node.ArgumentNamesOpt);
            BoundExpression expr = bindObjectCreationExpression(node.Syntax, node.InitializerOpt, node.Binder, destination.StrippedType(), arguments, diagnostics);
            arguments.Free();

            Debug.Assert(expr is BoundObjectCreationExpressionBase { WasTargetTyped: true } or
                                 BoundDelegateCreationExpression { WasTargetTyped: true } or
                                 BoundBadExpression);

            // Assert that the shape of the BoundBadExpression is sound and is not going to confuse NullableWalker for target-typed 'new'.
            Debug.Assert(expr is not BoundBadExpression { ChildBoundNodes: var children } || !children.Any((child, node) => child.Syntax == node.Syntax, node));

            if (wasCompilerGenerated)
            {
                expr.MakeCompilerGenerated();
            }

            expr = new BoundConversion(
                                  syntax,
                                  expr,
                                  expr is BoundBadExpression ? Conversion.NoConversion : conversion,
                                  node.Binder.CheckOverflowAtRuntime,
                                  explicitCastInCode: isCast && !wasCompilerGenerated,
                                  conversionGroupOpt,
                                  expr.ConstantValueOpt,
                                  destination)
            { WasCompilerGenerated = wasCompilerGenerated };

            return expr;

            static BoundExpression bindObjectCreationExpression(
                SyntaxNode syntax, InitializerExpressionSyntax? initializerOpt, Binder binder,
                TypeSymbol type, AnalyzedArguments arguments, BindingDiagnosticBag diagnostics)
            {
                switch (type.TypeKind)
                {
                    case TypeKind.Enum:
                    case TypeKind.Struct:
                    case TypeKind.Class when !type.IsAnonymousType: // We don't want to enable object creation with unspeakable types
                        return binder.BindClassCreationExpression(syntax, type.Name, typeNode: syntax, (NamedTypeSymbol)type, arguments, diagnostics, initializerOpt, wasTargetTyped: true);
                    case TypeKind.TypeParameter:
                        return binder.BindTypeParameterCreationExpression(syntax, (TypeParameterSymbol)type, arguments, initializerOpt, typeSyntax: syntax, wasTargetTyped: true, diagnostics);
                    case TypeKind.Delegate:
                        return binder.BindDelegateCreationExpression(syntax, (NamedTypeSymbol)type, arguments, initializerOpt, wasTargetTyped: true, diagnostics);
                    case TypeKind.Interface:
                        return binder.BindInterfaceCreationExpression(syntax, (NamedTypeSymbol)type, diagnostics, typeNode: syntax, arguments, initializerOpt, wasTargetTyped: true);
                    case TypeKind.Array:
                    case TypeKind.Class:
                    case TypeKind.Dynamic:
                        Error(diagnostics, ErrorCode.ERR_ImplicitObjectCreationIllegalTargetType, syntax, type);
                        goto case TypeKind.Error;
                    case TypeKind.Pointer:
                    case TypeKind.FunctionPointer:
                        Error(diagnostics, ErrorCode.ERR_UnsafeTypeInObjectCreation, syntax, type);
                        goto case TypeKind.Error;
                    case TypeKind.Error:
                        return binder.MakeBadExpressionForObjectCreation(syntax, type, arguments, initializerOpt, typeSyntax: syntax, diagnostics);
                    case var v:
                        throw ExceptionUtilities.UnexpectedValue(v);
                }
            }
        }

        private BoundCollectionExpression ConvertCollectionExpression(
            BoundUnconvertedCollectionExpression node,
            TypeSymbol targetType,
            Conversion conversion,
            BindingDiagnosticBag diagnostics)
        {
            if (conversion.IsNullable)
            {
                targetType = targetType.GetNullableUnderlyingType();
                conversion = conversion.UnderlyingConversions[0];
                _ = GetSpecialTypeMember(SpecialMember.System_Nullable_T__ctor, diagnostics, syntax: node.Syntax);
            }

            var collectionTypeKind = conversion.GetCollectionExpressionTypeKind(out var elementType, out MethodSymbol? constructor, out bool isExpanded);

            if (collectionTypeKind == CollectionExpressionTypeKind.None)
            {
                Debug.Assert(conversion.Kind is ConversionKind.NoConversion);
                return BindCollectionExpressionForErrorRecovery(node, targetType, inConversion: false, diagnostics);
            }

            Debug.Assert(elementType is { });

            var syntax = node.Syntax;
            if (LocalRewriter.IsAllocatingRefStructCollectionExpression(node, collectionTypeKind, elementType, Compilation))
            {
                diagnostics.Add(node.HasSpreadElements(out _, out _)
                    ? ErrorCode.WRN_CollectionExpressionRefStructSpreadMayAllocate
                    : ErrorCode.WRN_CollectionExpressionRefStructMayAllocate,
                    syntax, targetType);
            }

            switch (collectionTypeKind)
            {
                case CollectionExpressionTypeKind.Span:
                    _ = GetWellKnownTypeMember(WellKnownMember.System_Span_T__ctor_Array, diagnostics, syntax: syntax);
                    break;

                case CollectionExpressionTypeKind.ReadOnlySpan:
                    _ = GetWellKnownTypeMember(WellKnownMember.System_ReadOnlySpan_T__ctor_Array, diagnostics, syntax: syntax);
                    break;

                case CollectionExpressionTypeKind.ImplementsIEnumerable:
                    if (targetType.OriginalDefinition.Equals(Compilation.GetWellKnownType(WellKnownType.System_Collections_Immutable_ImmutableArray_T), TypeCompareKind.ConsiderEverything))
                    {
                        diagnostics.Add(ErrorCode.ERR_CollectionExpressionImmutableArray, syntax, targetType.OriginalDefinition);
                        return BindCollectionExpressionForErrorRecovery(node, targetType, inConversion: true, diagnostics);
                    }
                    break;

                case CollectionExpressionTypeKind.DictionaryInterface:
                    MessageID.IDS_FeatureDictionaryExpressions.CheckFeatureAvailability(diagnostics, syntax);
                    break;
            }

            var elements = node.Elements;
            BoundExpression? collectionCreation = null;
            BoundObjectOrCollectionValuePlaceholder? implicitReceiver = null;
            BoundValuePlaceholder? collectionBuilderSpanPlaceholder = null;
            MethodSymbol? setMethod = null;

            // Verify the existence of the well-known members that may be used in lowering, even
            // though not all will be used for any particular collection expression. Checking all
            // gives a consistent behavior, regardless of collection expression elements.

            if (collectionTypeKind is CollectionExpressionTypeKind.ImplementsIEnumerableWithIndexer or CollectionExpressionTypeKind.DictionaryInterface)
            {
                // PROTOTYPE: Check System_Collections_Generic_KeyValuePair_KV__ctor always as well?
                // PROTOTYPE: Should we check all of these for non-dictionary collections of KeyValuePair<,>?
                _ = GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_KeyValuePair_KV__get_Key, diagnostics, syntax: syntax);
                _ = GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_KeyValuePair_KV__get_Value, diagnostics, syntax: syntax);
            }

            if (collectionTypeKind is CollectionExpressionTypeKind.ImplementsIEnumerable or CollectionExpressionTypeKind.ImplementsIEnumerableWithIndexer)
            {
                if (collectionTypeKind is CollectionExpressionTypeKind.ImplementsIEnumerableWithIndexer)
                {
                    // https://github.com/dotnet/roslyn/issues/77865: Consider returning a BoundIndexerAccess from GetCollectionExpressionApplicableIndexer().
                    var indexer = GetCollectionExpressionApplicableIndexer(syntax, targetType, elementType, diagnostics);
                    setMethod = indexer?.GetOwnOrInheritedSetMethod();
                    Debug.Assert(setMethod is { });
                }

                if (targetType is NamedTypeSymbol namedType &&
                    HasParamsCollectionTypeInProgress(namedType, out NamedTypeSymbol? inProgress, out MethodSymbol? inProgressConstructor))
                {
                    Debug.Assert(inProgressConstructor is not null);
                    diagnostics.Add(ErrorCode.ERR_ParamsCollectionInfiniteChainOfConstructorCalls, syntax, inProgress, inProgressConstructor.OriginalDefinition);
                    return BindCollectionExpressionForErrorRecovery(node, namedType, inConversion: true, diagnostics);
                }

                implicitReceiver = new BoundObjectOrCollectionValuePlaceholder(syntax, isNewInstance: true, targetType) { WasCompilerGenerated = true };

                // Bind collection creation with arguments.
                foreach (var element in elements)
                {
                    if (element is BoundCollectionExpressionWithElement withElement)
                    {
                        var analyzedArguments = AnalyzedArguments.GetInstance();
                        withElement.GetArguments(analyzedArguments);
                        var collectionWithArguments = BindCollectionExpressionConstructor(syntax, targetType, constructor, analyzedArguments, diagnostics);
                        analyzedArguments.Free();
                        collectionCreation ??= collectionWithArguments;
                    }
                }

                // Bind collection creation with no arguments if necessary.
                if (collectionCreation is null)
                {
                    var analyzedArguments = AnalyzedArguments.GetInstance();
                    collectionCreation = BindCollectionExpressionConstructor(syntax, targetType, constructor, analyzedArguments, diagnostics);
                    analyzedArguments.Free();
                    Debug.Assert((collectionCreation is BoundNewT && !isExpanded && constructor is null) ||
                                 (collectionCreation is BoundObjectCreationExpression creation && creation.Expanded == isExpanded && creation.Constructor == constructor));

                    if (collectionCreation.HasErrors)
                    {
                        return BindCollectionExpressionForErrorRecovery(node, targetType, inConversion: true, diagnostics);
                    }
                }

                if (!elements.IsDefaultOrEmpty && HasCollectionInitializerTypeInProgress(syntax, targetType))
                {
                    diagnostics.Add(ErrorCode.ERR_CollectionInitializerInfiniteChainOfAddCalls, syntax, targetType);
                    return BindCollectionExpressionForErrorRecovery(node, targetType, inConversion: true, diagnostics);
                }
            }
            else if (collectionTypeKind is CollectionExpressionTypeKind.DictionaryInterface)
            {
                var dictionaryType = GetWellKnownType(WellKnownType.System_Collections_Generic_Dictionary_KV, diagnostics, syntax).
                    Construct(((NamedTypeSymbol)targetType).TypeArgumentsWithAnnotationsNoUseSiteDiagnostics);
                var useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
                var dictionaryConversion = Conversions.ClassifyConversionFromType(dictionaryType, targetType, isChecked: false, ref useSiteInfo);
                diagnostics.Add(syntax, useSiteInfo);
                if (!dictionaryConversion.IsImplicit)
                {
                    GenerateImplicitConversionError(diagnostics, Compilation, syntax, dictionaryConversion, dictionaryType, targetType);
                }

                _ = GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_Dictionary_KV__ctor, diagnostics, syntax: syntax);

                if ((object)targetType.OriginalDefinition == Compilation.GetWellKnownType(WellKnownType.System_Collections_Generic_IReadOnlyDictionary_KV))
                {
                    _ = GetWellKnownTypeMember(WellKnownMember.System_Collections_ObjectModel_ReadOnlyDictionary_KV__ctor, diagnostics, syntax: syntax);
                }

                implicitReceiver = new BoundObjectOrCollectionValuePlaceholder(syntax, isNewInstance: true, dictionaryType) { WasCompilerGenerated = true };
                setMethod = ((MethodSymbol)GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_Dictionary_KV__set_Item, diagnostics, syntax: syntax))?.AsMember(dictionaryType);
            }
            else if ((collectionTypeKind is CollectionExpressionTypeKind.ArrayInterface) ||
                node.HasSpreadElements(out _, out _))
            {
                _ = GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_List_T__ctor, diagnostics, syntax: syntax);
                _ = GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_List_T__ctorInt32, diagnostics, syntax: syntax);
                _ = GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_List_T__Add, diagnostics, syntax: syntax);
                _ = GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_List_T__ToArray, diagnostics, syntax: syntax);
            }

            if (collectionTypeKind is CollectionExpressionTypeKind.CollectionBuilder)
            {
                Debug.Assert(elementType is { });

                ((NamedTypeSymbol)targetType).HasCollectionBuilderAttribute(out TypeSymbol? builderType, out string? methodName);

                MethodSymbol? collectionBuilderMethod = GetAndValidateCollectionBuilderMethod(syntax, ((NamedTypeSymbol)targetType).OriginalDefinition, builderType, methodName, diagnostics);
                if (collectionBuilderMethod is null)
                {
                    return BindCollectionExpressionForErrorRecovery(node, targetType, inConversion: true, diagnostics);
                }

                Debug.Assert(builderType is { });
                Debug.Assert(!string.IsNullOrEmpty(methodName));

                var useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
                var typeArguments = ((NamedTypeSymbol)targetType).GetAllTypeArguments(ref useSiteInfo);
                diagnostics.Add(syntax, useSiteInfo);

                var candidateMethodGroup = BindCollectionBuilderMethodGroup(syntax, methodName, typeArguments, [collectionBuilderMethod]);
                collectionBuilderSpanPlaceholder = new BoundValuePlaceholder(syntax, GetWellKnownType(WellKnownType.System_ReadOnlySpan_T, diagnostics, syntax).Construct(elementType)) { WasCompilerGenerated = true };

                // Bind collection creation with no arguments.
                collectionCreation = BindCollectionBuilderCreate(
                    syntax,
                    candidateMethodGroup,
                    collectionBuilderSpanPlaceholder,
                    diagnostics);

                collectionCreation = CreateConversion(collectionCreation, targetType, diagnostics);
            }

            if (collectionTypeKind is not
                (CollectionExpressionTypeKind.ImplementsIEnumerable or CollectionExpressionTypeKind.ImplementsIEnumerableWithIndexer))
            {
                var withElement = elements.FirstOrDefault(e => e is BoundCollectionExpressionWithElement { Arguments.Length: > 0 });
                if (withElement is { })
                {
                    diagnostics.Add(ErrorCode.ERR_CollectionArgumentsNotSupportedForType, ((WithElementSyntax)withElement.Syntax).WithKeyword, targetType);
                }
            }

            return new BoundCollectionExpression(
                syntax,
                collectionTypeKind,
                implicitReceiver,
                collectionCreation,
                collectionBuilderSpanPlaceholder,
                setMethod,
                wasTargetTyped: true,
                node,
                ConvertCollectionExpressionElements(node, targetType, conversion, collectionTypeKind, elementType, implicitReceiver, diagnostics),
                targetType)
            { WasCompilerGenerated = node.IsParamsArrayOrCollection, IsParamsArrayOrCollection = node.IsParamsArrayOrCollection };
        }

        private ImmutableArray<BoundNode> ConvertCollectionExpressionElements(
            BoundUnconvertedCollectionExpression node,
            TypeSymbol targetType,
            Conversion conversion,
            CollectionExpressionTypeKind collectionTypeKind,
            TypeSymbol elementType,
            BoundObjectOrCollectionValuePlaceholder? implicitReceiver,
            BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(elementType is { });

            var syntax = node.Syntax;
            var elements = node.Elements;
            var elementConversions = conversion.UnderlyingConversions;
            Debug.Assert(elementConversions.All(c => c.Exists));
            var elementKeyValueTypes = ConversionsBase.TryGetCollectionKeyValuePairTypes(Compilation, elementType);
            var builder = ArrayBuilder<BoundNode>.GetInstance(elements.Length);
            int conversionIndex = 0;
            var collectionInitializerAddMethodBinder = (collectionTypeKind is CollectionExpressionTypeKind.ImplementsIEnumerable) ?
                new CollectionInitializerAddMethodBinder(syntax, targetType, this) :
                null;
            Debug.Assert(collectionInitializerAddMethodBinder is null || implicitReceiver is { });

            foreach (var element in elements)
            {
                if (element is BoundCollectionExpressionWithElement)
                {
                    continue;
                }
                var elementConversion = elementConversions[conversionIndex++];
                BoundNode convertedElement;
                switch (element)
                {
                    case BoundExpression expressionElement:
                        {
                            var expressionSyntax = expressionElement.Syntax;
                            if (elementKeyValueTypes is (var elementKeyType, var elementValueType) &&
                                elementConversion.TryGetKeyValueConversions(out var keyConversion, out var valueConversion))
                            {
                                _ = GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_KeyValuePair_KV__ctor, diagnostics, syntax: expressionSyntax);
                                if (!ConversionsBase.IsKeyValuePairType(Compilation, expressionElement.Type, out var keyType, out var valueType))
                                {
                                    throw ExceptionUtilities.UnexpectedValue(expressionElement.Type);
                                }
                                BoundExpression convertedExpression = BindToNaturalType(expressionElement, diagnostics);
                                var keyPlaceholder = new BoundValuePlaceholder(expressionSyntax, keyType);
                                var valuePlaceholder = new BoundValuePlaceholder(expressionSyntax, valueType);
                                var keyValuePairConversion = new BoundKeyValuePairConversion(
                                    expressionSyntax,
                                    expression: convertedExpression,
                                    keyPlaceholder: keyPlaceholder,
                                    valuePlaceholder: valuePlaceholder,
                                    keyConversion: CreateConversion(keyPlaceholder, keyConversion, elementKeyType, diagnostics),
                                    valueConversion: CreateConversion(valuePlaceholder, valueConversion, elementValueType, diagnostics),
                                    elementType);
                                if (collectionInitializerAddMethodBinder is { })
                                {
                                    // PROTOTYPE: Need to check System_Collections_Generic_KeyValuePair_KV__getKey and __getValue are available. Test KeyValuePair_MissingMember_NonDictionary() currently fails some cases in lowering.
                                    convertedElement = BindCollectionInitializerElementAddMethod(
                                        expressionSyntax,
                                        [keyValuePairConversion],
                                        hasEnumerableInitializerType: true,
                                        collectionInitializerAddMethodBinder,
                                        diagnostics,
                                        implicitReceiver);
                                }
                                else
                                {
                                    convertedElement = keyValuePairConversion;
                                }
                            }
                            else if (collectionInitializerAddMethodBinder is { })
                            {
                                convertedElement = BindCollectionInitializerElementAddMethod(
                                    expressionSyntax,
                                    [expressionElement],
                                    hasEnumerableInitializerType: true,
                                    collectionInitializerAddMethodBinder,
                                    diagnostics,
                                    implicitReceiver);
                            }
                            else
                            {
                                convertedElement = CreateConversion(expressionElement, elementConversion, elementType, diagnostics);
                            }
                        }
                        break;
                    case BoundCollectionExpressionSpreadElement spreadElement:
                        convertedElement = BindCollectionExpressionSpreadElement(
                            (SpreadElementSyntax)spreadElement.Syntax,
                            spreadElement,
                            implicitReceiver!, // PROTOTYPE: We don't always need an implicit receiver here, and in fact it can be null.
                            static (binder, syntax, item, implicitReceiver, arg, diagnostics) =>
                            {
                                // PROTOTYPE: This implementation is almost identical to the BoundExpression case above. The code should be shared.
                                if (arg.elementKeyValueTypes is (var elementKeyType, var elementValueType) &&
                                    arg.elementConversion.TryGetKeyValueConversions(out var keyConversion, out var valueConversion))
                                {
                                    _ = binder.GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_KeyValuePair_KV__ctor, diagnostics, syntax: syntax);
                                    if (!ConversionsBase.IsKeyValuePairType(binder.Compilation, item.Type, out var keyType, out var valueType))
                                    {
                                        throw ExceptionUtilities.UnexpectedValue(item.Type);
                                    }
                                    var keyPlaceholder = new BoundValuePlaceholder(syntax, keyType);
                                    var valuePlaceholder = new BoundValuePlaceholder(syntax, valueType);
                                    var keyValuePairConversion = new BoundKeyValuePairConversion(
                                        syntax,
                                        expression: item,
                                        keyPlaceholder: keyPlaceholder,
                                        valuePlaceholder: valuePlaceholder,
                                        keyConversion: binder.CreateConversion(keyPlaceholder, keyConversion, elementKeyType, diagnostics),
                                        valueConversion: binder.CreateConversion(valuePlaceholder, valueConversion, elementValueType, diagnostics),
                                        type: arg.elementType);
                                    if (arg.collectionInitializerAddMethodBinder is { })
                                    {
                                        // PROTOTYPE: Need to check System_Collections_Generic_KeyValuePair_KV__getKey and __getValue are available. Test KeyValuePair_MissingMember_NonDictionary() currently fails some cases in lowering.
                                        return binder.BindCollectionInitializerElementAddMethod(
                                            syntax,
                                            [keyValuePairConversion],
                                            hasEnumerableInitializerType: true,
                                            arg.collectionInitializerAddMethodBinder,
                                            diagnostics,
                                            implicitReceiver);
                                    }
                                    else
                                    {
                                        return keyValuePairConversion;
                                    }
                                }
                                else if (arg.collectionInitializerAddMethodBinder is { })
                                {
                                    return binder.BindCollectionInitializerElementAddMethod(
                                        syntax,
                                        [item],
                                        hasEnumerableInitializerType: true,
                                        arg.collectionInitializerAddMethodBinder,
                                        diagnostics,
                                        implicitReceiver);
                                }
                                else
                                {
                                    return binder.CreateConversion(item, arg.elementConversion, arg.elementType, diagnostics);
                                }
                            },
                            (elementType, elementConversion, elementKeyValueTypes, collectionInitializerAddMethodBinder),
                            diagnostics);
                        break;
                    case BoundKeyValuePairElement keyValuePairElement:
                        {
                            if (elementKeyValueTypes is (var elementKeyType, var elementValueType) &&
                                elementConversion.TryGetKeyValueConversions(out var keyConversion, out var valueConversion))
                            {
                                var keyValuePairSyntax = (KeyValuePairElementSyntax)keyValuePairElement.Syntax;
                                var keyValuePairConstructor = (MethodSymbol?)GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_KeyValuePair_KV__ctor, diagnostics, syntax: keyValuePairSyntax); // PROTOTYPE: Test missing constructor.
                                var key = CreateConversion(keyValuePairElement.Key, keyConversion, elementKeyType, diagnostics);
                                var value = CreateConversion(keyValuePairElement.Value, valueConversion, elementValueType, diagnostics);
                                if (collectionInitializerAddMethodBinder is { })
                                {
                                    var keyValuePair = createKeyValuePair(keyValuePairSyntax, keyValuePairConstructor, key, value, elementType);
                                    convertedElement = BindCollectionInitializerElementAddMethod(
                                        keyValuePairSyntax,
                                        [keyValuePair],
                                        hasEnumerableInitializerType: true,
                                        collectionInitializerAddMethodBinder,
                                        diagnostics,
                                        implicitReceiver);
                                }
                                else
                                {
                                    convertedElement = new BoundKeyValuePairElement(keyValuePairSyntax, key, value);
                                }
                            }
                            else
                            {
                                throw ExceptionUtilities.UnexpectedValue(elementConversion);
                            }
                        }
                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(element);
                }
                builder.Add(convertedElement);
            }

            Debug.Assert(conversionIndex == elementConversions.Length);
            conversion.MarkUnderlyingConversionsChecked();

            return builder.ToImmutableAndFree();

            BoundExpression createKeyValuePair(
                SyntaxNode syntax,
                MethodSymbol? constructor,
                BoundExpression key,
                BoundExpression value,
                TypeSymbol keyValuePairType)
            {
#if DEBUG
                Debug.Assert(ConversionsBase.IsKeyValuePairType(Compilation, keyValuePairType, out var keyType, out var valueType) &&
                    keyType.Equals(key.Type, TypeCompareKind.AllIgnoreOptions) &&
                    valueType.Equals(value.Type, TypeCompareKind.AllIgnoreOptions));
#endif
                if (constructor is null)
                {
                    return new BoundBadExpression(syntax, LookupResultKind.Empty, symbols: [], childBoundNodes: [], keyValuePairType) { WasCompilerGenerated = true };
                }
                constructor = constructor.AsMember((NamedTypeSymbol)keyValuePairType);
                return new BoundObjectCreationExpression(syntax, constructor, key, value) { WasCompilerGenerated = true };
            }
        }

        internal BoundMethodGroup BindCollectionBuilderMethodGroup(
            SyntaxNode syntax,
            string methodName,
            ImmutableArray<TypeWithAnnotations> typeArguments,
            ImmutableArray<MethodSymbol> collectionBuilderCandidates)
        {
            Debug.Assert(collectionBuilderCandidates.All(c => c.IsDefinition));

            return new BoundMethodGroup(
                syntax,
                typeArgumentsOpt: typeArguments,
                name: methodName,
                methods: collectionBuilderCandidates,
                lookupSymbolOpt: null,
                lookupError: null,
                flags: BoundMethodGroupFlags.None,
                functionType: null,
                receiverOpt: null,
                resultKind: LookupResultKind.Viable);
        }

        internal BoundExpression BindCollectionBuilderCreate(
            SyntaxNode syntax,
            BoundMethodGroup candidateMethodGroup,
            BoundExpression spanArgument,
            BindingDiagnosticBag diagnostics)
        {
            var analyzedArguments = AnalyzedArguments.GetInstance();
            analyzedArguments.Arguments.Add(spanArgument);
            var collectionCreation = BindMethodGroupInvocation(
                syntax,
                expression: syntax,
                methodName: candidateMethodGroup.Name,
                candidateMethodGroup,
                analyzedArguments,
                diagnostics,
                queryClause: null,
                ignoreNormalFormIfHasValidParamsParameter: false,
                out _,
                disallowExpandedNonArrayParams: true,
                acceptOnlyMethods: true).MakeCompilerGenerated();
            analyzedArguments.Free();
            return collectionCreation;
        }

        private bool HasCollectionInitializerTypeInProgress(SyntaxNode syntax, TypeSymbol targetType)
        {
            Binder? current = this;
            while (current?.Flags.Includes(BinderFlags.CollectionInitializerAddMethod) == true)
            {
                if (current is CollectionInitializerAddMethodBinder binder &&
                    binder.Syntax == syntax &&
                    binder.CollectionType.OriginalDefinition.Equals(targetType.OriginalDefinition, TypeCompareKind.AllIgnoreOptions))
                {
                    return true;
                }

                current = current.Next;
            }

            return false;
        }

        internal MethodSymbol? GetAndValidateCollectionBuilderMethod(
            SyntaxNode syntax,
            NamedTypeSymbol targetType,
            TypeSymbol? builderType,
            string? methodName,
            BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(targetType.IsDefinition);

            bool result = TryGetCollectionIterationType(syntax, targetType, out var elementType);
            Debug.Assert(result);

            if (SourceNamedTypeSymbol.IsValidCollectionBuilderType(builderType) &&
                !string.IsNullOrEmpty(methodName))
            {
                var useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
                MethodSymbol? collectionBuilderMethod = GetCollectionBuilderMethod(targetType, elementType.Type, (NamedTypeSymbol)builderType, methodName, ref useSiteInfo);
                diagnostics.Add(syntax, useSiteInfo);

                if (collectionBuilderMethod is { })
                {
                    Debug.Assert(collectionBuilderMethod.Arity == targetType.AllTypeArgumentCount());

                    ReportDiagnosticsIfObsolete(diagnostics, builderType, syntax, hasBaseReceiver: false);
                    return collectionBuilderMethod;
                }
            }

            diagnostics.Add(ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound, syntax, methodName ?? "", elementType.Type, targetType);
            return null;
        }

        internal static MethodSymbol? GetCollectionBuilderMethod(BoundCollectionExpression node)
        {
            if (node.CollectionTypeKind != CollectionExpressionTypeKind.CollectionBuilder)
            {
                return null;
            }

            Debug.Assert(node.CollectionCreation is { });
            return getMethodFromExpression(node.CollectionCreation);

            static MethodSymbol? getMethodFromExpression(BoundExpression? expr)
            {
                return expr switch
                {
                    BoundCall call => call.Method,
                    BoundConversion conversion => getMethodFromExpression(conversion.Operand),
                    _ => null,
                };
            }
        }

        internal BoundExpression BindCollectionExpressionConstructor(
            SyntaxNode syntax,
            TypeSymbol targetType,
            MethodSymbol? constructorNoArguments,
            AnalyzedArguments analyzedArguments,
            BindingDiagnosticBag diagnostics)
        {
            //
            // !!! ATTENTION !!!
            //
            // In terms of errors relevant for HasCollectionExpressionApplicableConstructor check
            // this function should be kept in sync with HasCollectionExpressionApplicableConstructor.
            //

            BoundExpression collectionCreation;
            if (targetType is NamedTypeSymbol namedType)
            {
                var binder = new ParamsCollectionTypeInProgressBinder(namedType, this, constructorNoArguments);
                collectionCreation = binder.BindClassCreationExpression(syntax, namedType.Name, syntax, namedType, analyzedArguments, diagnostics);
                collectionCreation.WasCompilerGenerated = true;
            }
            else if (targetType is TypeParameterSymbol typeParameter)
            {
                collectionCreation = BindTypeParameterCreationExpression(syntax, typeParameter, analyzedArguments, initializerOpt: null, typeSyntax: syntax, wasTargetTyped: true, diagnostics);
                collectionCreation.WasCompilerGenerated = true;
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(targetType);
            }
            return collectionCreation;
        }

        internal bool HasCollectionExpressionApplicableConstructor(SyntaxNode syntax, TypeSymbol targetType, out MethodSymbol? constructor, out bool isExpanded, BindingDiagnosticBag diagnostics, bool isParamsModifierValidation = false)
        {
            Debug.Assert(!isParamsModifierValidation || syntax is ParameterSyntax);

            // This is what BindClassCreationExpression is doing in terms of reporting diagnostics

            constructor = null;
            isExpanded = false;

            if (targetType is NamedTypeSymbol namedType)
            {
                // This is what BindClassCreationExpression called by BindCollectionExpressionConstructor is doing in terms of reporting diagnostics

                if (namedType.IsAbstract)
                {
                    // Report error for new of abstract type.
                    diagnostics.Add(ErrorCode.ERR_NoNewAbstract, syntax.Location, namedType);
                    return false;
                }

                if (HasParamsCollectionTypeInProgress(namedType, out _, out _))
                {
                    // We are in a cycle. Optimistically assume we have the right constructor to break the cycle
                    return true;
                }

                var analyzedArguments = AnalyzedArguments.GetInstance();
                var binder = new ParamsCollectionTypeInProgressBinder(namedType, this);

                bool overloadResolutionSucceeded = binder.TryPerformConstructorOverloadResolution(
                        namedType,
                        analyzedArguments,
                        namedType.Name,
                        syntax.Location,
                        suppressResultDiagnostics: false,
                        diagnostics,
                        out MemberResolutionResult<MethodSymbol> memberResolutionResult,
                        candidateConstructors: out _,
                        allowProtectedConstructorsOfBaseType: false,
                        out CompoundUseSiteInfo<AssemblySymbol> overloadResolutionUseSiteInfo,
                        isParamsModifierValidation: isParamsModifierValidation);

                analyzedArguments.Free();

                if (overloadResolutionSucceeded)
                {
                    bindClassCreationExpressionContinued(binder, syntax, memberResolutionResult, in overloadResolutionUseSiteInfo, isParamsModifierValidation, diagnostics);
                    constructor = memberResolutionResult.Member;
                    isExpanded = memberResolutionResult.Result.Kind == MemberResolutionKind.ApplicableInExpandedForm;
                }
                else
                {
                    reportAdditionalDiagnosticsForOverloadResolutionFailure(syntax, in overloadResolutionUseSiteInfo, diagnostics);
                }

                return overloadResolutionSucceeded;
            }
            else if (targetType is TypeParameterSymbol typeParameter)
            {
                return TypeParameterHasParameterlessConstructor(syntax, typeParameter, diagnostics);
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(targetType);
            }

            // This is what BindClassCreationExpressionContinued is doing in terms of reporting diagnostics
            static void bindClassCreationExpressionContinued(
                Binder binder,
                SyntaxNode node,
                MemberResolutionResult<MethodSymbol> memberResolutionResult,
                in CompoundUseSiteInfo<AssemblySymbol> overloadResolutionUseSiteInfo,
                bool isParamsModifierValidation,
                BindingDiagnosticBag diagnostics)
            {
                ReportConstructorUseSiteDiagnostics(node.Location, diagnostics, suppressUnsupportedRequiredMembersError: false, in overloadResolutionUseSiteInfo);

                var method = memberResolutionResult.Member;

                binder.ReportDiagnosticsIfObsolete(diagnostics, method, node, hasBaseReceiver: false);
                // NOTE: Use-site diagnostics were reported during overload resolution.

                ImmutableSegmentedDictionary<string, Symbol> requiredMembers = GetMembersRequiringInitialization(method);
                if (requiredMembers.Count != 0)
                {
                    if (isParamsModifierValidation)
                    {
                        diagnostics.Add(
                            ErrorCode.ERR_ParamsCollectionConstructorDoesntInitializeRequiredMember,
                            ((ParameterSyntax)node).Modifiers.First(static m => m.IsKind(SyntaxKind.ParamsKeyword)).GetLocation(),
                            method, requiredMembers.First().Value);
                    }
                    else
                    {
                        ReportMembersRequiringInitialization(node, requiredMembers.ToBuilder(), diagnostics);
                    }
                }
            }

            // This is what CreateBadClassCreationExpression is doing in terms of reporting diagnostics
            static void reportAdditionalDiagnosticsForOverloadResolutionFailure(
                SyntaxNode typeNode,
                in CompoundUseSiteInfo<AssemblySymbol> overloadResolutionUseSiteInfo,
                BindingDiagnosticBag diagnostics)
            {
                ReportConstructorUseSiteDiagnostics(typeNode.Location, diagnostics, suppressUnsupportedRequiredMembersError: false, in overloadResolutionUseSiteInfo);
            }
        }

        private bool HasParamsCollectionTypeInProgress(NamedTypeSymbol toCheck,
            [NotNullWhen(returnValue: true)] out NamedTypeSymbol? inProgress,
            out MethodSymbol? constructor)
        {
            Binder? current = this;
            while (current?.Flags.Includes(BinderFlags.CollectionExpressionConversionValidation) == true)
            {
                if (current.ParamsCollectionTypeInProgress?.OriginalDefinition.Equals(toCheck.OriginalDefinition, TypeCompareKind.AllIgnoreOptions) == true)
                {
                    // We are in a cycle.
                    inProgress = current.ParamsCollectionTypeInProgress;
                    constructor = current.ParamsCollectionConstructorInProgress;
                    return true;
                }

                current = current.Next;
            }

            inProgress = null;
            constructor = null;
            return false;
        }

        internal bool HasParamsCollectionTypeApplicableIndexerInProgress(SyntaxNode syntax, TypeSymbol targetType)
        {
            Binder? current = this;
            do
            {
                if (current is ParamsCollectionTypeApplicableIndexerInProgress binder &&
                    binder.Syntax == syntax &&
                    object.ReferenceEquals(binder.CollectionType.OriginalDefinition, targetType.OriginalDefinition))
                {
                    return true;
                }
                current = current.Next;
            } while (current is { });

            return false;
        }

        internal PropertySymbol? GetCollectionExpressionApplicableIndexer(SyntaxNode syntax, TypeSymbol targetType, TypeSymbol elementType, BindingDiagnosticBag diagnostics)
        {
            if (!Compilation.IsFeatureEnabled(MessageID.IDS_FeatureDictionaryExpressions))
            {
                return null;
            }

            if (!ConversionsBase.IsKeyValuePairType(Compilation, elementType, out var keyType, out var valueType))
            {
                return null;
            }

            var receiver = new BoundValuePlaceholder(syntax, targetType);
            var analyzedArguments = AnalyzedArguments.GetInstance();
            analyzedArguments.Arguments.Add(new BoundValuePlaceholder(syntax, keyType));
            var indexerAccess = BindIndexerAccess(syntax, receiver, analyzedArguments, diagnostics);
            analyzedArguments.Free();

            if (indexerAccess is BoundIndexerAccess { Indexer: { } indexer })
            {
                var useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
                indexer = indexer.GetLeastOverriddenProperty(accessingTypeOpt: null);
                if (indexer is
                    {
                        IsStatic: false,
                        DeclaredAccessibility: Accessibility.Public,
                        RefKind: RefKind.None,
                        GetMethod: { DeclaredAccessibility: Accessibility.Public },
                        SetMethod: { DeclaredAccessibility: Accessibility.Public },
                        Parameters: [{ RefKind: RefKind.None or RefKind.In } parameter]
                    } &&
                    Conversions.ClassifyImplicitConversionFromType(parameter.Type, keyType, ref useSiteInfo).IsIdentity &&
                    Conversions.ClassifyImplicitConversionFromType(indexer.Type, valueType, ref useSiteInfo).IsIdentity)
                {
                    diagnostics.Add(syntax, useSiteInfo);
                    return indexer;
                }
            }

            return null;
        }

        internal bool HasCollectionExpressionApplicableAddMethod(SyntaxNode syntax, TypeSymbol targetType, out ImmutableArray<MethodSymbol> addMethods, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(!targetType.IsDynamic());

            NamedTypeSymbol? namedType = targetType as NamedTypeSymbol;

            if (namedType is not null && HasParamsCollectionTypeInProgress(namedType, out _, out _))
            {
                // We are in a cycle. Optimistically assume we have the right Add to break the cycle
                addMethods = [];
                return true;
            }

            var implicitReceiver = new BoundObjectOrCollectionValuePlaceholder(syntax, isNewInstance: true, targetType) { WasCompilerGenerated = true };

            // For the element, we create a dynamic argument and will be forcing overload resolution to convert it to any type.
            // This way we are going to do most of the work in terms of determining applicability of 'Add' method candidates
            // in overload resolution.
            var elementPlaceholder = new BoundValuePlaceholder(syntax, Compilation.DynamicType) { WasCompilerGenerated = true };

            var addMethodBinder = WithAdditionalFlags(BinderFlags.CollectionInitializerAddMethod | BinderFlags.CollectionExpressionConversionValidation);

            if (namedType is not null)
            {
                addMethodBinder = new ParamsCollectionTypeInProgressBinder(namedType, addMethodBinder);
            }

            return bindCollectionInitializerElementAddMethod(
                addMethodBinder,
                syntax,
                elementPlaceholder,
                diagnostics,
                implicitReceiver,
                out addMethods);

            // This is what BindCollectionInitializerElementAddMethod is doing in terms of reporting diagnostics and detecting a failure
            static bool bindCollectionInitializerElementAddMethod(
                Binder addMethodBinder,
                SyntaxNode elementInitializer,
                BoundValuePlaceholder arg,
                BindingDiagnosticBag diagnostics,
                BoundObjectOrCollectionValuePlaceholder implicitReceiver,
                out ImmutableArray<MethodSymbol> addMethods)
            {
                return makeInvocationExpression(
                    addMethodBinder,
                    elementInitializer,
                    implicitReceiver,
                    arg: arg,
                    diagnostics,
                    out addMethods);
            }

            // This is what MakeInvocationExpression is doing in terms of reporting diagnostics and detecting a failure
            static bool makeInvocationExpression(
                Binder addMethodBinder,
                SyntaxNode node,
                BoundExpression receiver,
                BoundValuePlaceholder arg,
                BindingDiagnosticBag diagnostics,
                out ImmutableArray<MethodSymbol> addMethods)
            {
                var boundExpression = addMethodBinder.BindInstanceMemberAccess(
                    node, node, receiver, WellKnownMemberNames.CollectionInitializerAddMethodName, rightArity: 0,
                    typeArgumentsSyntax: default(SeparatedSyntaxList<TypeSyntax>),
                    typeArgumentsWithAnnotations: default(ImmutableArray<TypeWithAnnotations>),
                    invoked: true, indexed: false, diagnostics, searchExtensionsIfNecessary: true);

                // require the target member to be a method.
                if (boundExpression.Kind == BoundKind.FieldAccess || boundExpression.Kind == BoundKind.PropertyAccess)
                {
                    ReportMakeInvocationExpressionBadMemberKind(node, WellKnownMemberNames.CollectionInitializerAddMethodName, boundExpression, diagnostics);
                    addMethods = [];
                    return false;
                }

                if (boundExpression.Kind != BoundKind.MethodGroup)
                {
                    Debug.Assert(boundExpression.HasErrors);
                    addMethods = [];
                    return false;
                }

                var analyzedArguments = AnalyzedArguments.GetInstance();
                analyzedArguments.Arguments.AddRange(arg);

                bool result = bindInvocationExpression(
                    addMethodBinder, node, node, (BoundMethodGroup)boundExpression, analyzedArguments, diagnostics, out addMethods);

                analyzedArguments.Free();
                return result;
            }

            // This is what BindInvocationExpression is doing in terms of reporting diagnostics and detecting a failure
            static bool bindInvocationExpression(
                Binder addMethodBinder,
                SyntaxNode node,
                SyntaxNode expression,
                BoundMethodGroup boundExpression,
                AnalyzedArguments analyzedArguments,
                BindingDiagnosticBag diagnostics,
                out ImmutableArray<MethodSymbol> addMethods)
            {
                return bindMethodGroupInvocation(
                    addMethodBinder, node, expression, boundExpression, analyzedArguments,
                    diagnostics, out addMethods);
            }

            // This is what BindMethodGroupInvocation is doing in terms of reporting diagnostics and detecting a failure
            static bool bindMethodGroupInvocation(
                Binder addMethodBinder,
                SyntaxNode syntax,
                SyntaxNode expression,
                BoundMethodGroup methodGroup,
                AnalyzedArguments analyzedArguments,
                BindingDiagnosticBag diagnostics,
                out ImmutableArray<MethodSymbol> addMethods)
            {
                Debug.Assert(methodGroup.ReceiverOpt is not null);
                Debug.Assert(methodGroup.ReceiverOpt.Type is not null);

                bool result;
                CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = addMethodBinder.GetNewCompoundUseSiteInfo(diagnostics);
                var resolution = addMethodBinder.ResolveMethodGroup(
                    methodGroup, expression, WellKnownMemberNames.CollectionInitializerAddMethodName, analyzedArguments,
                    useSiteInfo: ref useSiteInfo,
                    options: OverloadResolution.Options.DynamicResolution | OverloadResolution.Options.DynamicConvertsToAnything,
                    acceptOnlyMethods: true);

                diagnostics.Add(expression, useSiteInfo);

                if (!methodGroup.HasAnyErrors) diagnostics.AddRange(resolution.Diagnostics); // Suppress cascading.

                if (resolution.IsNonMethodExtensionMember(out Symbol? extensionMember))
                {
                    Debug.Assert(false); // Should not get here given the 'acceptOnlyMethods' argument value used in 'ResolveMethodGroup' call above  
                    ReportMakeInvocationExpressionBadMemberKind(syntax, WellKnownMemberNames.CollectionInitializerAddMethodName, methodGroup, diagnostics);
                    addMethods = [];
                    result = false;
                }
                else if (resolution.HasAnyErrors)
                {
                    addMethods = [];
                    result = false;
                }
                else if (!resolution.IsEmpty)
                {
                    // We're checking resolution.ResultKind, rather than methodGroup.HasErrors
                    // to better handle the case where there's a problem with the receiver
                    // (e.g. inaccessible), but the method group resolved correctly (e.g. because
                    // it's actually an accessible static method on a base type).
                    // CONSIDER: could check for error types amongst method group type arguments.
                    if (resolution.ResultKind != LookupResultKind.Viable)
                    {
                        addMethods = [];
                        result = false;
                    }
                    else
                    {
                        Debug.Assert(resolution.AnalyzedArguments.HasDynamicArgument);

                        // If overload resolution found one or more applicable methods and at least one argument
                        // was dynamic then treat this as a dynamic call.
                        if (resolution.OverloadResolutionResult.HasAnyApplicableMember)
                        {
                            // Note that the runtime binder may consider candidates that haven't passed compile-time final validation 
                            // and an ambiguity error may be reported. Also additional checks are performed in runtime final validation 
                            // that are not performed at compile-time.
                            // Only if the set of final applicable candidates is empty we know for sure the call will fail at runtime.
                            var finalApplicableCandidates = addMethodBinder.GetCandidatesPassingFinalValidation(syntax, resolution.OverloadResolutionResult,
                                                                                                                methodGroup.ReceiverOpt,
                                                                                                                methodGroup.TypeArgumentsOpt,
                                                                                                                isExtensionMethodGroup: resolution.IsExtensionMethodGroup,
                                                                                                                diagnostics);

                            Debug.Assert(finalApplicableCandidates.Length != 1 || finalApplicableCandidates[0].IsApplicable);

                            if (finalApplicableCandidates.Length == 0)
                            {
                                addMethods = [];
                                result = false;
                            }
                            else
                            {
                                addMethods = filterOutBadGenericMethods(addMethodBinder, syntax, methodGroup, analyzedArguments, resolution, finalApplicableCandidates, ref useSiteInfo);
                                result = !addMethods.IsEmpty;

                                if (!result)
                                {
                                    diagnostics.Add(ErrorCode.ERR_CollectionExpressionMissingAdd, syntax, methodGroup.ReceiverOpt.Type);
                                }
                                else if (addMethods.Length == 1)
                                {
                                    addMethodBinder.ReportDiagnosticsIfObsolete(diagnostics, addMethods[0], syntax, hasBaseReceiver: false);
                                    ReportDiagnosticsIfUnmanagedCallersOnly(diagnostics, addMethods[0], syntax, isDelegateConversion: false);
                                }
                            }
                        }
                        else
                        {
                            result = bindInvocationExpressionContinued(
                                addMethodBinder, syntax, expression, resolution.OverloadResolutionResult, resolution.AnalyzedArguments,
                                resolution.MethodGroup, diagnostics: diagnostics, out var addMethod);
                            addMethods = addMethod is null ? [] : [addMethod];
                        }
                    }
                }
                else
                {
                    addMethods = [];
                    result = false;
                }

                resolution.Free();
                return result;
            }

            static ImmutableArray<MethodSymbol> filterOutBadGenericMethods(
                Binder addMethodBinder, SyntaxNode syntax, BoundMethodGroup methodGroup, AnalyzedArguments analyzedArguments, MethodGroupResolution resolution,
                ImmutableArray<MemberResolutionResult<MethodSymbol>> finalApplicableCandidates, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
            {
                Debug.Assert(methodGroup.ReceiverOpt is not null);
                var resultBuilder = ArrayBuilder<MethodSymbol>.GetInstance(finalApplicableCandidates.Length);

                foreach (var candidate in finalApplicableCandidates)
                {
                    // If the method is generic, skip it if the type arguments cannot be inferred.
                    var member = candidate.Member;
                    var typeParameters = member.TypeParameters;

                    if (!typeParameters.IsEmpty)
                    {
                        if (resolution.IsExtensionMethodGroup) // Tracked by https://github.com/dotnet/roslyn/issues/76130 : we need to handle new extension methods
                        {
                            // We need to validate an ability to infer type arguments as well as check conversion to 'this' parameter.
                            // Overload resolution doesn't check the conversion when 'this' type refers to a type parameter
                            TypeSymbol? receiverType = methodGroup.ReceiverOpt.Type;
                            Debug.Assert(receiverType is not null);
                            bool thisTypeIsOpen = typeParameters.Any((typeParameter, parameter) => parameter.Type.ContainsTypeParameter(typeParameter), member.Parameters[0]);
                            MethodSymbol? constructed = null;
                            bool wasFullyInferred = false;

                            if (thisTypeIsOpen)
                            {
                                constructed = ReducedExtensionMethodSymbol.InferExtensionMethodTypeArguments(
                                                            member, receiverType, addMethodBinder.Compilation, ref useSiteInfo, out wasFullyInferred);
                            }

                            if (constructed is null || !wasFullyInferred)
                            {
                                // It is quite possible that inference failed because we didn't supply type from the second argument
                                if (!typeParameters.Any((typeParameter, parameter) => parameter.Type.ContainsTypeParameter(typeParameter), member.Parameters[1]))
                                {
                                    continue;
                                }

                                // Let's attempt inference with type for the second parameter
                                // We are going to use the second parameter's type for that
                                OverloadResolution.GetEffectiveParameterTypes(
                                    member,
                                    argumentCount: 2,
                                    argToParamMap: default,
                                    argumentRefKinds: analyzedArguments.RefKinds,
                                    isMethodGroupConversion: false,
                                    allowRefOmittedArguments: methodGroup.ReceiverOpt.IsExpressionOfComImportType(),
                                    binder: addMethodBinder,
                                    expanded: candidate.Result.Kind == MemberResolutionKind.ApplicableInExpandedForm,
                                    parameterTypes: out ImmutableArray<TypeWithAnnotations> parameterTypes,
                                    parameterRefKinds: out ImmutableArray<RefKind> parameterRefKinds);

                                // If we were able to infer something just from the first parameter,
                                // use partially substituted second type, otherwise inference might fail
                                // for type parameters "shared" between the parameters.
                                TypeSymbol secondArgumentType = (constructed ?? member).Parameters[1].Type;

                                MethodTypeInferenceResult inferenceResult = MethodTypeInferrer.Infer(
                                    addMethodBinder,
                                    addMethodBinder.Conversions,
                                    member.TypeParameters,
                                    member.ContainingType,
                                    parameterTypes,
                                    parameterRefKinds,
                                    ImmutableArray.Create<BoundExpression>(methodGroup.ReceiverOpt, new BoundValuePlaceholder(syntax, secondArgumentType) { WasCompilerGenerated = true }),
                                    ref useSiteInfo); // Tracked by https://github.com/dotnet/roslyn/issues/76130 : we may need to override ordinals here

                                if (!inferenceResult.Success)
                                {
                                    continue;
                                }

                                if (thisTypeIsOpen)
                                {
                                    constructed = member.Construct(inferenceResult.InferredTypeArguments);
                                }
                            }

                            if (thisTypeIsOpen)
                            {
                                Debug.Assert(constructed is not null);
                                var conversions = constructed.ContainingAssembly.CorLibrary.TypeConversions;
                                var conversion = conversions.ConvertExtensionMethodThisArg(constructed.Parameters[0].Type, receiverType, ref useSiteInfo, isMethodGroupConversion: false);
                                if (!conversion.Exists)
                                {
                                    continue; // Conversion to 'this' parameter failed
                                }
                            }
                        }
                        else if (typeParameters.Any((typeParameter, parameter) => !parameter.Type.ContainsTypeParameter(typeParameter), member.Parameters[0]))
                        {
                            // A type parameter does not appear in the parameter type.
                            continue;
                        }
                    }

                    resultBuilder.Add(member);
                }

                return resultBuilder.ToImmutableAndFree();
            }

            // This is what BindInvocationExpressionContinued is doing in terms of reporting diagnostics and detecting a failure
            static bool bindInvocationExpressionContinued(
                Binder addMethodBinder,
                SyntaxNode node,
                SyntaxNode expression,
                OverloadResolutionResult<MethodSymbol> result,
                AnalyzedArguments analyzedArguments,
                MethodGroup methodGroup,
                BindingDiagnosticBag diagnostics,
                out MethodSymbol? addMethod)
            {
                Debug.Assert(node != null);
                Debug.Assert(methodGroup != null);
                Debug.Assert(methodGroup.Error == null);
                Debug.Assert(methodGroup.Methods.Count > 0);

                var invokedAsExtensionMethod = methodGroup.IsExtensionMethodGroup;

                // We have already determined that we are not in a situation where we can successfully do
                // a dynamic binding. We might be in one of the following situations:
                //
                // * There were dynamic arguments but overload resolution still found zero applicable candidates.
                // * There were no dynamic arguments and overload resolution found zero applicable candidates.
                // * There were no dynamic arguments and overload resolution found multiple applicable candidates
                //   without being able to find the best one.
                //
                // In those three situations we might give an additional error.

                if (!result.Succeeded)
                {
                    // Since there were no argument errors to report, we report an error on the invocation itself.
                    result.ReportDiagnostics(
                        binder: addMethodBinder, location: GetLocationForOverloadResolutionDiagnostic(node, expression), nodeOpt: node, diagnostics: diagnostics, name: WellKnownMemberNames.CollectionInitializerAddMethodName,
                        receiver: methodGroup.Receiver, invokedExpression: expression, arguments: analyzedArguments, memberGroup: methodGroup.Methods.ToImmutable(),
                        typeContainingConstructor: null, delegateTypeBeingInvoked: null, queryClause: null);

                    addMethod = null;
                    return false;
                }

                // Otherwise, there were no dynamic arguments and overload resolution found a unique best candidate. 
                // We still have to determine if it passes final validation.

                var methodResult = result.ValidResult;
                var method = methodResult.Member;

                // Tracked by https://github.com/dotnet/roslyn/issues/76130: It looks like we added a bunch of code in BindInvocationExpressionContinued at this position
                //            that specifically deals with new extension methods. It adjusts analyzedArguments, etc.
                //            It is very likely we need to do the same here. 

                // It is possible that overload resolution succeeded, but we have chosen an
                // instance method and we're in a static method. A careful reading of the
                // overload resolution spec shows that the "final validation" stage allows an
                // "implicit this" on any method call, not just method calls from inside
                // instance methods. Therefore we must detect this scenario here, rather than in
                // overload resolution.

                var receiver = methodGroup.Receiver;

                // Note: we specifically want to do final validation (7.6.5.1) without checking delegate compatibility (15.2),
                // so we're calling MethodGroupFinalValidation directly, rather than via MethodGroupConversionHasErrors.
                // Note: final validation wants the receiver that corresponds to the source representation
                // (i.e. the first argument, if invokedAsExtensionMethod).
                var gotError = addMethodBinder.MemberGroupFinalValidation(receiver, method, expression, diagnostics, invokedAsExtensionMethod);

                addMethodBinder.ReportDiagnosticsIfObsolete(diagnostics, method, node, hasBaseReceiver: false);
                ReportDiagnosticsIfUnmanagedCallersOnly(diagnostics, method, node, isDelegateConversion: false);

                // No use site errors, but there could be use site warnings.
                // If there are any use site warnings, they have already been reported by overload resolution.
                Debug.Assert(!method.HasUseSiteError, "Shouldn't have reached this point if there were use site errors.");
                Debug.Assert(!method.IsRuntimeFinalizer());

                addMethod = method;
                return !gotError;
            }
        }

        /// <summary>
        /// If the element is from a collection type where elements are added with collection initializers,
        /// return the argument to the collection initializer Add method or null if the element is not a
        /// collection initializer node. Otherwise, return the element as is.
        /// </summary>
        internal static BoundExpression GetUnderlyingCollectionExpressionElement(BoundCollectionExpression expr, BoundExpression element, bool throwOnErrors)
        {
            if (expr.CollectionTypeKind is CollectionExpressionTypeKind.ImplementsIEnumerable)
            {
                // PROTOTYPE: For a k:v element, or for an expression element with a KeyValuePair<,> variance conversion,
                // simply unwrapping call to get the argument is not sufficient because a KeyValuePair<,> instance
                // of the expected collection element type was created in ConvertCollectionExpressionElements().
                switch (element)
                {
                    case BoundCollectionElementInitializer collectionInitializer:
                        return getCollectionInitializerElement(collectionInitializer);
                    case BoundDynamicCollectionElementInitializer dynamicInitializer:
                        return dynamicInitializer.Arguments[0];
                }

                if (throwOnErrors)
                {
                    throw ExceptionUtilities.UnexpectedValue(element);
                }

                // Handle error cases from bindCollectionInitializerElementAddMethod.
                switch (element)
                {
                    case BoundCall call:
                        // Overload resolution failed with one or more applicable or ambiguous
                        // Add methods. This case can be hit for spreads and non-spread elements.
                        Debug.Assert(call.HasErrors);
                        Debug.Assert(call.Method.Name == "Add");
                        return call.Arguments[call.InvokedAsExtensionMethod ? 1 : 0]; // Tracked by https://github.com/dotnet/roslyn/issues/76130: Add test coverage for new extensions
                    case BoundBadExpression badExpression:
                        Debug.Assert(false); // Add test if we hit this assert.
                        return badExpression;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(element);
                }
            }

            return element;

            static BoundExpression getCollectionInitializerElement(BoundCollectionElementInitializer collectionInitializer)
            {
                int argIndex = collectionInitializer.InvokedAsExtensionMethod ? 1 : 0;
                var arg = collectionInitializer.Arguments[argIndex];
                Debug.Assert(!collectionInitializer.DefaultArguments[argIndex]);
                if (collectionInitializer.Expanded && argIndex == collectionInitializer.AddMethod.ParameterCount - 1)
                {
                    if (arg.IsParamsArrayOrCollection)
                    {
                        if (arg is BoundArrayCreation { InitializerOpt.Initializers: [var arrayElement] })
                        {
                            return arrayElement;
                        }
                        else if (arg is BoundConversion { Operand: BoundCollectionExpression { Elements: [BoundExpression collectionElement] } })
                        {
                            return collectionElement;
                        }
                    }

                    Debug.Assert(false);
                }
                return arg;
            }
        }

        internal bool TryGetCollectionIterationType(SyntaxNode syntax, TypeSymbol collectionType, out TypeWithAnnotations iterationType)
        {
            BoundExpression collectionExpr = new BoundValuePlaceholder(syntax, collectionType);
            bool result = GetEnumeratorInfoAndInferCollectionElementType(
                syntax,
                syntax,
                ref collectionExpr,
                isAsync: false,
                isSpread: false,
                BindingDiagnosticBag.Discarded,
                out iterationType,
                builder: out var builder);
            // Collection expression target types require instance method GetEnumerator.
            if (result && builder.ViaExtensionMethod) // Tracked by https://github.com/dotnet/roslyn/issues/76130: Add test coverage for new extensions
            {
                iterationType = default;
                return false;
            }
            return result;
        }

        private BoundCollectionExpression BindCollectionExpressionForErrorRecovery(
            BoundUnconvertedCollectionExpression node,
            TypeSymbol targetType,
            bool inConversion,
            BindingDiagnosticBag diagnostics)
        {
            var syntax = node.Syntax;
            var builder = ArrayBuilder<BoundNode>.GetInstance(node.Elements.Length);
            bool reportNoTargetType = !targetType.IsErrorType();
            foreach (var element in node.Elements)
            {
                var result = element switch
                {
                    BoundCollectionExpressionSpreadElement spreadElement => (BoundNode)spreadElement,
                    BoundKeyValuePairElement keyValuePairElement =>
                        keyValuePairElement.Update(
                            BindToNaturalType(keyValuePairElement.Key, diagnostics, reportNoTargetType),
                            BindToNaturalType(keyValuePairElement.Value, diagnostics, reportNoTargetType)),
                    BoundCollectionExpressionWithElement withElement => bindArgumentsToNaturalType(withElement, diagnostics, reportNoTargetType),
                    _ => BindToNaturalType((BoundExpression)element, diagnostics, reportNoTargetType)
                };
                builder.Add(result);
            }
            return new BoundCollectionExpression(
                syntax,
                collectionTypeKind: CollectionExpressionTypeKind.None,
                placeholder: null,
                collectionCreation: null,
                collectionBuilderSpanPlaceholder: null,
                indexerSetMethod: null,
                wasTargetTyped: inConversion,
                node,
                elements: builder.ToImmutableAndFree(),
                targetType,
                hasErrors: true)
            { WasCompilerGenerated = node.IsParamsArrayOrCollection, IsParamsArrayOrCollection = node.IsParamsArrayOrCollection };

            BoundCollectionExpressionWithElement bindArgumentsToNaturalType(BoundCollectionExpressionWithElement withElement, BindingDiagnosticBag diagnostics, bool reportNoTargetType)
            {
                var arguments = withElement.Arguments;
                var builder = ArrayBuilder<BoundExpression>.GetInstance(arguments.Length);
                foreach (var argument in arguments)
                {
                    builder.Add(BindToNaturalType(argument, diagnostics, reportNoTargetType));
                }
                return withElement.Update(
                    builder.ToImmutableAndFree(),
                    withElement.ArgumentNamesOpt,
                    withElement.ArgumentRefKindsOpt,
                    withElement.Binder);
            }
        }

        internal void GenerateImplicitConversionErrorForCollectionExpression(
            BoundUnconvertedCollectionExpression node,
            TypeSymbol targetType,
            BindingDiagnosticBag diagnostics)
        {
            var collectionTypeKind = ConversionsBase.GetCollectionExpressionTypeKind(Compilation, targetType, out TypeWithAnnotations elementTypeWithAnnotations);
            switch (collectionTypeKind)
            {
                case CollectionExpressionTypeKind.ImplementsIEnumerable:
                case CollectionExpressionTypeKind.CollectionBuilder:
                    Debug.Assert(elementTypeWithAnnotations.Type is null); // GetCollectionExpressionTypeKind() does not set elementType for these cases.
                    if (!TryGetCollectionIterationType(node.Syntax, targetType, out elementTypeWithAnnotations))
                    {
                        Error(
                            diagnostics,
                            collectionTypeKind == CollectionExpressionTypeKind.CollectionBuilder ?
                                ErrorCode.ERR_CollectionBuilderNoElementType :
                                ErrorCode.ERR_CollectionExpressionTargetNoElementType,
                            node.Syntax,
                            targetType);
                        return;
                    }
                    Debug.Assert(elementTypeWithAnnotations.HasType);
                    break;
            }

            bool reportedErrors = false;

            if (collectionTypeKind != CollectionExpressionTypeKind.None)
            {
                var elements = node.Elements;
                var elementType = elementTypeWithAnnotations.Type;
                Debug.Assert(elementType is { });

                if (collectionTypeKind == CollectionExpressionTypeKind.ImplementsIEnumerable)
                {
                    if (!HasCollectionExpressionApplicableConstructor(node.Syntax, targetType, constructor: out _, isExpanded: out _, diagnostics))
                    {
                        reportedErrors = true;
                    }

                    // https://github.com/dotnet/roslyn/issues/77879: Report diagnostics when GetCollectionExpressionApplicableIndexer() returns non-null?
                    if (GetCollectionExpressionApplicableIndexer(node.Syntax, targetType, elementTypeWithAnnotations.Type, BindingDiagnosticBag.Discarded) is { })
                    {
                        collectionTypeKind = CollectionExpressionTypeKind.ImplementsIEnumerableWithIndexer;
                    }
                    else if (elements.Length > 0 &&
                        !HasCollectionExpressionApplicableAddMethod(node.Syntax, targetType, addMethods: out _, diagnostics))
                    {
                        reportedErrors = true;
                    }
                }

                // Compare with similar loop in Conversions.GetCollectionExpressionConversion().
                var keyValueTypes = ConversionsBase.TryGetCollectionKeyValuePairTypes(Compilation, elementType);
                var useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
                foreach (var element in elements)
                {
                    switch (element)
                    {
                        case BoundCollectionExpressionWithElement:
                            // Collection arguments do not affect convertibility.
                            break;
                        case BoundExpression expressionElement:
                            {
                                var expressionSyntax = expressionElement.Syntax;
                                var elementConversion = Conversions.ClassifyImplicitConversionFromExpression(expressionElement, elementType, ref useSiteInfo);
                                if (elementConversion.Exists)
                                {
                                    continue;
                                }
                                else if (expressionElement.Type is { } &&
                                    keyValueTypes is (var keyType, var valueType) &&
                                    ConversionsBase.IsKeyValuePairType(Compilation, expressionElement.Type, out var elementKeyType, out var elementValueType))
                                {
                                    generateImplicitConversionFromTypeError(diagnostics, expressionSyntax, elementKeyType, keyType, ref useSiteInfo, ref reportedErrors);
                                    generateImplicitConversionFromTypeError(diagnostics, expressionSyntax, elementValueType, valueType, ref useSiteInfo, ref reportedErrors);
                                }
                                else
                                {
                                    GenerateImplicitConversionError(diagnostics, expressionSyntax, elementConversion, expressionElement, elementType);
                                    reportedErrors = true;
                                }
                            }
                            break;
                        case BoundCollectionExpressionSpreadElement spreadElement:
                            {
                                var expressionSyntax = spreadElement.Expression.Syntax;
                                var enumeratorInfo = spreadElement.EnumeratorInfoOpt;
                                if (enumeratorInfo is null)
                                {
                                    Error(diagnostics, ErrorCode.ERR_NoImplicitConv, expressionSyntax, spreadElement.Expression.Display, elementType);
                                    reportedErrors = true;
                                }
                                else
                                {
                                    var elementConversion = Conversions.GetCollectionExpressionSpreadElementConversion(expressionSyntax, elementType, enumeratorInfo, ref useSiteInfo);
                                    if (elementConversion.Exists)
                                    {
                                        continue;
                                    }
                                    else if (keyValueTypes is (var keyType, var valueType) &&
                                        ConversionsBase.IsKeyValuePairType(Compilation, enumeratorInfo.ElementType, out var itemKeyType, out var itemValueType))
                                    {
                                        generateImplicitConversionFromTypeError(diagnostics, expressionSyntax, itemKeyType, keyType, ref useSiteInfo, ref reportedErrors);
                                        generateImplicitConversionFromTypeError(diagnostics, expressionSyntax, itemValueType, valueType, ref useSiteInfo, ref reportedErrors);
                                    }
                                    else
                                    {
                                        GenerateImplicitConversionError(diagnostics, Compilation, expressionSyntax, elementConversion, enumeratorInfo.ElementType, elementType);
                                        reportedErrors = true;
                                    }
                                }
                            }
                            break;
                        case BoundKeyValuePairElement keyValuePairElement:
                            {
                                if (keyValueTypes is (var keyType, var valueType))
                                {
                                    generateImplicitConversionFromExpressionError(diagnostics, keyValuePairElement.Key, keyType, ref useSiteInfo, ref reportedErrors);
                                    generateImplicitConversionFromExpressionError(diagnostics, keyValuePairElement.Value, valueType, ref useSiteInfo, ref reportedErrors);
                                }
                                else
                                {
                                    Error(diagnostics, ErrorCode.ERR_CollectionExpressionKeyValuePairNotSupported, keyValuePairElement.Syntax, targetType);
                                    reportedErrors = true;
                                }
                            }
                            break;
                        default:
                            Debug.Assert(false);
                            break;
                    }
                }
                Debug.Assert(reportedErrors);
            }

            if (!reportedErrors)
            {
                Error(diagnostics, ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, node.Syntax, targetType);
            }

            return;

            void generateImplicitConversionFromExpressionError(
                BindingDiagnosticBag diagnostics,
                BoundExpression expression,
                TypeSymbol targetType,
                ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo,
                ref bool reportedErrors)
            {
                Conversion elementConversion = Conversions.ClassifyImplicitConversionFromExpression(expression, targetType, ref useSiteInfo);
                if (!elementConversion.Exists)
                {
                    GenerateImplicitConversionError(diagnostics, expression.Syntax, elementConversion, expression, targetType);
                    reportedErrors = true;
                }
            }

            void generateImplicitConversionFromTypeError(
                BindingDiagnosticBag diagnostics,
                SyntaxNode syntax,
                TypeSymbol sourceType,
                TypeSymbol targetType,
                ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo,
                ref bool reportedErrors)
            {
                Conversion elementConversion = Conversions.ClassifyImplicitConversionFromType(sourceType, targetType, ref useSiteInfo);
                if (!elementConversion.Exists)
                {
                    GenerateImplicitConversionError(diagnostics, Compilation, syntax, elementConversion, sourceType, targetType);
                    reportedErrors = true;
                }
            }
        }

        private MethodSymbol? GetCollectionBuilderMethod(
            NamedTypeSymbol targetType,
            TypeSymbol elementType,
            NamedTypeSymbol builderType,
            string methodName,
            ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            Debug.Assert(targetType.IsDefinition);
            Debug.Assert(builderType.IsDefinition);
            Debug.Assert(!builderType.IsGenericType);

            var readOnlySpanType = Compilation.GetWellKnownType(WellKnownType.System_ReadOnlySpan_T);

            foreach (var candidate in builderType.GetMembers(methodName))
            {
                if (candidate is not MethodSymbol { IsStatic: true } method)
                {
                    continue;
                }

                Debug.Assert(method.IsDefinition);
                var candidateUseSiteInfo = new CompoundUseSiteInfo<AssemblySymbol>(useSiteInfo);
                if (!IsAccessible(method, ref candidateUseSiteInfo))
                {
                    continue;
                }

                var allTypeParameters = TypeMap.TypeParametersAsTypeSymbolsWithAnnotations(targetType.GetAllTypeParameters());
                if (method.Arity != allTypeParameters.Length)
                {
                    continue;
                }

                if (method.Parameters is not [{ RefKind: RefKind.None, Type: var parameterType }]
                    || !readOnlySpanType.Equals(parameterType.OriginalDefinition, TypeCompareKind.AllIgnoreOptions))
                {
                    continue;
                }

                MethodSymbol methodWithTargetTypeParameters = allTypeParameters.IsEmpty ? // builder method substituted with type parameters from target type
                    method :
                    method.Construct(allTypeParameters);

                var spanTypeArg = ((NamedTypeSymbol)methodWithTargetTypeParameters.Parameters[0].Type).TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0].Type;
                var conversion = Conversions.ClassifyImplicitConversionFromType(elementType, spanTypeArg, ref candidateUseSiteInfo);
                if (!conversion.IsIdentity)
                {
                    continue;
                }

                conversion = Conversions.ClassifyImplicitConversionFromType(methodWithTargetTypeParameters.ReturnType, targetType, ref candidateUseSiteInfo);
                switch (conversion.Kind)
                {
                    case ConversionKind.Identity:
                    case ConversionKind.ImplicitReference:
                    case ConversionKind.Boxing:
                        break;
                    default:
                        continue;
                }

                useSiteInfo.AddDiagnostics(candidateUseSiteInfo.Diagnostics);
                return method;
            }

            return null;
        }

        /// <summary>
        /// Rewrite the subexpressions in a conditional expression to convert the whole thing to the destination type.
        /// </summary>
        private BoundExpression ConvertConditionalExpression(
            BoundUnconvertedConditionalOperator source,
            TypeSymbol destination,
            Conversion? conversionIfTargetTyped,
            BindingDiagnosticBag diagnostics,
            bool hasErrors = false)
        {
            bool targetTyped = conversionIfTargetTyped is { };
            Debug.Assert(targetTyped || destination.IsErrorType() || destination.Equals(source.Type, TypeCompareKind.ConsiderEverything));
            var conversion = conversionIfTargetTyped.GetValueOrDefault();
            ImmutableArray<Conversion> underlyingConversions = conversion.UnderlyingConversions;
            var condition = source.Condition;
            hasErrors |= source.HasErrors || destination.IsErrorType();

            var trueExpr =
                targetTyped
                ? CreateConversion(source.Consequence.Syntax, source.Consequence, underlyingConversions[0], isCast: false, conversionGroupOpt: null, destination, diagnostics)
                : GenerateConversionForAssignment(destination, source.Consequence, diagnostics);
            var falseExpr =
                targetTyped
                ? CreateConversion(source.Alternative.Syntax, source.Alternative, underlyingConversions[1], isCast: false, conversionGroupOpt: null, destination, diagnostics)
                : GenerateConversionForAssignment(destination, source.Alternative, diagnostics);
            conversion.MarkUnderlyingConversionsChecked();
            var constantValue = FoldConditionalOperator(condition, trueExpr, falseExpr);
            hasErrors |= constantValue?.IsBad == true;
            if (targetTyped && !destination.IsErrorType() && !Compilation.IsFeatureEnabled(MessageID.IDS_FeatureTargetTypedConditional))
            {
                diagnostics.Add(
                    ErrorCode.ERR_NoImplicitConvTargetTypedConditional,
                    source.Syntax.Location,
                    Compilation.LanguageVersion.ToDisplayString(),
                    source.Consequence.Display,
                    source.Alternative.Display,
                    new CSharpRequiredLanguageVersion(MessageID.IDS_FeatureTargetTypedConditional.RequiredVersion()));
            }

            return new BoundConditionalOperator(source.Syntax, isRef: false, condition, trueExpr, falseExpr, constantValue, source.Type, wasTargetTyped: targetTyped, destination, hasErrors)
                .WithSuppression(source.IsSuppressed);
        }

        /// <summary>
        /// Rewrite the expressions in the switch expression arms to add a conversion to the destination type.
        /// </summary>
        private BoundExpression ConvertSwitchExpression(BoundUnconvertedSwitchExpression source, TypeSymbol destination, Conversion? conversionIfTargetTyped, BindingDiagnosticBag diagnostics, bool hasErrors = false)
        {
            bool targetTyped = conversionIfTargetTyped is { };
            Conversion conversion = conversionIfTargetTyped ?? Conversion.Identity;
            Debug.Assert(targetTyped || destination.IsErrorType() || destination.Equals(source.Type, TypeCompareKind.ConsiderEverything));
            ImmutableArray<Conversion> underlyingConversions = conversion.UnderlyingConversions;
            var builder = ArrayBuilder<BoundSwitchExpressionArm>.GetInstance(source.SwitchArms.Length);
            for (int i = 0, n = source.SwitchArms.Length; i < n; i++)
            {
                var oldCase = source.SwitchArms[i];
                var oldValue = oldCase.Value;
                var newValue =
                    targetTyped
                    ? CreateConversion(oldValue.Syntax, oldValue, underlyingConversions[i], isCast: false, conversionGroupOpt: null, destination, diagnostics)
                    : GenerateConversionForAssignment(destination, oldValue, diagnostics);
                var newCase = (oldValue == newValue) ? oldCase :
                    new BoundSwitchExpressionArm(oldCase.Syntax, oldCase.Locals, oldCase.Pattern, oldCase.WhenClause, newValue, oldCase.Label, oldCase.HasErrors);
                builder.Add(newCase);
            }
            conversion.MarkUnderlyingConversionsChecked();

            var newSwitchArms = builder.ToImmutableAndFree();
            return new BoundConvertedSwitchExpression(
                source.Syntax, source.Type, targetTyped, source.Expression, newSwitchArms, source.ReachabilityDecisionDag,
                source.DefaultLabel, source.ReportedNotExhaustive, destination, hasErrors || source.HasErrors).WithSuppression(source.IsSuppressed);
        }

        private BoundExpression CreateUserDefinedConversion(
            SyntaxNode syntax,
            BoundExpression source,
            Conversion conversion,
            bool isCast,
            ConversionGroup conversionGroup,
            TypeSymbol destination,
            BindingDiagnosticBag diagnostics,
            bool hasErrors)
        {
            Debug.Assert(conversionGroup != null);
            Debug.Assert(conversion.IsUserDefined);

            conversion.MarkUnderlyingConversionsChecked();
            if (!conversion.IsValid)
            {
                if (!hasErrors)
                    GenerateImplicitConversionError(diagnostics, syntax, conversion, source, destination);

                return new BoundConversion(
                    syntax,
                    BindToNaturalType(source, diagnostics),
                    conversion,
                    CheckOverflowAtRuntime,
                    explicitCastInCode: isCast,
                    conversionGroup,
                    constantValueOpt: ConstantValue.NotAvailable,
                    type: destination,
                    hasErrors: true)
                { WasCompilerGenerated = source.WasCompilerGenerated };
            }

            // Due to an oddity in the way we create a non-lifted user-defined conversion from A to D? 
            // (required backwards compatibility with the native compiler) we can end up in a situation 
            // where we have:
            // a standard conversion from A to B?
            // then a standard conversion from B? to B
            // then a user-defined  conversion from B to C
            // then a standard conversion from C to C? 
            // then a standard conversion from C? to D?
            //
            // In that scenario, the "from type" of the conversion will be B? and the "from conversion" will be 
            // from A to B?. Similarly the "to type" of the conversion will be C? and the "to conversion"
            // of the conversion will be from C? to D?.
            //
            // Therefore, we might need to introduce an extra conversion on the source side, from B? to B.
            // Now, you might think we should also introduce an extra conversion on the destination side,
            // from C to C?. But that then gives us the following bad situation: If we in fact bind this as
            //
            // (D?)(C?)(C)(B)(B?)(A)x 
            //
            // then what we are in effect doing is saying "convert C? to D? by checking for null, unwrapping,
            // converting C to D, and then wrapping". But we know that the C? will never be null. In this case
            // we should actually generate
            //
            // (D?)(C)(B)(B?)(A)x
            //
            // And thereby skip the unnecessary nullable conversion.

            Debug.Assert(conversion.BestUserDefinedConversionAnalysis is object); // All valid user-defined conversions have this populated

            // Original expression --> conversion's "from" type
            BoundExpression convertedOperand = CreateConversion(
                syntax: source.Syntax,
                source: source,
                conversion: conversion.UserDefinedFromConversion,
                isCast: false,
                conversionGroupOpt: conversionGroup,
                wasCompilerGenerated: false,
                destination: conversion.BestUserDefinedConversionAnalysis.FromType,
                diagnostics: diagnostics);

            TypeSymbol conversionParameterType = conversion.BestUserDefinedConversionAnalysis.Operator.GetParameterType(0);
            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);

            if (conversion.BestUserDefinedConversionAnalysis.Kind == UserDefinedConversionAnalysisKind.ApplicableInNormalForm &&
                !TypeSymbol.Equals(conversion.BestUserDefinedConversionAnalysis.FromType, conversionParameterType, TypeCompareKind.ConsiderEverything2))
            {
                // Conversion's "from" type --> conversion method's parameter type.
                convertedOperand = CreateConversion(
                    syntax: syntax,
                    source: convertedOperand,
                    conversion: Conversions.ClassifyStandardConversion(convertedOperand.Type, conversionParameterType, ref useSiteInfo),
                    isCast: false,
                    conversionGroupOpt: conversionGroup,
                    wasCompilerGenerated: true,
                    destination: conversionParameterType,
                    diagnostics: diagnostics);
            }

            BoundExpression userDefinedConversion;

            TypeSymbol conversionReturnType = conversion.BestUserDefinedConversionAnalysis.Operator.ReturnType;
            TypeSymbol conversionToType = conversion.BestUserDefinedConversionAnalysis.ToType;
            Conversion toConversion = conversion.UserDefinedToConversion;

            if (conversion.BestUserDefinedConversionAnalysis.Kind == UserDefinedConversionAnalysisKind.ApplicableInNormalForm &&
                !TypeSymbol.Equals(conversionToType, conversionReturnType, TypeCompareKind.ConsiderEverything2))
            {
                // Conversion method's parameter type --> conversion method's return type
                // NB: not calling CreateConversion here because this is the recursive base case.
                userDefinedConversion = new BoundConversion(
                    syntax,
                    convertedOperand,
                    conversion,
                    @checked: CheckOverflowAtRuntime,
                    explicitCastInCode: isCast,
                    conversionGroup,
                    constantValueOpt: ConstantValue.NotAvailable,
                    type: conversionReturnType)
                { WasCompilerGenerated = true };

                if (conversionToType.IsNullableType() && TypeSymbol.Equals(conversionToType.GetNullableUnderlyingType(), conversionReturnType, TypeCompareKind.ConsiderEverything2))
                {
                    // Skip introducing the conversion from C to C?.  The "to" conversion is now wrong though,
                    // because it will still assume converting C? to D?. 

                    toConversion.MarkUnderlyingConversionsCheckedRecursive();
                    toConversion = Conversions.ClassifyConversionFromType(conversionReturnType, destination, isChecked: CheckOverflowAtRuntime, ref useSiteInfo);
                    Debug.Assert(toConversion.Exists);
                }
                else
                {
                    // Conversion method's return type --> conversion's "to" type
                    userDefinedConversion = CreateConversion(
                        syntax: syntax,
                        source: userDefinedConversion,
                        conversion: Conversions.ClassifyStandardConversion(conversionReturnType, conversionToType, ref useSiteInfo),
                        isCast: false,
                        conversionGroupOpt: conversionGroup,
                        wasCompilerGenerated: true,
                        destination: conversionToType,
                        diagnostics: diagnostics);
                }
            }
            else
            {
                // Conversion method's parameter type --> conversion method's "to" type
                // NB: not calling CreateConversion here because this is the recursive base case.
                userDefinedConversion = new BoundConversion(
                    syntax,
                    convertedOperand,
                    conversion,
                    @checked: CheckOverflowAtRuntime,
                    explicitCastInCode: isCast,
                    conversionGroup,
                    constantValueOpt: ConstantValue.NotAvailable,
                    type: conversionToType)
                { WasCompilerGenerated = true };
            }

            diagnostics.Add(syntax, useSiteInfo);

            // Conversion's "to" type --> final type
            BoundExpression finalConversion = CreateConversion(
                syntax: syntax,
                source: userDefinedConversion,
                conversion: toConversion,
                isCast: false,
                conversionGroupOpt: conversionGroup,
                wasCompilerGenerated: true, // NOTE: doesn't necessarily set flag on resulting bound expression.
                destination: destination,
                diagnostics: diagnostics);

            conversion.AssertUnderlyingConversionsCheckedRecursive();

            finalConversion.ResetCompilerGenerated(source.WasCompilerGenerated);

            return finalConversion;
        }

        private BoundExpression CreateFunctionTypeConversion(SyntaxNode syntax, BoundExpression source, Conversion conversion, bool isCast, ConversionGroup? conversionGroup, TypeSymbol destination, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(conversion.Kind == ConversionKind.FunctionType);
            Debug.Assert(source.Kind is BoundKind.MethodGroup or BoundKind.UnboundLambda);
            Debug.Assert(syntax.IsFeatureEnabled(MessageID.IDS_FeatureInferredDelegateType));

            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
            var delegateType = source.GetInferredDelegateType(ref useSiteInfo);
            Debug.Assert(delegateType is { });

            if (source.Kind == BoundKind.UnboundLambda &&
                destination.IsNonGenericExpressionType())
            {
                delegateType = Compilation.GetWellKnownType(WellKnownType.System_Linq_Expressions_Expression_T).Construct(delegateType);
                delegateType.AddUseSiteInfo(ref useSiteInfo);
            }

            conversion = Conversions.ClassifyConversionFromExpression(source, delegateType, isChecked: CheckOverflowAtRuntime, ref useSiteInfo);
            bool warnOnMethodGroupConversion =
                source.Kind == BoundKind.MethodGroup &&
                !isCast &&
                conversion.Exists &&
                destination.SpecialType == SpecialType.System_Object;
            BoundExpression expr;
            if (!conversion.Exists)
            {
                GenerateImplicitConversionError(diagnostics, syntax, conversion, source, delegateType);
                expr = new BoundConversion(syntax, source, conversion, @checked: false, explicitCastInCode: isCast, conversionGroup, constantValueOpt: ConstantValue.NotAvailable, type: delegateType, hasErrors: true) { WasCompilerGenerated = source.WasCompilerGenerated };
            }
            else
            {
                expr = CreateConversion(syntax, source, conversion, isCast, conversionGroup, delegateType, diagnostics);
            }

            conversion = Conversions.ClassifyConversionFromExpression(expr, destination, isChecked: CheckOverflowAtRuntime, ref useSiteInfo);
            if (!conversion.Exists)
            {
                GenerateImplicitConversionError(diagnostics, syntax, conversion, source, destination);
            }
            else if (warnOnMethodGroupConversion)
            {
                Error(diagnostics, ErrorCode.WRN_MethGrpToNonDel, syntax, ((BoundMethodGroup)source).Name, destination);
            }

            diagnostics.Add(syntax, useSiteInfo);
            return CreateConversion(syntax, expr, conversion, isCast, conversionGroup, destination, diagnostics);
        }

        private BoundExpression CreateAnonymousFunctionConversion(SyntaxNode syntax, BoundExpression source, Conversion conversion, bool isCast, ConversionGroup? conversionGroup, TypeSymbol destination, BindingDiagnosticBag diagnostics)
        {
            // We have a successful anonymous function conversion; rather than producing a node
            // which is a conversion on top of an unbound lambda, replace it with the bound
            // lambda.

            // UNDONE: Figure out what to do about the error case, where a lambda
            // UNDONE: is converted to a delegate that does not match. What to surface then?

            var unboundLambda = (UnboundLambda)source;
            var boundLambda = unboundLambda.Bind((NamedTypeSymbol)destination, isExpressionTree: destination.IsGenericOrNonGenericExpressionType(out _)).WithInAnonymousFunctionConversion();
            diagnostics.AddRange(boundLambda.Diagnostics);

            CheckParameterModifierMismatchMethodConversion(syntax, boundLambda.Symbol, destination, invokedAsExtensionMethod: false, diagnostics);
            CheckLambdaConversion((LambdaSymbol)boundLambda.Symbol, destination, diagnostics);
            return new BoundConversion(
                syntax,
                boundLambda,
                conversion,
                @checked: false,
                explicitCastInCode: isCast,
                conversionGroup,
                constantValueOpt: ConstantValue.NotAvailable,
                type: destination)
            { WasCompilerGenerated = source.WasCompilerGenerated };
        }

        private BoundExpression CreateMethodGroupConversion(SyntaxNode syntax, BoundExpression source, Conversion conversion, bool isCast, ConversionGroup? conversionGroup, TypeSymbol destination, BindingDiagnosticBag diagnostics)
        {
            var (originalGroup, isAddressOf) = source switch
            {
                BoundMethodGroup m => (m, false),
                BoundUnconvertedAddressOfOperator { Operand: { } m } => (m, true),
                _ => throw ExceptionUtilities.UnexpectedValue(source),
            };
            BoundMethodGroup group = FixMethodGroupWithTypeOrValue(originalGroup, conversion, diagnostics);
            bool hasErrors = false;

            if (MethodGroupConversionHasErrors(syntax, conversion, group.ReceiverOpt, conversion.IsExtensionMethod, isAddressOf, destination, diagnostics))
            {
                hasErrors = true;
            }

            Debug.Assert(conversion.UnderlyingConversions.IsDefault);
            conversion.MarkUnderlyingConversionsChecked();

            return new BoundConversion(syntax, group, conversion, @checked: false, explicitCastInCode: isCast, conversionGroup, constantValueOpt: ConstantValue.NotAvailable, type: destination, hasErrors: hasErrors) { WasCompilerGenerated = group.WasCompilerGenerated };
        }

        private static void CheckParameterModifierMismatchMethodConversion(SyntaxNode syntax, MethodSymbol lambdaOrMethod, TypeSymbol targetType, bool invokedAsExtensionMethod, BindingDiagnosticBag diagnostics)
        {
            MethodSymbol? delegateMethod;
            if (targetType.GetDelegateType() is { } delegateType)
            {
                delegateMethod = delegateType.DelegateInvokeMethod;
            }
            else if (targetType is FunctionPointerTypeSymbol functionPointerType)
            {
                delegateMethod = functionPointerType.Signature;
            }
            else
            {
                return;
            }

            if (SourceMemberContainerTypeSymbol.RequiresValidScopedOverrideForRefSafety(delegateMethod))
            {
                SourceMemberContainerTypeSymbol.CheckValidScopedOverride(
                    delegateMethod,
                    lambdaOrMethod,
                    diagnostics,
                    static (diagnostics, delegateMethod, lambdaOrMethod, parameter, _, typeAndSyntax) =>
                    {
                        diagnostics.Add(
                            SourceMemberContainerTypeSymbol.ReportInvalidScopedOverrideAsError(delegateMethod, lambdaOrMethod) ?
                                ErrorCode.ERR_ScopedMismatchInParameterOfTarget :
                                ErrorCode.WRN_ScopedMismatchInParameterOfTarget,
                            typeAndSyntax.Syntax.Location,
                            new FormattedSymbol(parameter, SymbolDisplayFormat.ShortFormat),
                            typeAndSyntax.Type);
                    },
                    (Type: targetType, Syntax: syntax),
                    allowVariance: true,
                    invokedAsExtensionMethod: invokedAsExtensionMethod);
            }

            SourceMemberContainerTypeSymbol.CheckRefReadonlyInMismatch(
                delegateMethod, lambdaOrMethod, diagnostics,
                static (diagnostics, delegateMethod, lambdaOrMethod, lambdaOrMethodParameter, _, arg) =>
                {
                    var (delegateParameter, location) = arg;
                    diagnostics.Add(ErrorCode.WRN_TargetDifferentRefness, location, lambdaOrMethodParameter, delegateParameter);
                },
                syntax.Location,
                invokedAsExtensionMethod: invokedAsExtensionMethod);
        }

        private static void CheckLambdaConversion(LambdaSymbol lambdaSymbol, TypeSymbol targetType, BindingDiagnosticBag diagnostics)
        {
            var delegateType = targetType.GetDelegateType();
            Debug.Assert(delegateType is not null);
            var isSynthesized = delegateType.DelegateInvokeMethod?.OriginalDefinition is SynthesizedDelegateInvokeMethod;
            var delegateParameters = delegateType.DelegateParameters();

            Debug.Assert(lambdaSymbol.ParameterCount == delegateParameters.Length);
            for (int p = 0; p < lambdaSymbol.ParameterCount; p++)
            {
                var lambdaParameter = lambdaSymbol.Parameters[p];
                var delegateParameter = delegateParameters[p];

                if (isSynthesized)
                {
                    // If synthesizing a delegate with `decimal`/`DateTime` default value,
                    // check that the corresponding `*ConstantAttribute` is available.
                    if (delegateParameter.ExplicitDefaultConstantValue is { } defaultValue &&
                        // Skip reporting this diagnostic if already reported in `SourceComplexParameterSymbolBase.DefaultSyntaxValue`.
                        lambdaParameter is not SourceComplexParameterSymbolBase
                        {
                            ExplicitDefaultConstantValue.IsDecimal: true,
                            DefaultValueFromAttributes: ConstantValue.NotAvailable
                        })
                    {
                        WellKnownMember? member = defaultValue.SpecialType switch
                        {
                            SpecialType.System_Decimal => WellKnownMember.System_Runtime_CompilerServices_DecimalConstantAttribute__ctor,
                            SpecialType.System_DateTime => WellKnownMember.System_Runtime_CompilerServices_DateTimeConstantAttribute__ctor,
                            _ => null
                        };
                        if (member != null)
                        {
                            reportUseSiteDiagnosticForSynthesizedAttribute(
                                lambdaSymbol,
                                lambdaParameter,
                                member.GetValueOrDefault(),
                                diagnostics);
                        }
                    }

                    // If synthesizing a delegate with an [UnscopedRef] parameter, check the attribute is available.
                    if (delegateParameter.HasUnscopedRefAttribute)
                    {
                        reportUseSiteDiagnosticForSynthesizedAttribute(
                            lambdaSymbol,
                            lambdaParameter,
                            WellKnownMember.System_Diagnostics_CodeAnalysis_UnscopedRefAttribute__ctor,
                            diagnostics);
                    }
                }

                // Warn for defaults/`params` mismatch.
                if (!lambdaSymbol.SyntaxNode.IsKind(SyntaxKind.AnonymousMethodExpression))
                {
                    if (lambdaParameter.HasExplicitDefaultValue &&
                        lambdaParameter.ExplicitDefaultConstantValue is { IsBad: false } lambdaParamDefault)
                    {
                        var delegateParamDefault = delegateParameter.HasExplicitDefaultValue ? delegateParameter.ExplicitDefaultConstantValue : null;
                        if (delegateParamDefault?.IsBad != true && lambdaParamDefault != delegateParamDefault)
                        {
                            // Parameter {0} has default value '{1}' in lambda but '{2}' in target delegate type.
                            Error(diagnostics, ErrorCode.WRN_OptionalParamValueMismatch, lambdaParameter.GetFirstLocation(), p + 1, lambdaParamDefault, delegateParamDefault ?? ((object)MessageID.IDS_Missing.Localize()));
                        }
                    }

                    if (lambdaParameter.IsParams && !delegateParameter.IsParams && p == lambdaSymbol.ParameterCount - 1)
                    {
                        // Parameter {0} has params modifier in lambda but not in target delegate type.
                        Error(diagnostics, ErrorCode.WRN_ParamsArrayInLambdaOnly, lambdaParameter.GetFirstLocation(), p + 1);
                    }
                }
            }

            static void reportUseSiteDiagnosticForSynthesizedAttribute(
                LambdaSymbol lambdaSymbol,
                ParameterSymbol lambdaParameter,
                WellKnownMember member,
                BindingDiagnosticBag diagnostics)
            {
                ReportUseSiteDiagnosticForSynthesizedAttribute(
                    lambdaSymbol.DeclaringCompilation,
                    member,
                    diagnostics,
                    lambdaParameter.TryGetFirstLocation() ?? lambdaSymbol.SyntaxNode.Location);
            }
        }

        private BoundExpression CreateStackAllocConversion(SyntaxNode syntax, BoundExpression source, Conversion conversion, bool isCast, ConversionGroup? conversionGroup, TypeSymbol destination, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(conversion.IsStackAlloc);

            var boundStackAlloc = (BoundStackAllocArrayCreation)source;
            var elementType = boundStackAlloc.ElementType;
            TypeSymbol stackAllocType;

            switch (conversion.Kind)
            {
                case ConversionKind.StackAllocToPointerType:
                    ReportUnsafeIfNotAllowed(syntax.Location, diagnostics);
                    stackAllocType = new PointerTypeSymbol(TypeWithAnnotations.Create(elementType));
                    break;
                case ConversionKind.StackAllocToSpanType:
                    CheckFeatureAvailability(syntax, MessageID.IDS_FeatureRefStructs, diagnostics);
                    stackAllocType = Compilation.GetWellKnownType(WellKnownType.System_Span_T).Construct(elementType);
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(conversion.Kind);
            }

            var convertedNode = new BoundConvertedStackAllocExpression(syntax, elementType, boundStackAlloc.Count, boundStackAlloc.InitializerOpt, stackAllocType, boundStackAlloc.HasErrors);

            var underlyingConversion = conversion.UnderlyingConversions.Single();
            return CreateConversion(syntax, convertedNode, underlyingConversion, isCast: isCast, conversionGroup, destination, diagnostics);
        }

        private BoundExpression CreateTupleLiteralConversion(SyntaxNode syntax, BoundTupleLiteral sourceTuple, Conversion conversion, bool isCast, ConversionGroup? conversionGroup, TypeSymbol destination, BindingDiagnosticBag diagnostics)
        {
            // We have a successful tuple conversion; rather than producing a separate conversion node 
            // which is a conversion on top of a tuple literal, tuple conversion is an element-wise conversion of arguments.
            Debug.Assert(conversion.IsNullable == destination.IsNullableType());

            var destinationWithoutNullable = destination;
            var conversionWithoutNullable = conversion;

            if (conversion.IsNullable)
            {
                destinationWithoutNullable = destination.GetNullableUnderlyingType();
                conversionWithoutNullable = conversion.UnderlyingConversions[0];
                conversion.MarkUnderlyingConversionsChecked();
            }

            Debug.Assert(conversionWithoutNullable.IsTupleLiteralConversion);

            NamedTypeSymbol targetType = (NamedTypeSymbol)destinationWithoutNullable;
            if (targetType.IsTupleType)
            {
                NamedTypeSymbol.ReportTupleNamesMismatchesIfAny(targetType, sourceTuple, diagnostics);

                // do not lose the original element names and locations in the literal if different from names in the target
                //
                // the tuple has changed the type of elements due to target-typing, 
                // but element names has not changed and locations of their declarations 
                // should not be confused with element locations on the target type.

                if (sourceTuple.Type is NamedTypeSymbol { IsTupleType: true } sourceType)
                {
                    targetType = targetType.WithTupleDataFrom(sourceType);
                }
                else
                {
                    var tupleSyntax = (TupleExpressionSyntax)sourceTuple.Syntax;
                    var locationBuilder = ArrayBuilder<Location?>.GetInstance();

                    foreach (var argument in tupleSyntax.Arguments)
                    {
                        locationBuilder.Add(argument.NameColon?.Name.Location);
                    }

                    targetType = targetType.WithElementNames(sourceTuple.ArgumentNamesOpt!,
                        locationBuilder.ToImmutableAndFree(),
                        errorPositions: default,
                        ImmutableArray.Create(tupleSyntax.Location));
                }
            }

            var arguments = sourceTuple.Arguments;
            var convertedArguments = ArrayBuilder<BoundExpression>.GetInstance(arguments.Length);

            var targetElementTypes = targetType.TupleElementTypesWithAnnotations;
            Debug.Assert(targetElementTypes.Length == arguments.Length, "converting a tuple literal to incompatible type?");
            var underlyingConversions = conversionWithoutNullable.UnderlyingConversions;
            conversionWithoutNullable.MarkUnderlyingConversionsChecked();

            for (int i = 0; i < arguments.Length; i++)
            {
                var argument = arguments[i];
                var destType = targetElementTypes[i];
                var elementConversion = underlyingConversions[i];
                var elementConversionGroup = isCast ? new ConversionGroup(elementConversion, destType) : null;
                convertedArguments.Add(CreateConversion(argument.Syntax, argument, elementConversion, isCast: isCast, elementConversionGroup, destType.Type, diagnostics));
            }

            BoundExpression result = new BoundConvertedTupleLiteral(
                sourceTuple.Syntax,
                sourceTuple,
                wasTargetTyped: true,
                convertedArguments.ToImmutableAndFree(),
                sourceTuple.ArgumentNamesOpt,
                sourceTuple.InferredNamesOpt,
                targetType).WithSuppression(sourceTuple.IsSuppressed);

            if (!TypeSymbol.Equals(sourceTuple.Type, destination, TypeCompareKind.ConsiderEverything2))
            {
                // literal cast is applied to the literal 
                result = new BoundConversion(
                    sourceTuple.Syntax,
                    result,
                    conversion,
                    @checked: false,
                    explicitCastInCode: isCast,
                    conversionGroup,
                    constantValueOpt: ConstantValue.NotAvailable,
                    type: destination);
            }

            // If we had a cast in the code, keep conversion in the tree.
            // even though the literal is already converted to the target type.
            if (isCast)
            {
                result = new BoundConversion(
                    syntax,
                    result,
                    Conversion.Identity,
                    @checked: false,
                    explicitCastInCode: isCast,
                    conversionGroup,
                    constantValueOpt: ConstantValue.NotAvailable,
                    type: destination);
            }

            return result;
        }

        private static bool IsMethodGroupWithTypeOrValueReceiver(BoundNode node)
        {
            if (node.Kind != BoundKind.MethodGroup)
            {
                return false;
            }

            return Binder.IsTypeOrValueExpression(((BoundMethodGroup)node).ReceiverOpt);
        }

        private BoundMethodGroup FixMethodGroupWithTypeOrValue(BoundMethodGroup group, Conversion conversion, BindingDiagnosticBag diagnostics)
        {
            if (!IsMethodGroupWithTypeOrValueReceiver(group))
            {
                return group;
            }

            BoundExpression? receiverOpt = group.ReceiverOpt;
            RoslynDebug.Assert(receiverOpt != null);

            receiverOpt = ReplaceTypeOrValueReceiver(receiverOpt, useType: conversion.Method?.RequiresInstanceReceiver == false && !conversion.IsExtensionMethod, diagnostics);
            return group.Update(
                group.TypeArgumentsOpt,
                group.Name,
                group.Methods,
                group.LookupSymbolOpt,
                group.LookupError,
                group.Flags,
                group.FunctionType,
                receiverOpt, //only change
                group.ResultKind);
        }

        /// <summary>
        /// This method implements the algorithm in spec section 7.6.5.1.
        /// 
        /// For method group conversions, there are situations in which the conversion is
        /// considered to exist ("Otherwise the algorithm produces a single best method M having
        /// the same number of parameters as D and the conversion is considered to exist"), but
        /// application of the conversion fails.  These are the "final validation" steps of
        /// overload resolution.
        /// </summary>
        /// <returns>
        /// True if there is any error, except lack of runtime support errors.
        /// </returns>
        private bool MemberGroupFinalValidation(BoundExpression? receiverOpt, MethodSymbol methodSymbol, SyntaxNode node, BindingDiagnosticBag diagnostics, bool invokedAsExtensionMethod)
        {
            if (!IsBadBaseAccess(node, receiverOpt, methodSymbol, diagnostics))
            {
                CheckReceiverAndRuntimeSupportForSymbolAccess(node, receiverOpt, methodSymbol, diagnostics);
            }

            if (MemberGroupFinalValidationAccessibilityChecks(receiverOpt, methodSymbol, node, diagnostics, invokedAsExtensionMethod))
            {
                return true;
            }

            // SPEC: If the best method is a generic method, the type arguments (supplied or inferred) are checked against the constraints 
            // SPEC: declared on the generic method. If any type argument does not satisfy the corresponding constraint(s) on
            // SPEC: the type parameter, a binding-time error occurs.

            // The portion of the overload resolution spec quoted above is subtle and somewhat
            // controversial. The upshot of this is that overload resolution does not consider
            // constraints to be a part of the signature. Overload resolution matches arguments to
            // parameter lists; it does not consider things which are outside of the parameter list.
            // If the best match from the arguments to the formal parameters is not viable then we
            // give an error rather than falling back to a worse match. 
            //
            // Consider the following:
            //
            // void M<T>(T t) where T : Reptile {}
            // void M(object x) {}
            // ...
            // M(new Giraffe());
            //
            // The correct analysis is to determine that the applicable candidates are
            // M<Giraffe>(Giraffe) and M(object). Overload resolution then chooses the former
            // because it is an exact match, over the latter which is an inexact match. Only after
            // the best method is determined do we check the constraints and discover that the
            // constraint on T has been violated.
            // 
            // Note that this is different from the rule that says that during type inference, if an
            // inference violates a constraint then inference fails. For example:
            // 
            // class C<T> where T : struct {}
            // ...
            // void M<U>(U u, C<U> c){}
            // void M(object x, object y) {}
            // ...
            // M("hello", null);
            //
            // Type inference determines that U is string, but since C<string> is not a valid type
            // because of the constraint, type inference fails. M<string> is never added to the
            // applicable candidate set, so the applicable candidate set consists solely of
            // M(object, object) and is therefore the best match.

            return !methodSymbol.CheckConstraints(new ConstraintsHelper.CheckConstraintsArgs(this.Compilation, this.Conversions, includeNullability: false, node.Location, diagnostics));
        }

        /// <summary>
        /// Performs the following checks:
        /// 
        /// Spec 7.6.5: Invocation expressions (definition of Final Validation) 
        ///   The method is validated in the context of the method group: If the best method is a static method, 
        ///   the method group must have resulted from a simple-name or a member-access through a type. If the best 
        ///   method is an instance method, the method group must have resulted from a simple-name, a member-access
        ///   through a variable or value, or a base-access. If neither of these requirements is true, a binding-time
        ///   error occurs.
        ///   (Note that the spec omits to mention, in the case of an instance method invoked through a simple name, that
        ///   the invocation must appear within the body of an instance method)
        ///
        /// Spec 7.5.4: Compile-time checking of dynamic overload resolution 
        ///   If F is a static method, the method group must have resulted from a simple-name, a member-access through a type, 
        ///   or a member-access whose receiver can't be classified as a type or value until after overload resolution (see §7.6.4.1). 
        ///   If F is an instance method, the method group must have resulted from a simple-name, a member-access through a variable or value,
        ///   or a member-access whose receiver can't be classified as a type or value until after overload resolution (see §7.6.4.1).
        /// </summary>
        /// <returns>
        /// True if there is any error.
        /// </returns>
        private bool MemberGroupFinalValidationAccessibilityChecks(BoundExpression? receiverOpt, Symbol memberSymbol, SyntaxNode node, BindingDiagnosticBag diagnostics, bool invokedAsExtensionMethod)
        {
            // Perform final validation of the method to be invoked.

            Debug.Assert(memberSymbol is not MethodSymbol { MethodKind: not MethodKind.Constructor } ||
                memberSymbol.CanBeReferencedByName);
            //note that the same assert does not hold for all properties. Some properties and (all indexers) are not referenceable by name, yet
            //their binding brings them through here, perhaps needlessly.

            if (receiverOpt != null || memberSymbol is not MethodSymbol { MethodKind: MethodKind.Constructor })
            {
                if (IsTypeOrValueExpression(receiverOpt))
                {
                    // TypeOrValue expression isn't replaced only if the invocation is late bound, in which case it can't be extension method.
                    // None of the checks below apply if the receiver can't be classified as a type or value. 
                    Debug.Assert(!invokedAsExtensionMethod);
                }
                else if (!memberSymbol.RequiresInstanceReceiver())
                {
                    Debug.Assert(!invokedAsExtensionMethod || (receiverOpt != null));

                    if (invokedAsExtensionMethod)
                    {
                        if (IsMemberAccessedThroughType(receiverOpt))
                        {
                            if (receiverOpt.Kind == BoundKind.QueryClause)
                            {
                                RoslynDebug.Assert(receiverOpt.Type is object);
                                // Could not find an implementation of the query pattern for source type '{0}'.  '{1}' not found.
                                diagnostics.Add(ErrorCode.ERR_QueryNoProvider, node.Location, receiverOpt.Type, memberSymbol.Name);
                            }
                            else
                            {
                                // An object reference is required for the non-static field, method, or property '{0}'
                                diagnostics.Add(ErrorCode.ERR_ObjectRequired, node.Location, memberSymbol);
                            }
                            return true;
                        }
                    }
                    else if (!WasImplicitReceiver(receiverOpt) && IsMemberAccessedThroughVariableOrValue(receiverOpt))
                    {
                        if (this.Flags.Includes(BinderFlags.CollectionInitializerAddMethod))
                        {
                            diagnostics.Add(ErrorCode.ERR_InitializerAddHasWrongSignature, node.Location, memberSymbol);
                        }
                        else if (node.Kind() == SyntaxKind.AwaitExpression && memberSymbol.Name == WellKnownMemberNames.GetAwaiter)
                        {
                            RoslynDebug.Assert(receiverOpt.Type is object);
                            diagnostics.Add(ErrorCode.ERR_BadAwaitArg, node.Location, receiverOpt.Type);
                        }
                        else
                        {
                            diagnostics.Add(ErrorCode.ERR_ObjectProhibited, node.Location, memberSymbol);
                        }
                        return true;
                    }
                }
                else if (IsMemberAccessedThroughType(receiverOpt))
                {
                    diagnostics.Add(ErrorCode.ERR_ObjectRequired, node.Location, memberSymbol);
                    return true;
                }
                else if (WasImplicitReceiver(receiverOpt))
                {
                    if (InFieldInitializer && !ContainingType!.IsScriptClass || InConstructorInitializer || InAttributeArgument)
                    {
                        SyntaxNode errorNode = node;
                        if (node.Parent != null && node.Parent.Kind() == SyntaxKind.InvocationExpression)
                        {
                            errorNode = node.Parent;
                        }

                        ErrorCode code = InFieldInitializer ? ErrorCode.ERR_FieldInitRefNonstatic : ErrorCode.ERR_ObjectRequired;
                        diagnostics.Add(code, errorNode.Location, memberSymbol);
                        return true;
                    }

                    // If we could access the member through implicit "this" the receiver would be a BoundThisReference.
                    // If it is null it means that the instance member is inaccessible.
                    if (receiverOpt == null || ContainingMember().IsStatic)
                    {
                        Error(diagnostics, ErrorCode.ERR_ObjectRequired, node, memberSymbol);
                        return true;
                    }
                }
            }

            var containingType = this.ContainingType;
            if (containingType is object)
            {
                CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
                bool isAccessible = this.IsSymbolAccessibleConditional(memberSymbol.GetTypeOrReturnType().Type, containingType, ref useSiteInfo);
                diagnostics.Add(node, useSiteInfo);

                if (!isAccessible)
                {
                    // In the presence of non-transitive [InternalsVisibleTo] in source, or obnoxious symbols from metadata, it is possible
                    // to select a method through overload resolution in which the type is not accessible.  In this case a method cannot
                    // be called through normal IL, so we give an error.  Neither [InternalsVisibleTo] nor the need for this diagnostic is
                    // described by the language specification.
                    //
                    // Dev11 perform different access checks. See bug #530360 and tests AccessCheckTests.InaccessibleReturnType.
                    Error(diagnostics, ErrorCode.ERR_BadAccess, node, memberSymbol);
                    return true;
                }
            }

            return false;
        }

        private static bool IsMemberAccessedThroughVariableOrValue(BoundExpression? receiverOpt)
        {
            if (receiverOpt == null)
            {
                return false;
            }

            return !IsMemberAccessedThroughType(receiverOpt);
        }

        internal static bool IsMemberAccessedThroughType([NotNullWhen(true)] BoundExpression? receiverOpt)
        {
            if (receiverOpt == null)
            {
                return false;
            }

            while (receiverOpt.Kind == BoundKind.QueryClause)
            {
                receiverOpt = ((BoundQueryClause)receiverOpt).Value;
            }

            return receiverOpt.Kind == BoundKind.TypeExpression;
        }

        /// <summary>
        /// Was the receiver expression compiler-generated?
        /// </summary>
        internal static bool WasImplicitReceiver([NotNullWhen(false)] BoundExpression? receiverOpt)
        {
            if (receiverOpt == null) return true;
            if (!receiverOpt.WasCompilerGenerated) return false;
            switch (receiverOpt.Kind)
            {
                case BoundKind.ThisReference:
                case BoundKind.HostObjectMemberReference:
                case BoundKind.PreviousSubmissionReference:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// This method implements the checks in spec section 15.2.
        /// </summary>
        internal bool MethodIsCompatibleWithDelegateOrFunctionPointer(BoundExpression? receiverOpt, bool isExtensionMethod, MethodSymbol method, TypeSymbol delegateType, Location errorLocation, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(delegateType is NamedTypeSymbol { TypeKind: TypeKind.Delegate, DelegateInvokeMethod: { HasUseSiteError: false } }
                           || delegateType.TypeKind == TypeKind.FunctionPointer,
                         "This method should only be called for valid delegate or function pointer types.");

            MethodSymbol delegateOrFuncPtrMethod = delegateType switch
            {
                NamedTypeSymbol { DelegateInvokeMethod: { } invokeMethod } => invokeMethod,
                FunctionPointerTypeSymbol { Signature: { } signature } => signature,
                _ => throw ExceptionUtilities.UnexpectedValue(delegateType),
            };

            Debug.Assert(!isExtensionMethod || (receiverOpt != null));

            // - Argument types "match", and
            var delegateOrFuncPtrParameters = delegateOrFuncPtrMethod.Parameters;
            var methodParameters = method.Parameters;
            int numParams = delegateOrFuncPtrParameters.Length;

            Debug.Assert(methodParameters.Length == numParams + (isExtensionMethod ? 1 : 0));

            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);

            // If this is an extension method delegate, the caller should have verified the
            // receiver is compatible with the "this" parameter of the extension method.
            Debug.Assert(!(isExtensionMethod || (method.GetIsNewExtensionMember() && !method.IsStatic)) ||
                (Conversions.ConvertExtensionMethodThisArg(GetReceiverParameter(method)!.Type, receiverOpt!.Type, ref useSiteInfo, isMethodGroupConversion: true).Exists && useSiteInfo.Diagnostics.IsNullOrEmpty()));

            useSiteInfo = new CompoundUseSiteInfo<AssemblySymbol>(useSiteInfo);

            for (int i = 0; i < numParams; i++)
            {
                var delegateParameter = delegateOrFuncPtrParameters[i];
                var methodParameter = methodParameters[isExtensionMethod ? i + 1 : i];

                // The delegate compatibility checks are stricter than the checks on applicable functions: it's possible
                // to get here with a method that, while all the parameters are applicable, is not actually delegate
                // compatible. This is because the Applicable function member spec requires that:
                //  * Every value parameter (non-ref or similar) from the delegate type has an implicit conversion to the corresponding
                //    target parameter
                //  * Every ref or similar parameter has an identity conversion to the corresponding target parameter
                // However, the delegate compatibility requirements are stricter:
                //  * Every value parameter (non-ref or similar) from the delegate type has an implicit _reference_ conversion to the
                //    corresponding target parameter.
                //  * Every ref or similar parameter has an identity conversion to the corresponding target parameter
                // Note the addition of the reference requirement: it means that for delegate type void D(int i), void M(long l) is
                // _applicable_, but not _compatible_.
                if (!hasConversion(
                        delegateType.TypeKind,
                        Conversions,
                        source: delegateParameter.Type,
                        destination: methodParameter.Type,
                        sourceRefKind: delegateParameter.RefKind,
                        destinationRefKind: methodParameter.RefKind,
                        checkingReturns: false,
                        ref useSiteInfo))
                {
                    // No overload for '{0}' matches delegate '{1}'
                    Error(diagnostics, getMethodMismatchErrorCode(delegateType.TypeKind), errorLocation, method, delegateType);
                    diagnostics.Add(errorLocation, useSiteInfo);
                    return false;
                }
            }

            if (delegateOrFuncPtrMethod.RefKind != method.RefKind)
            {
                Error(diagnostics, getRefMismatchErrorCode(delegateType.TypeKind), errorLocation, method, delegateType);
                diagnostics.Add(errorLocation, useSiteInfo);
                return false;
            }

            var methodReturnType = method.ReturnType;
            var delegateReturnType = delegateOrFuncPtrMethod.ReturnType;
            bool returnsMatch = delegateOrFuncPtrMethod switch
            {
                { RefKind: RefKind.None, ReturnsVoid: true } => method.ReturnsVoid,
                { RefKind: var destinationRefKind } => hasConversion(
                    delegateType.TypeKind,
                    Conversions,
                    source: methodReturnType,
                    destination: delegateReturnType,
                    sourceRefKind: method.RefKind,
                    destinationRefKind: destinationRefKind,
                    checkingReturns: true,
                    ref useSiteInfo),
            };

            if (!returnsMatch)
            {
                Error(diagnostics, ErrorCode.ERR_BadRetType, errorLocation, method, method.ReturnType);
                diagnostics.Add(errorLocation, useSiteInfo);
                return false;
            }

            if (delegateType.IsFunctionPointer())
            {
                if (isExtensionMethod)
                {
                    Error(diagnostics, ErrorCode.ERR_CannotUseReducedExtensionMethodInAddressOf, errorLocation);
                    diagnostics.Add(errorLocation, useSiteInfo);
                    return false;
                }

                if (!method.IsStatic)
                {
                    // This check is here purely for completeness of implementing the spec. It should
                    // never be hit, as static methods should be eliminated as candidates in overload
                    // resolution and should never make it to this point.
                    Debug.Fail("This method should have been eliminated in overload resolution!");
                    Error(diagnostics, ErrorCode.ERR_FuncPtrMethMustBeStatic, errorLocation, method);
                    diagnostics.Add(errorLocation, useSiteInfo);
                    return false;
                }
            }

            diagnostics.Add(errorLocation, useSiteInfo);
            return true;

            static bool hasConversion(TypeKind targetKind, Conversions conversions, TypeSymbol source, TypeSymbol destination,
                RefKind sourceRefKind, RefKind destinationRefKind, bool checkingReturns, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
            {
                // Allowed ref kind mismatches between parameters have already been checked by overload resolution.
                if (checkingReturns
                    ? sourceRefKind != destinationRefKind
                    : (sourceRefKind == RefKind.None) != (destinationRefKind == RefKind.None))
                {
                    return false;
                }

                if (sourceRefKind != RefKind.None)
                {
                    return ConversionsBase.HasIdentityConversion(source, destination);
                }

                if (conversions.HasIdentityOrImplicitReferenceConversion(source, destination, ref useSiteInfo))
                {
                    return true;
                }

                return targetKind == TypeKind.FunctionPointer
                       && (ConversionsBase.HasImplicitPointerToVoidConversion(source, destination)
                           || conversions.HasImplicitPointerConversion(source, destination, ref useSiteInfo));
            }

            static ErrorCode getMethodMismatchErrorCode(TypeKind type)
                => type switch
                {
                    TypeKind.Delegate => ErrorCode.ERR_MethDelegateMismatch,
                    TypeKind.FunctionPointer => ErrorCode.ERR_MethFuncPtrMismatch,
                    _ => throw ExceptionUtilities.UnexpectedValue(type)
                };

            static ErrorCode getRefMismatchErrorCode(TypeKind type)
                => type switch
                {
                    TypeKind.Delegate => ErrorCode.ERR_DelegateRefMismatch,
                    TypeKind.FunctionPointer => ErrorCode.ERR_FuncPtrRefMismatch,
                    _ => throw ExceptionUtilities.UnexpectedValue(type)
                };
        }

        internal static ParameterSymbol? GetReceiverParameter(MethodSymbol method)
        {
            if (method.IsExtensionMethod)
            {
                return method.Parameters[0];
            }

            Debug.Assert(method.GetIsNewExtensionMember());
            return method.ContainingType.ExtensionParameter;
        }

        /// <summary>
        /// This method combines final validation (section 7.6.5.1) and delegate compatibility (section 15.2).
        /// </summary>
        /// <param name="syntax">CSharpSyntaxNode of the expression requiring method group conversion.</param>
        /// <param name="conversion">Conversion to be performed.</param>
        /// <param name="receiverOpt">Optional receiver.</param>
        /// <param name="isExtensionMethod">Method invoked as extension method.</param>
        /// <param name="delegateOrFuncPtrType">Target delegate type.</param>
        /// <param name="diagnostics">Where diagnostics should be added.</param>
        /// <returns>True if a diagnostic has been added.</returns>
        private bool MethodGroupConversionHasErrors(
            SyntaxNode syntax,
            Conversion conversion,
            BoundExpression? receiverOpt,
            bool isExtensionMethod,
            bool isAddressOf,
            TypeSymbol delegateOrFuncPtrType,
            BindingDiagnosticBag diagnostics)
        {
            var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
            Debug.Assert(Conversions.IsAssignableFromMulticastDelegate(delegateOrFuncPtrType, ref discardedUseSiteInfo) || delegateOrFuncPtrType.TypeKind == TypeKind.Delegate || delegateOrFuncPtrType.TypeKind == TypeKind.FunctionPointer);
            Debug.Assert(conversion.Method is object);
            MethodSymbol selectedMethod = conversion.Method;

            if (!Conversions.IsAssignableFromMulticastDelegate(delegateOrFuncPtrType, ref discardedUseSiteInfo))
            {
                if (!MethodIsCompatibleWithDelegateOrFunctionPointer(receiverOpt, isExtensionMethod, selectedMethod, delegateOrFuncPtrType, syntax.Location, diagnostics) ||
                    MemberGroupFinalValidation(receiverOpt, selectedMethod, syntax, diagnostics, isExtensionMethod))
                {
                    return true;
                }
            }

            if (selectedMethod.IsConditional)
            {
                // CS1618: Cannot create delegate with '{0}' because it has a Conditional attribute
                Error(diagnostics, ErrorCode.ERR_DelegateOnConditional, syntax.Location, selectedMethod);
                return true;
            }

            var sourceMethod = selectedMethod.OriginalDefinition as SourceOrdinaryMethodSymbol;
            if (sourceMethod is object && sourceMethod.IsPartialWithoutImplementation)
            {
                // CS0762: Cannot create delegate from method '{0}' because it is a partial method without an implementing declaration
                Error(diagnostics, ErrorCode.ERR_PartialMethodToDelegate, syntax.Location, selectedMethod);
                return true;
            }

            if ((selectedMethod.HasParameterContainingPointerType() || selectedMethod.ReturnType.ContainsPointerOrFunctionPointer())
                && ReportUnsafeIfNotAllowed(syntax, diagnostics))
            {
                return true;
            }

            CheckParameterModifierMismatchMethodConversion(syntax, selectedMethod, delegateOrFuncPtrType, isExtensionMethod, diagnostics);
            if (!isAddressOf)
            {
                ReportDiagnosticsIfUnmanagedCallersOnly(diagnostics, selectedMethod, syntax, isDelegateConversion: true);
            }
            ReportDiagnosticsIfObsolete(diagnostics, selectedMethod, syntax, hasBaseReceiver: false);

            // No use site errors, but there could be use site warnings.
            // If there are use site warnings, they were reported during the overload resolution process
            // that chose selectedMethod.
            Debug.Assert(!selectedMethod.HasUseSiteError, "Shouldn't have reached this point if there were use site errors.");

            return false;
        }

        /// <summary>
        /// This method is a wrapper around MethodGroupConversionHasErrors.  As a preliminary step,
        /// it checks whether a conversion exists.
        /// </summary>
        private bool MethodGroupConversionDoesNotExistOrHasErrors(
            BoundMethodGroup boundMethodGroup,
            NamedTypeSymbol delegateType,
            Location delegateMismatchLocation,
            BindingDiagnosticBag diagnostics,
            out Conversion conversion)
        {
            if (ReportDelegateInvokeUseSiteDiagnostic(diagnostics, delegateType, delegateMismatchLocation))
            {
                conversion = Conversion.NoConversion;
                return true;
            }

            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
            conversion = Conversions.GetMethodGroupDelegateConversion(boundMethodGroup, delegateType, ref useSiteInfo);
            diagnostics.Add(delegateMismatchLocation, useSiteInfo);
            if (!conversion.Exists)
            {
                if (!Conversions.ReportDelegateOrFunctionPointerMethodGroupDiagnostics(this, boundMethodGroup, delegateType, diagnostics))
                {
                    // No overload for '{0}' matches delegate '{1}'
                    diagnostics.Add(ErrorCode.ERR_MethDelegateMismatch, delegateMismatchLocation, boundMethodGroup.Name, delegateType);
                }

                return true;
            }
            else
            {
                Debug.Assert(conversion.IsValid); // i.e. if it exists, then it is valid.
                // Only cares about nullness and type of receiver, so no need to worry about BoundTypeOrValueExpression.
                return this.MethodGroupConversionHasErrors(boundMethodGroup.Syntax, conversion, boundMethodGroup.ReceiverOpt, conversion.IsExtensionMethod, isAddressOf: false, delegateType, diagnostics);
            }
        }

        public ConstantValue? FoldConstantConversion(
            SyntaxNode syntax,
            BoundExpression source,
            Conversion conversion,
            TypeSymbol destination,
            BindingDiagnosticBag diagnostics)
        {
            RoslynDebug.Assert(source != null);
            RoslynDebug.Assert((object)destination != null);

            // The diagnostics bag can be null in cases where we know ahead of time that the
            // conversion will succeed without error or warning. (For example, if we have a valid
            // implicit numeric conversion on a constant of numeric type.)

            // SPEC: A constant expression must be the null literal or a value with one of 
            // SPEC: the following types: sbyte, byte, short, ushort, int, uint, long, 
            // SPEC: ulong, char, float, double, decimal, bool, string, or any enumeration type.

            // SPEC: The following conversions are permitted in constant expressions:
            // SPEC: Identity conversions
            // SPEC: Numeric conversions
            // SPEC: Enumeration conversions
            // SPEC: Constant expression conversions
            // SPEC: Implicit and explicit reference conversions, provided that the source of the conversions 
            // SPEC: is a constant expression that evaluates to the null value.

            // SPEC VIOLATION: C# has always allowed the following, even though this does violate the rule that
            // SPEC VIOLATION: a constant expression must be either the null literal, or an expression of one 
            // SPEC VIOLATION: of the given types. 

            // SPEC VIOLATION: const C c = (C)null;

            // TODO: Some conversions can produce errors or warnings depending on checked/unchecked.
            // TODO: Fold conversions on enums and strings too.

            var sourceConstantValue = source.ConstantValueOpt;
            if (sourceConstantValue == null)
            {
                if (conversion.Kind == ConversionKind.DefaultLiteral)
                {
                    return destination.GetDefaultValue();
                }
                else
                {
                    return sourceConstantValue;
                }
            }
            else if (sourceConstantValue.IsBad)
            {
                return sourceConstantValue;
            }

            if (source.HasAnyErrors)
            {
                return null;
            }

            switch (conversion.Kind)
            {
                case ConversionKind.Identity:
                    // An identity conversion to a floating-point type (for example from a cast in
                    // source code) changes the internal representation of the constant value
                    // to precisely the required precision.
                    switch (destination.SpecialType)
                    {
                        case SpecialType.System_Single:
                            return ConstantValue.Create(sourceConstantValue.SingleValue);
                        case SpecialType.System_Double:
                            return ConstantValue.Create(sourceConstantValue.DoubleValue);
                        default:
                            return sourceConstantValue;
                    }

                case ConversionKind.NullLiteral:
                    return sourceConstantValue;

                case ConversionKind.ImplicitConstant:
                    return FoldConstantNumericConversion(syntax, sourceConstantValue, destination, diagnostics);

                case ConversionKind.ExplicitNumeric:
                case ConversionKind.ImplicitNumeric:
                case ConversionKind.ExplicitEnumeration:
                case ConversionKind.ImplicitEnumeration:
                    // The C# specification categorizes conversion from literal zero to nullable enum as 
                    // an Implicit Enumeration Conversion. Such a thing should not be constant folded
                    // because nullable enums are never constants.

                    if (destination.IsNullableType())
                    {
                        return null;
                    }

                    return FoldConstantNumericConversion(syntax, sourceConstantValue, destination, diagnostics);

                case ConversionKind.ExplicitReference:
                case ConversionKind.ImplicitReference:
                    return sourceConstantValue.IsNull ? sourceConstantValue : null;
            }

            return null;
        }

        private ConstantValue? FoldConstantNumericConversion(
            SyntaxNode syntax,
            ConstantValue sourceValue,
            TypeSymbol destination,
            BindingDiagnosticBag diagnostics)
        {
            RoslynDebug.Assert(sourceValue != null);
            Debug.Assert(!sourceValue.IsBad);

            SpecialType destinationType;
            if ((object)destination != null && destination.IsEnumType())
            {
                var underlyingType = ((NamedTypeSymbol)destination).EnumUnderlyingType;
                RoslynDebug.Assert((object)underlyingType != null);
                Debug.Assert(underlyingType.SpecialType != SpecialType.None);
                destinationType = underlyingType.SpecialType;
            }
            else
            {
                destinationType = destination.GetSpecialTypeSafe();
            }

            // In an unchecked context we ignore overflowing conversions on conversions from any
            // integral type, float and double to any integral type. "unchecked" actually does not
            // affect conversions from decimal to any integral type; if those are out of bounds then
            // we always give an error regardless.

            if (sourceValue.IsDecimal)
            {
                if (!CheckConstantBounds(destinationType, sourceValue, out _))
                {
                    // NOTE: Dev10 puts a suffix, "M", on the constant value.
                    Error(diagnostics, ErrorCode.ERR_ConstOutOfRange, syntax, sourceValue.Value + "M", destination!);
                    return ConstantValue.Bad;
                }
            }
            else if (destinationType == SpecialType.System_Decimal)
            {
                if (!CheckConstantBounds(destinationType, sourceValue, out _))
                {
                    Error(diagnostics, ErrorCode.ERR_ConstOutOfRange, syntax, sourceValue.Value!, destination!);
                    return ConstantValue.Bad;
                }
            }
            else if (CheckOverflowAtCompileTime)
            {
                if (!CheckConstantBounds(destinationType, sourceValue, out bool maySucceedAtRuntime))
                {
                    if (maySucceedAtRuntime)
                    {
                        // Can be calculated at runtime, but is not a compile-time constant.
                        Error(diagnostics, ErrorCode.WRN_ConstOutOfRangeChecked, syntax, sourceValue.Value!, destination!);
                        return null;
                    }
                    else
                    {
                        Error(diagnostics, ErrorCode.ERR_ConstOutOfRangeChecked, syntax, sourceValue.Value!, destination!);
                        return ConstantValue.Bad;
                    }
                }
            }
            else if (destinationType == SpecialType.System_IntPtr || destinationType == SpecialType.System_UIntPtr)
            {
                if (!CheckConstantBounds(destinationType, sourceValue, out _))
                {
                    // Can be calculated at runtime, but is not a compile-time constant.
                    return null;
                }
            }

            return ConstantValue.Create(DoUncheckedConversion(destinationType, sourceValue), destinationType);
        }

        private static object DoUncheckedConversion(SpecialType destinationType, ConstantValue value)
        {
            // Note that we keep "single" floats as doubles internally to maintain higher precision. However,
            // we do not do so in an entirely "lossless" manner. When *converting* to a float, we do lose 
            // the precision lost due to the conversion. But when doing arithmetic, we do the arithmetic on
            // the double values.
            //
            // An example will help. Suppose we have:
            //
            // const float cf1 = 1.0f;
            // const float cf2 = 1.0e-15f;
            // const double cd3 = cf1 - cf2;
            //
            // We first take the double-precision values for 1.0 and 1.0e-15 and round them to floats,
            // and then turn them back into doubles. Then when we do the subtraction, we do the subtraction
            // in doubles, not in floats. Had we done the subtraction in floats, we'd get 1.0; but instead we
            // do it in doubles and get 0.99999999999999.
            //
            // Similarly, if we have
            //
            // const int i4 = int.MaxValue; // 2147483647
            // const float cf5 = int.MaxValue; //  2147483648.0
            // const double cd6 = cf5; // 2147483648.0
            //
            // The int is converted to float and stored internally as the double 214783648, even though the
            // fully precise int would fit into a double.

            unchecked
            {
                switch (value.Discriminator)
                {
                    case ConstantValueTypeDiscriminator.Byte:
                        byte byteValue = value.ByteValue;
                        switch (destinationType)
                        {
                            case SpecialType.System_Byte: return (byte)byteValue;
                            case SpecialType.System_Char: return (char)byteValue;
                            case SpecialType.System_UInt16: return (ushort)byteValue;
                            case SpecialType.System_UInt32: return (uint)byteValue;
                            case SpecialType.System_UInt64: return (ulong)byteValue;
                            case SpecialType.System_SByte: return (sbyte)byteValue;
                            case SpecialType.System_Int16: return (short)byteValue;
                            case SpecialType.System_Int32: return (int)byteValue;
                            case SpecialType.System_Int64: return (long)byteValue;
                            case SpecialType.System_IntPtr: return (int)byteValue;
                            case SpecialType.System_UIntPtr: return (uint)byteValue;
                            case SpecialType.System_Single:
                            case SpecialType.System_Double: return (double)byteValue;
                            case SpecialType.System_Decimal: return (decimal)byteValue;
                            default: throw ExceptionUtilities.UnexpectedValue(destinationType);
                        }
                    case ConstantValueTypeDiscriminator.Char:
                        char charValue = value.CharValue;
                        switch (destinationType)
                        {
                            case SpecialType.System_Byte: return (byte)charValue;
                            case SpecialType.System_Char: return (char)charValue;
                            case SpecialType.System_UInt16: return (ushort)charValue;
                            case SpecialType.System_UInt32: return (uint)charValue;
                            case SpecialType.System_UInt64: return (ulong)charValue;
                            case SpecialType.System_SByte: return (sbyte)charValue;
                            case SpecialType.System_Int16: return (short)charValue;
                            case SpecialType.System_Int32: return (int)charValue;
                            case SpecialType.System_Int64: return (long)charValue;
                            case SpecialType.System_IntPtr: return (int)charValue;
                            case SpecialType.System_UIntPtr: return (uint)charValue;
                            case SpecialType.System_Single:
                            case SpecialType.System_Double: return (double)charValue;
                            case SpecialType.System_Decimal: return (decimal)charValue;
                            default: throw ExceptionUtilities.UnexpectedValue(destinationType);
                        }
                    case ConstantValueTypeDiscriminator.UInt16:
                        ushort uint16Value = value.UInt16Value;
                        switch (destinationType)
                        {
                            case SpecialType.System_Byte: return (byte)uint16Value;
                            case SpecialType.System_Char: return (char)uint16Value;
                            case SpecialType.System_UInt16: return (ushort)uint16Value;
                            case SpecialType.System_UInt32: return (uint)uint16Value;
                            case SpecialType.System_UInt64: return (ulong)uint16Value;
                            case SpecialType.System_SByte: return (sbyte)uint16Value;
                            case SpecialType.System_Int16: return (short)uint16Value;
                            case SpecialType.System_Int32: return (int)uint16Value;
                            case SpecialType.System_Int64: return (long)uint16Value;
                            case SpecialType.System_IntPtr: return (int)uint16Value;
                            case SpecialType.System_UIntPtr: return (uint)uint16Value;
                            case SpecialType.System_Single:
                            case SpecialType.System_Double: return (double)uint16Value;
                            case SpecialType.System_Decimal: return (decimal)uint16Value;
                            default: throw ExceptionUtilities.UnexpectedValue(destinationType);
                        }
                    case ConstantValueTypeDiscriminator.UInt32:
                        uint uint32Value = value.UInt32Value;
                        switch (destinationType)
                        {
                            case SpecialType.System_Byte: return (byte)uint32Value;
                            case SpecialType.System_Char: return (char)uint32Value;
                            case SpecialType.System_UInt16: return (ushort)uint32Value;
                            case SpecialType.System_UInt32: return (uint)uint32Value;
                            case SpecialType.System_UInt64: return (ulong)uint32Value;
                            case SpecialType.System_SByte: return (sbyte)uint32Value;
                            case SpecialType.System_Int16: return (short)uint32Value;
                            case SpecialType.System_Int32: return (int)uint32Value;
                            case SpecialType.System_Int64: return (long)uint32Value;
                            case SpecialType.System_IntPtr: return (int)uint32Value;
                            case SpecialType.System_UIntPtr: return (uint)uint32Value;
                            case SpecialType.System_Single: return (double)(float)uint32Value;
                            case SpecialType.System_Double: return (double)uint32Value;
                            case SpecialType.System_Decimal: return (decimal)uint32Value;
                            default: throw ExceptionUtilities.UnexpectedValue(destinationType);
                        }
                    case ConstantValueTypeDiscriminator.UInt64:
                        ulong uint64Value = value.UInt64Value;
                        switch (destinationType)
                        {
                            case SpecialType.System_Byte: return (byte)uint64Value;
                            case SpecialType.System_Char: return (char)uint64Value;
                            case SpecialType.System_UInt16: return (ushort)uint64Value;
                            case SpecialType.System_UInt32: return (uint)uint64Value;
                            case SpecialType.System_UInt64: return (ulong)uint64Value;
                            case SpecialType.System_SByte: return (sbyte)uint64Value;
                            case SpecialType.System_Int16: return (short)uint64Value;
                            case SpecialType.System_Int32: return (int)uint64Value;
                            case SpecialType.System_Int64: return (long)uint64Value;
                            case SpecialType.System_IntPtr: return (int)uint64Value;
                            case SpecialType.System_UIntPtr: return (uint)uint64Value;
                            case SpecialType.System_Single: return (double)(float)uint64Value;
                            case SpecialType.System_Double: return (double)uint64Value;
                            case SpecialType.System_Decimal: return (decimal)uint64Value;
                            default: throw ExceptionUtilities.UnexpectedValue(destinationType);
                        }
                    case ConstantValueTypeDiscriminator.NUInt:
                        uint nuintValue = value.UInt32Value;
                        switch (destinationType)
                        {
                            case SpecialType.System_Byte: return (byte)nuintValue;
                            case SpecialType.System_Char: return (char)nuintValue;
                            case SpecialType.System_UInt16: return (ushort)nuintValue;
                            case SpecialType.System_UInt32: return (uint)nuintValue;
                            case SpecialType.System_UInt64: return (ulong)nuintValue;
                            case SpecialType.System_SByte: return (sbyte)nuintValue;
                            case SpecialType.System_Int16: return (short)nuintValue;
                            case SpecialType.System_Int32: return (int)nuintValue;
                            case SpecialType.System_Int64: return (long)nuintValue;
                            case SpecialType.System_IntPtr: return (int)nuintValue;
                            case SpecialType.System_Single: return (double)(float)nuintValue;
                            case SpecialType.System_Double: return (double)nuintValue;
                            case SpecialType.System_Decimal: return (decimal)nuintValue;
                            default: throw ExceptionUtilities.UnexpectedValue(destinationType);
                        }
                    case ConstantValueTypeDiscriminator.SByte:
                        sbyte sbyteValue = value.SByteValue;
                        switch (destinationType)
                        {
                            case SpecialType.System_Byte: return (byte)sbyteValue;
                            case SpecialType.System_Char: return (char)sbyteValue;
                            case SpecialType.System_UInt16: return (ushort)sbyteValue;
                            case SpecialType.System_UInt32: return (uint)sbyteValue;
                            case SpecialType.System_UInt64: return (ulong)sbyteValue;
                            case SpecialType.System_SByte: return (sbyte)sbyteValue;
                            case SpecialType.System_Int16: return (short)sbyteValue;
                            case SpecialType.System_Int32: return (int)sbyteValue;
                            case SpecialType.System_Int64: return (long)sbyteValue;
                            case SpecialType.System_IntPtr: return (int)sbyteValue;
                            case SpecialType.System_UIntPtr: return (uint)sbyteValue;
                            case SpecialType.System_Single:
                            case SpecialType.System_Double: return (double)sbyteValue;
                            case SpecialType.System_Decimal: return (decimal)sbyteValue;
                            default: throw ExceptionUtilities.UnexpectedValue(destinationType);
                        }
                    case ConstantValueTypeDiscriminator.Int16:
                        short int16Value = value.Int16Value;
                        switch (destinationType)
                        {
                            case SpecialType.System_Byte: return (byte)int16Value;
                            case SpecialType.System_Char: return (char)int16Value;
                            case SpecialType.System_UInt16: return (ushort)int16Value;
                            case SpecialType.System_UInt32: return (uint)int16Value;
                            case SpecialType.System_UInt64: return (ulong)int16Value;
                            case SpecialType.System_SByte: return (sbyte)int16Value;
                            case SpecialType.System_Int16: return (short)int16Value;
                            case SpecialType.System_Int32: return (int)int16Value;
                            case SpecialType.System_Int64: return (long)int16Value;
                            case SpecialType.System_IntPtr: return (int)int16Value;
                            case SpecialType.System_UIntPtr: return (uint)int16Value;
                            case SpecialType.System_Single:
                            case SpecialType.System_Double: return (double)int16Value;
                            case SpecialType.System_Decimal: return (decimal)int16Value;
                            default: throw ExceptionUtilities.UnexpectedValue(destinationType);
                        }
                    case ConstantValueTypeDiscriminator.Int32:
                        int int32Value = value.Int32Value;
                        switch (destinationType)
                        {
                            case SpecialType.System_Byte: return (byte)int32Value;
                            case SpecialType.System_Char: return (char)int32Value;
                            case SpecialType.System_UInt16: return (ushort)int32Value;
                            case SpecialType.System_UInt32: return (uint)int32Value;
                            case SpecialType.System_UInt64: return (ulong)int32Value;
                            case SpecialType.System_SByte: return (sbyte)int32Value;
                            case SpecialType.System_Int16: return (short)int32Value;
                            case SpecialType.System_Int32: return (int)int32Value;
                            case SpecialType.System_Int64: return (long)int32Value;
                            case SpecialType.System_IntPtr: return (int)int32Value;
                            case SpecialType.System_UIntPtr: return (uint)int32Value;
                            case SpecialType.System_Single: return (double)(float)int32Value;
                            case SpecialType.System_Double: return (double)int32Value;
                            case SpecialType.System_Decimal: return (decimal)int32Value;
                            default: throw ExceptionUtilities.UnexpectedValue(destinationType);
                        }
                    case ConstantValueTypeDiscriminator.Int64:
                        long int64Value = value.Int64Value;
                        switch (destinationType)
                        {
                            case SpecialType.System_Byte: return (byte)int64Value;
                            case SpecialType.System_Char: return (char)int64Value;
                            case SpecialType.System_UInt16: return (ushort)int64Value;
                            case SpecialType.System_UInt32: return (uint)int64Value;
                            case SpecialType.System_UInt64: return (ulong)int64Value;
                            case SpecialType.System_SByte: return (sbyte)int64Value;
                            case SpecialType.System_Int16: return (short)int64Value;
                            case SpecialType.System_Int32: return (int)int64Value;
                            case SpecialType.System_Int64: return (long)int64Value;
                            case SpecialType.System_IntPtr: return (int)int64Value;
                            case SpecialType.System_UIntPtr: return (uint)int64Value;
                            case SpecialType.System_Single: return (double)(float)int64Value;
                            case SpecialType.System_Double: return (double)int64Value;
                            case SpecialType.System_Decimal: return (decimal)int64Value;
                            default: throw ExceptionUtilities.UnexpectedValue(destinationType);
                        }
                    case ConstantValueTypeDiscriminator.NInt:
                        int nintValue = value.Int32Value;
                        switch (destinationType)
                        {
                            case SpecialType.System_Byte: return (byte)nintValue;
                            case SpecialType.System_Char: return (char)nintValue;
                            case SpecialType.System_UInt16: return (ushort)nintValue;
                            case SpecialType.System_UInt32: return (uint)nintValue;
                            case SpecialType.System_UInt64: return (ulong)nintValue;
                            case SpecialType.System_SByte: return (sbyte)nintValue;
                            case SpecialType.System_Int16: return (short)nintValue;
                            case SpecialType.System_Int32: return (int)nintValue;
                            case SpecialType.System_Int64: return (long)nintValue;
                            case SpecialType.System_IntPtr: return (int)nintValue;
                            case SpecialType.System_UIntPtr: return (uint)nintValue;
                            case SpecialType.System_Single: return (double)(float)nintValue;
                            case SpecialType.System_Double: return (double)nintValue;
                            case SpecialType.System_Decimal: return (decimal)nintValue;
                            default: throw ExceptionUtilities.UnexpectedValue(destinationType);
                        }
                    case ConstantValueTypeDiscriminator.Single:
                    case ConstantValueTypeDiscriminator.Double:
                        // When converting from a floating-point type to an integral type, if the checked conversion would
                        // throw an overflow exception, then the unchecked conversion is undefined.  So that we have
                        // identical behavior on every host platform, we yield a result of zero in that case.
                        double doubleValue = CheckConstantBounds(destinationType, value.DoubleValue, out _) ? value.DoubleValue : 0D;
                        switch (destinationType)
                        {
                            case SpecialType.System_Byte: return (byte)doubleValue;
                            case SpecialType.System_Char: return (char)doubleValue;
                            case SpecialType.System_UInt16: return (ushort)doubleValue;
                            case SpecialType.System_UInt32: return (uint)doubleValue;
                            case SpecialType.System_UInt64: return (ulong)doubleValue;
                            case SpecialType.System_SByte: return (sbyte)doubleValue;
                            case SpecialType.System_Int16: return (short)doubleValue;
                            case SpecialType.System_Int32: return (int)doubleValue;
                            case SpecialType.System_Int64: return (long)doubleValue;
                            case SpecialType.System_IntPtr: return (int)doubleValue;
                            case SpecialType.System_UIntPtr: return (uint)doubleValue;
                            case SpecialType.System_Single: return (double)(float)doubleValue;
                            case SpecialType.System_Double: return (double)doubleValue;
                            case SpecialType.System_Decimal: return (value.Discriminator == ConstantValueTypeDiscriminator.Single) ? (decimal)(float)doubleValue : (decimal)doubleValue;
                            default: throw ExceptionUtilities.UnexpectedValue(destinationType);
                        }
                    case ConstantValueTypeDiscriminator.Decimal:
                        decimal decimalValue = CheckConstantBounds(destinationType, value.DecimalValue, out _) ? value.DecimalValue : 0m;
                        switch (destinationType)
                        {
                            case SpecialType.System_Byte: return (byte)decimalValue;
                            case SpecialType.System_Char: return (char)decimalValue;
                            case SpecialType.System_UInt16: return (ushort)decimalValue;
                            case SpecialType.System_UInt32: return (uint)decimalValue;
                            case SpecialType.System_UInt64: return (ulong)decimalValue;
                            case SpecialType.System_SByte: return (sbyte)decimalValue;
                            case SpecialType.System_Int16: return (short)decimalValue;
                            case SpecialType.System_Int32: return (int)decimalValue;
                            case SpecialType.System_Int64: return (long)decimalValue;
                            case SpecialType.System_IntPtr: return (int)decimalValue;
                            case SpecialType.System_UIntPtr: return (uint)decimalValue;
                            case SpecialType.System_Single: return (double)(float)decimalValue;
                            case SpecialType.System_Double: return (double)decimalValue;
                            case SpecialType.System_Decimal: return (decimal)decimalValue;
                            default: throw ExceptionUtilities.UnexpectedValue(destinationType);
                        }
                    default:
                        throw ExceptionUtilities.UnexpectedValue(value.Discriminator);
                }
            }

            // all cases should have been handled in the switch above.
            // return value.Value;
        }

        public static bool CheckConstantBounds(SpecialType destinationType, ConstantValue value, out bool maySucceedAtRuntime)
        {
            if (value.IsBad)
            {
                //assume that the constant was intended to be in bounds
                maySucceedAtRuntime = false;
                return true;
            }

            // Compute whether the value fits into the bounds of the given destination type without
            // error. We know that the constant will fit into either a double or a decimal, so
            // convert it to one of those and then check the bounds on that.
            var canonicalValue = CanonicalizeConstant(value);
            return canonicalValue is decimal ?
                CheckConstantBounds(destinationType, (decimal)canonicalValue, out maySucceedAtRuntime) :
                CheckConstantBounds(destinationType, (double)canonicalValue, out maySucceedAtRuntime);
        }

        private static bool CheckConstantBounds(SpecialType destinationType, double value, out bool maySucceedAtRuntime)
        {
            maySucceedAtRuntime = false;

            // Dev10 checks (minValue - 1) < value < (maxValue + 1).
            // See ExpressionBinder::isConstantInRange.
            switch (destinationType)
            {
                case SpecialType.System_Byte: return (byte.MinValue - 1D) < value && value < (byte.MaxValue + 1D);
                case SpecialType.System_Char: return (char.MinValue - 1D) < value && value < (char.MaxValue + 1D);
                case SpecialType.System_UInt16: return (ushort.MinValue - 1D) < value && value < (ushort.MaxValue + 1D);
                case SpecialType.System_UInt32: return (uint.MinValue - 1D) < value && value < (uint.MaxValue + 1D);
                case SpecialType.System_UInt64: return (ulong.MinValue - 1D) < value && value < (ulong.MaxValue + 1D);
                case SpecialType.System_SByte: return (sbyte.MinValue - 1D) < value && value < (sbyte.MaxValue + 1D);
                case SpecialType.System_Int16: return (short.MinValue - 1D) < value && value < (short.MaxValue + 1D);
                case SpecialType.System_Int32: return (int.MinValue - 1D) < value && value < (int.MaxValue + 1D);
                // Note: Using <= to compare the min value matches the native compiler.
                case SpecialType.System_Int64: return (long.MinValue - 1D) <= value && value < (long.MaxValue + 1D);
                case SpecialType.System_Decimal: return ((double)decimal.MinValue - 1D) < value && value < ((double)decimal.MaxValue + 1D);
                case SpecialType.System_IntPtr:
                    maySucceedAtRuntime = (long.MinValue - 1D) < value && value < (long.MaxValue + 1D);
                    return (int.MinValue - 1D) < value && value < (int.MaxValue + 1D);
                case SpecialType.System_UIntPtr:
                    maySucceedAtRuntime = (ulong.MinValue - 1D) < value && value < (ulong.MaxValue + 1D);
                    return (uint.MinValue - 1D) < value && value < (uint.MaxValue + 1D);
            }

            return true;
        }

        private static bool CheckConstantBounds(SpecialType destinationType, decimal value, out bool maySucceedAtRuntime)
        {
            maySucceedAtRuntime = false;

            // Dev10 checks (minValue - 1) < value < (maxValue + 1).
            // See ExpressionBinder::isConstantInRange.
            switch (destinationType)
            {
                case SpecialType.System_Byte: return (byte.MinValue - 1M) < value && value < (byte.MaxValue + 1M);
                case SpecialType.System_Char: return (char.MinValue - 1M) < value && value < (char.MaxValue + 1M);
                case SpecialType.System_UInt16: return (ushort.MinValue - 1M) < value && value < (ushort.MaxValue + 1M);
                case SpecialType.System_UInt32: return (uint.MinValue - 1M) < value && value < (uint.MaxValue + 1M);
                case SpecialType.System_UInt64: return (ulong.MinValue - 1M) < value && value < (ulong.MaxValue + 1M);
                case SpecialType.System_SByte: return (sbyte.MinValue - 1M) < value && value < (sbyte.MaxValue + 1M);
                case SpecialType.System_Int16: return (short.MinValue - 1M) < value && value < (short.MaxValue + 1M);
                case SpecialType.System_Int32: return (int.MinValue - 1M) < value && value < (int.MaxValue + 1M);
                case SpecialType.System_Int64: return (long.MinValue - 1M) < value && value < (long.MaxValue + 1M);
                case SpecialType.System_IntPtr:
                    maySucceedAtRuntime = (long.MinValue - 1M) < value && value < (long.MaxValue + 1M);
                    return (int.MinValue - 1M) < value && value < (int.MaxValue + 1M);
                case SpecialType.System_UIntPtr:
                    maySucceedAtRuntime = (ulong.MinValue - 1M) < value && value < (ulong.MaxValue + 1M);
                    return (uint.MinValue - 1M) < value && value < (uint.MaxValue + 1M);
            }

            return true;
        }

        // Takes in a constant of any kind and returns the constant as either a double or decimal
        private static object CanonicalizeConstant(ConstantValue value)
        {
            switch (value.Discriminator)
            {
                case ConstantValueTypeDiscriminator.SByte: return (decimal)value.SByteValue;
                case ConstantValueTypeDiscriminator.Int16: return (decimal)value.Int16Value;
                case ConstantValueTypeDiscriminator.Int32: return (decimal)value.Int32Value;
                case ConstantValueTypeDiscriminator.Int64: return (decimal)value.Int64Value;
                case ConstantValueTypeDiscriminator.NInt: return (decimal)value.Int32Value;
                case ConstantValueTypeDiscriminator.Byte: return (decimal)value.ByteValue;
                case ConstantValueTypeDiscriminator.Char: return (decimal)value.CharValue;
                case ConstantValueTypeDiscriminator.UInt16: return (decimal)value.UInt16Value;
                case ConstantValueTypeDiscriminator.UInt32: return (decimal)value.UInt32Value;
                case ConstantValueTypeDiscriminator.UInt64: return (decimal)value.UInt64Value;
                case ConstantValueTypeDiscriminator.NUInt: return (decimal)value.UInt32Value;
                case ConstantValueTypeDiscriminator.Single:
                case ConstantValueTypeDiscriminator.Double: return value.DoubleValue;
                case ConstantValueTypeDiscriminator.Decimal: return value.DecimalValue;
                default: throw ExceptionUtilities.UnexpectedValue(value.Discriminator);
            }

            // all cases handled in the switch, above.
        }
    }
}
