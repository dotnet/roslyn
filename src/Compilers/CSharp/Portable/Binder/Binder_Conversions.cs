// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
                reportUseSiteDiagnostics(conversion);

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
                    var unconvertedSource = (BoundUnconvertedInterpolatedString)source;
                    source = new BoundInterpolatedString(
                        unconvertedSource.Syntax,
                        interpolationData: null,
                        BindInterpolatedStringParts(unconvertedSource, diagnostics),
                        unconvertedSource.ConstantValueOpt,
                        unconvertedSource.Type,
                        unconvertedSource.HasErrors);
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

                reportUseSiteDiagnosticsForUnderlyingConversions(conversion);

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

                void reportUseSiteDiagnostics(Conversion conversion)
                {
                    // Obsolete diagnostics for method group are reported as part of creating the method group conversion.
                    Debug.Assert(!conversion.IsMethodGroup);
                    ReportDiagnosticsIfObsolete(diagnostics, conversion, syntax, hasBaseReceiver: false);
                    if (conversion.Method is not null)
                    {
                        ReportUseSite(conversion.Method, diagnostics, syntax.Location);
                    }
                    CheckConstraintLanguageVersionAndRuntimeSupportForConversion(syntax, conversion, diagnostics);
                }

                void reportUseSiteDiagnosticsForUnderlyingConversions(Conversion conversion)
                {
                    var underlyingConversions = conversion.UnderlyingConversions;

                    if (!underlyingConversions.IsDefaultOrEmpty)
                    {
                        foreach (var underlying in underlyingConversions)
                        {
                            reportUseSiteDiagnosticsForSelfAndUnderlyingConversions(underlying);

                            if (underlying.IsUserDefined)
                            {
                                reportUseSiteDiagnosticsForSelfAndUnderlyingConversions(underlying.UserDefinedFromConversion);
                                reportUseSiteDiagnosticsForSelfAndUnderlyingConversions(underlying.UserDefinedToConversion);
                                underlying.MarkUnderlyingConversionsChecked();
                            }
                        }

                        conversion.MarkUnderlyingConversionsChecked();
                    }

                    void reportUseSiteDiagnosticsForSelfAndUnderlyingConversions(Conversion conversion)
                    {
                        reportUseSiteDiagnostics(conversion);
                        reportUseSiteDiagnosticsForUnderlyingConversions(conversion);
                    }
                }
            }
        }

        internal void CheckConstraintLanguageVersionAndRuntimeSupportForConversion(SyntaxNodeOrToken syntax, Conversion conversion, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(syntax.SyntaxTree is object);

            if (conversion.IsUserDefined && conversion.Method is MethodSymbol method && method.IsStatic)
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
            ImmutableArray<Conversion> underlyingConversions = conversionIfTargetTyped.GetValueOrDefault().UnderlyingConversions;
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
                    source,
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

            CheckValidScopedMethodConversion(syntax, boundLambda.Symbol, destination, invokedAsExtensionMethod: false, diagnostics);
            CheckLambdaConversion(boundLambda.Symbol, destination, diagnostics);
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

            return new BoundConversion(syntax, group, conversion, @checked: false, explicitCastInCode: isCast, conversionGroup, constantValueOpt: ConstantValue.NotAvailable, type: destination, hasErrors: hasErrors) { WasCompilerGenerated = group.WasCompilerGenerated };
        }

        private static void CheckValidScopedMethodConversion(SyntaxNode syntax, MethodSymbol lambdaOrMethod, TypeSymbol targetType, bool invokedAsExtensionMethod, BindingDiagnosticBag diagnostics)
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
                    static (diagnostics, delegateMethod, lambdaOrMethod, parameter, _, typeAndLocation) =>
                    {
                        diagnostics.Add(
                            SourceMemberContainerTypeSymbol.ReportInvalidScopedOverrideAsError(delegateMethod, lambdaOrMethod) ?
                                ErrorCode.ERR_ScopedMismatchInParameterOfTarget :
                                ErrorCode.WRN_ScopedMismatchInParameterOfTarget,
                            typeAndLocation.Location,
                            new FormattedSymbol(parameter, SymbolDisplayFormat.ShortFormat),
                            typeAndLocation.Type);
                    },
                    (Type: targetType, Location: syntax.Location),
                    allowVariance: true,
                    invokedAsExtensionMethod: invokedAsExtensionMethod);
            }
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

                    if (lambdaParameter.IsParams && !delegateParameter.IsParams && p == lambdaSymbol.ParameterCount - 1 && lambdaParameter.Type.IsSZArray())
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

            Debug.Assert(memberSymbol.Kind != SymbolKind.Method ||
                memberSymbol.CanBeReferencedByName);
            //note that the same assert does not hold for all properties. Some properties and (all indexers) are not referenceable by name, yet
            //their binding brings them through here, perhaps needlessly.

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

            if (methodParameters.Length != numParams + (isExtensionMethod ? 1 : 0))
            {
                // This can happen if "method" has optional parameters.
                Debug.Assert(methodParameters.Length > numParams + (isExtensionMethod ? 1 : 0));
                Error(diagnostics, getMethodMismatchErrorCode(delegateType.TypeKind), errorLocation, method, delegateType);
                return false;
            }

            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);

            // If this is an extension method delegate, the caller should have verified the
            // receiver is compatible with the "this" parameter of the extension method.
            Debug.Assert(!isExtensionMethod ||
                (Conversions.ConvertExtensionMethodThisArg(methodParameters[0].Type, receiverOpt!.Type, ref useSiteInfo).Exists && useSiteInfo.Diagnostics.IsNullOrEmpty()));

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
                if (!hasConversion(delegateType.TypeKind, Conversions, delegateParameter.Type, methodParameter.Type, delegateParameter.RefKind, methodParameter.RefKind, ref useSiteInfo))
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
                { RefKind: var destinationRefKind } => hasConversion(delegateType.TypeKind, Conversions, methodReturnType, delegateReturnType, method.RefKind, destinationRefKind, ref useSiteInfo),
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
                RefKind sourceRefKind, RefKind destinationRefKind, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
            {
                if (sourceRefKind != destinationRefKind)
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

            var sourceMethod = selectedMethod as SourceOrdinaryMethodSymbol;
            if (sourceMethod is object && sourceMethod.IsPartialWithoutImplementation)
            {
                // CS0762: Cannot create delegate from method '{0}' because it is a partial method without an implementing declaration
                Error(diagnostics, ErrorCode.ERR_PartialMethodToDelegate, syntax.Location, selectedMethod);
                return true;
            }

            if ((selectedMethod.HasParameterContainingPointerType() || selectedMethod.ReturnType.ContainsPointer())
                && ReportUnsafeIfNotAllowed(syntax, diagnostics))
            {
                return true;
            }

            CheckValidScopedMethodConversion(syntax, selectedMethod, delegateOrFuncPtrType, isExtensionMethod, diagnostics);
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
