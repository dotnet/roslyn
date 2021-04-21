// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class Conversions : ConversionsBase
    {
        private readonly Binder _binder;

        public Conversions(Binder binder)
            : this(binder, currentRecursionDepth: 0, includeNullability: false, otherNullabilityOpt: null)
        {
        }

        private Conversions(Binder binder, int currentRecursionDepth, bool includeNullability, Conversions otherNullabilityOpt)
            : base(binder.Compilation.Assembly.CorLibrary, currentRecursionDepth, includeNullability, otherNullabilityOpt)
        {
            _binder = binder;
        }

        protected override ConversionsBase CreateInstance(int currentRecursionDepth)
        {
            return new Conversions(_binder, currentRecursionDepth, IncludeNullability, otherNullabilityOpt: null);
        }

        private CSharpCompilation Compilation { get { return _binder.Compilation; } }

        protected override ConversionsBase WithNullabilityCore(bool includeNullability)
        {
            Debug.Assert(IncludeNullability != includeNullability);
            return new Conversions(_binder, currentRecursionDepth, includeNullability, this);
        }

        public override Conversion GetMethodGroupDelegateConversion(BoundMethodGroup source, TypeSymbol destination, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            // Must be a bona fide delegate type, not an expression tree type.
            if (!destination.IsDelegateType())
            {
                return Conversion.NoConversion;
            }

            var (methodSymbol, isFunctionPointer, callingConventionInfo) = GetDelegateInvokeOrFunctionPointerMethodIfAvailable(destination);
            if ((object)methodSymbol == null)
            {
                return Conversion.NoConversion;
            }

            var resolution = ResolveDelegateOrFunctionPointerMethodGroup(_binder, source, methodSymbol, isFunctionPointer, callingConventionInfo, ref useSiteInfo);
            var conversion = (resolution.IsEmpty || resolution.HasAnyErrors) ?
                Conversion.NoConversion :
                ToConversion(resolution.OverloadResolutionResult, resolution.MethodGroup, ((NamedTypeSymbol)destination).DelegateInvokeMethod.ParameterCount);
            resolution.Free();
            return conversion;
        }

        public override Conversion GetMethodGroupFunctionPointerConversion(BoundMethodGroup source, FunctionPointerTypeSymbol destination, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            var resolution = ResolveDelegateOrFunctionPointerMethodGroup(
                _binder,
                source,
                destination.Signature,
                isFunctionPointer: true,
                new CallingConventionInfo(destination.Signature.CallingConvention, destination.Signature.GetCallingConventionModifiers()),
                ref useSiteInfo);
            var conversion = (resolution.IsEmpty || resolution.HasAnyErrors) ?
                Conversion.NoConversion :
                ToConversion(resolution.OverloadResolutionResult, resolution.MethodGroup, destination.Signature.ParameterCount);
            resolution.Free();
            return conversion;
        }

        protected override Conversion GetInterpolatedStringConversion(BoundUnconvertedInterpolatedString source, TypeSymbol destination, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            // An interpolated string expression may be converted to the types
            // System.IFormattable and System.FormattableString
            return (TypeSymbol.Equals(destination, Compilation.GetWellKnownType(WellKnownType.System_IFormattable), TypeCompareKind.ConsiderEverything2) ||
                    TypeSymbol.Equals(destination, Compilation.GetWellKnownType(WellKnownType.System_FormattableString), TypeCompareKind.ConsiderEverything2))
                ? Conversion.InterpolatedString : Conversion.NoConversion;
        }

#nullable  enable
        public override bool IsApplicableInterpolatedStringBuilderType(BoundUnconvertedInterpolatedString source, TypeSymbol builderType, BindingDiagnosticBag diagnostics, out ImmutableArray<MethodArgumentInfo> builderArguments)
        {
            // SPEC:
            // A type is said to be an _applicable_interpolated_string_builder_type_ if, given an _interpolated_string_literal_ `S`, the following is true:
            //  * Overload resolution with an identifier of `TryFormatBaseString` and a parameter type of `string` succeeds, and contains a single instance method that returns a `bool` or `void`.
            //  * For every _regular_balanced_text_ component of `S` (`Si`) without an _interpolation_format_ component or _constant_expression_ (alignment) component, overload resolution
            //    with an identifier of `TryFormatInterpolationHole` and parameter of the type of `Si` and succeeds, and contains a single instance method that returns a `bool` or `void`.
            //  * For every _regular_balanced_text_ component of `S` (`Si`) with an _interpolation_format_ component and no _constant_expression_ (alignment) component, overload resolution
            //    with an identifier of `TryFormatInterpolationHole` and parameter types of `Si` and `string` with name `format` (in that order) succeeds, and contains a single instance
            //    method that returns a `bool` or `void`.
            //  * For every _regular_balanced_text_ component of `S` (`Si`) with a _constant_expression_ (alignment) component and no _interpolation_format_ component, overload resolution
            //    with an identifier of `TryFormatInterpolationHole and parameter types of `Si` and `int` with name `alignment` (in that order) succeeds, and contains a single instance
            //    method that returns a `bool` or `void`.
            //  * For every _regular_balanced_text_ component of `S` (`Si`) with an _interpolation_format_ component and a _constant_expression_ (alignment) component, overload resolution
            //    with an identifier of `TryFormatInterpolationHole` and parameter types of `Si`, `int` with name `alignment`, and `string` with name `format` (in that order) succeeds, and
            //    contains a single instance method that returns a `bool` or `void`.
            // Additionally, all resolved method calls must return the same type. `TryFormat` calls that mix `bool` and `void` are not permitted.

            // If the type is applicable, we'll add these to the given info at the end.
            var useSiteInfo = _binder.GetNewCompoundUseSiteInfo(diagnostics);

            // All builder types have to at least have a `TryFormatBaseString` that takes a single string argument and returns either bool or void.
            if (!tryGetCandidateMethods("TryFormatBaseString", ref useSiteInfo, out ArrayBuilder<MethodSymbol>? baseStringCandidates))
            {
                // PROTOTYPE(interp-strings): We'll want to have a specific error for when we're converting to a non-string type
                builderArguments = default;
                diagnostics.AddDiagnostics(source.Syntax, useSiteInfo);
                return false;
            }

            var typeArguments = ArrayBuilder<TypeWithAnnotations>.GetInstance();
            var implicitBuilderReceiver = new BoundImplicitReceiver(source.Syntax, builderType);
            var analyzedArguments = AnalyzedArguments.GetInstance();
            var overloadResolutionResult = OverloadResolutionResult<MethodSymbol>.GetInstance();
            var stringType = _binder.GetSpecialType(SpecialType.System_String, diagnostics, source.Syntax);
            analyzedArguments.Arguments.Add(new BoundLiteral(CSharpSyntaxTree.Dummy.GetRoot(), constantValueOpt: null, stringType));
            _binder.OverloadResolution.MethodInvocationOverloadResolution(
                baseStringCandidates,
                typeArguments,
                implicitBuilderReceiver,
                analyzedArguments,
                overloadResolutionResult,
                ref useSiteInfo);

            if (!overloadResolutionResult.Succeeded || overloadResolutionResult.ValidResult.Member.CallsAreOmitted(source.SyntaxTree))
            {
                // PROTOTYPE(interp-strings): We'll want to have a specific error for when we're converting to a non-string type
                free();
                builderArguments = default;
                return false;
            }

            Debug.Assert(!overloadResolutionResult.ValidResult.Member.IsStatic);

            // All TryFormat... calls must have the same return type, and it must be either bool or void.
            bool usesBoolReturn;
            switch (overloadResolutionResult.ValidResult.Member.ReturnType.SpecialType)
            {
                case SpecialType.System_Void:
                    usesBoolReturn = false;
                    break;
                case SpecialType.System_Boolean:
                    usesBoolReturn = true;
                    break;
                default:
                    free();
                    builderArguments = default;
                    return false;
            }

            // We at least have an invocable, applicable TryFormatBaseString method on the type. We can proceed with finding the correct overloads for
            // each component.

            if (source.Parts.Length == 0)
            {
                free();
                diagnostics.AddDiagnostics(source.Syntax, useSiteInfo);
                builderArguments = ImmutableArray<MethodArgumentInfo>.Empty;
                return true;
            }

            ArrayBuilder<MethodSymbol>? interpolationHoleCandidates = null;
            var builderFormatCalls = ArrayBuilder<MethodArgumentInfo>.GetInstance(source.Parts.Length);

            foreach (var part in source.Parts)
            {
                Debug.Assert(typeArguments.IsEmpty());
                Debug.Assert(part is BoundLiteral or BoundStringInsert);
                analyzedArguments.Clear();
                overloadResolutionResult.Clear();

                ArrayBuilder<MethodSymbol> candidateMethods;
                if (part is BoundStringInsert insert)
                {
                    if (interpolationHoleCandidates is null && !tryGetCandidateMethods("TryFormatInterpolationHole", ref useSiteInfo, out interpolationHoleCandidates))
                    {
                        // PROTOTYPE(interp-string): We'll likely want to continue attempting to bind for errors when we're not directly being converted to a string
                        free();
                        builderFormatCalls.Free();
                        builderArguments = default;
                        return false;
                    }

                    candidateMethods = interpolationHoleCandidates;
                    analyzedArguments.Arguments.Add(insert.Value);
                    analyzedArguments.Names.Add(null);

                    if (insert.Alignment is not null)
                    {
                        analyzedArguments.Arguments.Add(insert.Alignment);
                        analyzedArguments.Names.Add(("alignment", insert.Alignment.Syntax.Location));
                    }
                    if (insert.Format is not null)
                    {
                        analyzedArguments.Arguments.Add(insert.Format);
                        analyzedArguments.Names.Add(("format", insert.Format.Syntax.Location));
                    }
                }
                else
                {
                    candidateMethods = baseStringCandidates;
                    analyzedArguments.Arguments.Add(part);
                }

                _binder.OverloadResolution.MethodInvocationOverloadResolution(
                    candidateMethods,
                    typeArguments,
                    implicitBuilderReceiver,
                    analyzedArguments,
                    overloadResolutionResult,
                    ref useSiteInfo);

                if (!overloadResolutionResult.Succeeded
                    || overloadResolutionResult.ValidResult.Member.CallsAreOmitted(source.SyntaxTree)
                    || !returnTypeMatches(overloadResolutionResult.ValidResult.Member, usesBoolReturn))
                {
                    // PROTOTYPE(interp-string): We'll likely want to continue attempting to bind for errors when we're not directly being converted to a string
                    free();
                    builderFormatCalls.Free();
                    interpolationHoleCandidates?.Free();
                    builderArguments = default;
                    return false;
                }

                var argsToParam = overloadResolutionResult.ValidResult.Result.ArgsToParamsOpt;

                _binder.CoerceArguments(overloadResolutionResult.ValidResult, analyzedArguments.Arguments, diagnostics);

                bool expanded = overloadResolutionResult.ValidResult.Result.Kind == MemberResolutionKind.ApplicableInExpandedForm;
                _binder.BindDefaultArguments(
                    source.Syntax,
                    overloadResolutionResult.ValidResult.Member.Parameters,
                    analyzedArguments.Arguments,
                    analyzedArguments.RefKinds,
                    ref argsToParam,
                    out BitVector defaultArguments,
                    expanded,
                    enableCallerInfo: true,
                    diagnostics);

                builderFormatCalls.Add(new MethodArgumentInfo(overloadResolutionResult.ValidResult.Member, analyzedArguments.Arguments.ToImmutable(), argsToParam, defaultArguments, expanded));
            }

            free();
            diagnostics.AddDiagnostics(source.Syntax, useSiteInfo);
            builderArguments = builderFormatCalls.ToImmutableAndFree();
            interpolationHoleCandidates?.Free();
            return true;

            bool tryGetCandidateMethods(string methodName, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo, [NotNullWhen(true)] out ArrayBuilder<MethodSymbol>? candidateMethods)
            {
                var lookupResult = LookupResult.GetInstance();
                _binder.LookupSymbolsSimpleName(lookupResult,
                    builderType,
                    methodName,
                    arity: 0,
                    basesBeingResolved: null,
                    options: LookupOptions.AllMethodsOnArityZero | LookupOptions.MustBeInstance | LookupOptions.MustBeInvocableIfMember,
                    diagnose: true,
                    ref useSiteInfo);

                if (!lookupResult.IsMultiViable)
                {
                    candidateMethods = null;
                    lookupResult.Free();
                    return false;
                }

                Debug.Assert(lookupResult.Symbols.Count > 0);
                candidateMethods = ArrayBuilder<MethodSymbol>.GetInstance(lookupResult.Symbols.Count);

                foreach (var symbol in lookupResult.Symbols)
                {
                    if (symbol is not MethodSymbol method)
                    {
                        // PROTOTYPE(interp-strings): We'll want to have a specific error for when we're converting to a non-string type
                        candidateMethods.Free();
                        lookupResult.Free();
                        candidateMethods = null;
                        return false;
                    }

                    candidateMethods.Add(method);
                }

                lookupResult.Free();
                return true;
            }

            void free()
            {
                typeArguments.Free();
                analyzedArguments.Free();
                overloadResolutionResult.Free();
                baseStringCandidates.Free();
            }

            static bool returnTypeMatches(MethodSymbol symbol, bool expectedBoolReturn)
                => symbol.ReturnType.SpecialType switch
                {
                    SpecialType.System_Boolean => expectedBoolReturn,
                    SpecialType.System_Void => !expectedBoolReturn,
                    _ => false
                };
        }
#nullable disable

        /// <summary>
        /// Resolve method group based on the optional delegate invoke method.
        /// If the invoke method is null, ignore arguments in resolution.
        /// </summary>
        private static MethodGroupResolution ResolveDelegateOrFunctionPointerMethodGroup(Binder binder, BoundMethodGroup source, MethodSymbol delegateInvokeMethodOpt, bool isFunctionPointer, in CallingConventionInfo callingConventionInfo, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            if ((object)delegateInvokeMethodOpt != null)
            {
                var analyzedArguments = AnalyzedArguments.GetInstance();
                GetDelegateOrFunctionPointerArguments(source.Syntax, analyzedArguments, delegateInvokeMethodOpt.Parameters, binder.Compilation);
                var resolution = binder.ResolveMethodGroup(source, analyzedArguments, useSiteInfo: ref useSiteInfo, inferWithDynamic: true,
                    isMethodGroupConversion: true, returnRefKind: delegateInvokeMethodOpt.RefKind, returnType: delegateInvokeMethodOpt.ReturnType,
                    isFunctionPointerResolution: isFunctionPointer, callingConventionInfo: callingConventionInfo);
                analyzedArguments.Free();
                return resolution;
            }
            else
            {
                return binder.ResolveMethodGroup(source, analyzedArguments: null, isMethodGroupConversion: true, ref useSiteInfo);
            }
        }

        /// <summary>
        /// Return the Invoke method symbol if the type is a delegate
        /// type and the Invoke method is available, otherwise null.
        /// </summary>
        private static (MethodSymbol, bool isFunctionPointer, CallingConventionInfo callingConventionInfo) GetDelegateInvokeOrFunctionPointerMethodIfAvailable(TypeSymbol type)
        {
            if (type is FunctionPointerTypeSymbol { Signature: { } signature })
            {
                return (signature, true, new CallingConventionInfo(signature.CallingConvention, signature.GetCallingConventionModifiers()));
            }

            var delegateType = type.GetDelegateType();
            if ((object)delegateType == null)
            {
                return (null, false, default);
            }

            MethodSymbol methodSymbol = delegateType.DelegateInvokeMethod;
            if ((object)methodSymbol == null || methodSymbol.HasUseSiteError)
            {
                return (null, false, default);
            }

            return (methodSymbol, false, default);
        }

        public static bool ReportDelegateOrFunctionPointerMethodGroupDiagnostics(Binder binder, BoundMethodGroup expr, TypeSymbol targetType, BindingDiagnosticBag diagnostics)
        {
            var (invokeMethodOpt, isFunctionPointer, callingConventionInfo) = GetDelegateInvokeOrFunctionPointerMethodIfAvailable(targetType);
            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = binder.GetNewCompoundUseSiteInfo(diagnostics);
            var resolution = ResolveDelegateOrFunctionPointerMethodGroup(binder, expr, invokeMethodOpt, isFunctionPointer, callingConventionInfo, ref useSiteInfo);
            diagnostics.Add(expr.Syntax, useSiteInfo);

            bool hasErrors = resolution.HasAnyErrors;

            diagnostics.AddRange(resolution.Diagnostics);

            // SPEC VIOLATION: Unfortunately, we cannot exactly implement the specification for
            // the scenario in which an extension method that extends a value type is converted
            // to a delegate. The code we generate that captures a delegate to a static method
            // that is "partially evaluated" with the bound-to-the-delegate first argument
            // requires that the first argument be of reference type.
            //
            // SPEC VIOLATION: Similarly, we cannot capture a method of Nullable<T>, because
            // boxing a Nullable<T> gives a T, not a boxed Nullable<T>.
            //
            // We give special error messages in these situations.

            if (resolution.MethodGroup != null)
            {
                var result = resolution.OverloadResolutionResult;
                if (result != null)
                {
                    if (result.Succeeded)
                    {
                        var method = result.BestResult.Member;
                        Debug.Assert((object)method != null);
                        if (resolution.MethodGroup.IsExtensionMethodGroup)
                        {
                            Debug.Assert(method.IsExtensionMethod);

                            var thisParameter = method.Parameters[0];
                            if (!thisParameter.Type.IsReferenceType)
                            {
                                // Extension method '{0}' defined on value type '{1}' cannot be used to create delegates
                                diagnostics.Add(
                                    ErrorCode.ERR_ValueTypeExtDelegate,
                                    expr.Syntax.Location,
                                    method,
                                    thisParameter.Type);
                                hasErrors = true;
                            }
                        }
                        else if (method.ContainingType.IsNullableType() && !method.IsOverride)
                        {
                            // CS1728: Cannot bind delegate to '{0}' because it is a member of 'System.Nullable<T>'
                            diagnostics.Add(
                                ErrorCode.ERR_DelegateOnNullable,
                                expr.Syntax.Location,
                                method);
                            hasErrors = true;
                        }
                    }
                    else if (!hasErrors &&
                            !resolution.IsEmpty &&
                            resolution.ResultKind == LookupResultKind.Viable)
                    {
                        var overloadDiagnostics = BindingDiagnosticBag.GetInstance(withDiagnostics: true, diagnostics.AccumulatesDependencies);

                        result.ReportDiagnostics(
                            binder: binder, location: expr.Syntax.Location, nodeOpt: expr.Syntax, diagnostics: overloadDiagnostics,
                            name: expr.Name,
                            receiver: resolution.MethodGroup.Receiver, invokedExpression: expr.Syntax, arguments: resolution.AnalyzedArguments,
                            memberGroup: resolution.MethodGroup.Methods.ToImmutable(),
                            typeContainingConstructor: null, delegateTypeBeingInvoked: null,
                            isMethodGroupConversion: true, returnRefKind: invokeMethodOpt?.RefKind, delegateOrFunctionPointerType: targetType);

                        hasErrors = overloadDiagnostics.HasAnyErrors();
                        diagnostics.AddRangeAndFree(overloadDiagnostics);
                    }
                }
            }

            resolution.Free();
            return hasErrors;
        }

        public Conversion MethodGroupConversion(SyntaxNode syntax, MethodGroup methodGroup, NamedTypeSymbol delegateType, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            var analyzedArguments = AnalyzedArguments.GetInstance();
            var result = OverloadResolutionResult<MethodSymbol>.GetInstance();
            var delegateInvokeMethod = delegateType.DelegateInvokeMethod;

            Debug.Assert((object)delegateInvokeMethod != null && !delegateInvokeMethod.HasUseSiteError,
                         "This method should only be called for valid delegate types");
            GetDelegateOrFunctionPointerArguments(syntax, analyzedArguments, delegateInvokeMethod.Parameters, Compilation);
            _binder.OverloadResolution.MethodInvocationOverloadResolution(
                methods: methodGroup.Methods,
                typeArguments: methodGroup.TypeArguments,
                receiver: methodGroup.Receiver,
                arguments: analyzedArguments,
                result: result,
                useSiteInfo: ref useSiteInfo,
                isMethodGroupConversion: true,
                returnRefKind: delegateInvokeMethod.RefKind,
                returnType: delegateInvokeMethod.ReturnType);
            var conversion = ToConversion(result, methodGroup, delegateType.DelegateInvokeMethod.ParameterCount);

            analyzedArguments.Free();
            result.Free();
            return conversion;
        }

        public static void GetDelegateOrFunctionPointerArguments(SyntaxNode syntax, AnalyzedArguments analyzedArguments, ImmutableArray<ParameterSymbol> delegateParameters, CSharpCompilation compilation)
        {
            foreach (var p in delegateParameters)
            {
                ParameterSymbol parameter = p;

                // In ExpressionBinder::BindGrpConversion, the native compiler substitutes object in place of dynamic.  This is
                // necessary because conversions from expressions of type dynamic always succeed, whereas conversions from the
                // type generally fail (modulo identity conversions).  This is not reflected in the C# 4 spec, but will be
                // incorporated going forward.  See DevDiv #742345 for additional details.
                // NOTE: Dev11 does a deep substitution (e.g. C<C<C<dynamic>>> -> C<C<C<object>>>), but that seems redundant.
                if (parameter.Type.IsDynamic())
                {
                    // If we don't have System.Object, then we'll get an error type, which will cause overload resolution to fail, 
                    // which will cause some error to be reported.  That's sufficient (i.e. no need to specifically report its absence here).
                    parameter = new SignatureOnlyParameterSymbol(
                        TypeWithAnnotations.Create(compilation.GetSpecialType(SpecialType.System_Object), customModifiers: parameter.TypeWithAnnotations.CustomModifiers), parameter.RefCustomModifiers, parameter.IsParams, parameter.RefKind);
                }

                analyzedArguments.Arguments.Add(new BoundParameter(syntax, parameter) { WasCompilerGenerated = true });
                analyzedArguments.RefKinds.Add(parameter.RefKind);
            }
        }

        private static Conversion ToConversion(OverloadResolutionResult<MethodSymbol> result, MethodGroup methodGroup, int parameterCount)
        {
            // 6.6 An implicit conversion (6.1) exists from a method group (7.1) to a compatible
            // delegate type. Given a delegate type D and an expression E that is classified as
            // a method group, an implicit conversion exists from E to D if E contains at least
            // one method that is applicable in its normal form (7.5.3.1) to an argument list
            // constructed by use of the parameter types and modifiers of D...

            // SPEC VIOLATION: Unfortunately, we cannot exactly implement the specification for
            // the scenario in which an extension method that extends a value type is converted
            // to a delegate. The code we generate that captures a delegate to a static method
            // that is "partially evaluated" with the bound-to-the-delegate first argument
            // requires that the first argument be of reference type.

            // SPEC VIOLATION: Similarly, we cannot capture a method of Nullable<T>, because
            // boxing a Nullable<T> gives a T, not a boxed Nullable<T>. (We can capture methods
            // of object on a nullable receiver, but not GetValueOrDefault.)

            if (!result.Succeeded)
            {
                return Conversion.NoConversion;
            }

            MethodSymbol method = result.BestResult.Member;

            if (methodGroup.IsExtensionMethodGroup && !method.Parameters[0].Type.IsReferenceType)
            {
                return Conversion.NoConversion;
            }

            //cannot capture stack-only types.
            if (method.RequiresInstanceReceiver && methodGroup.Receiver?.Type?.IsRestrictedType() == true)
            {
                return Conversion.NoConversion;
            }

            if (method.ContainingType.IsNullableType() && !method.IsOverride)
            {
                return Conversion.NoConversion;
            }

            // NOTE: Section 6.6 will be slightly updated:
            //
            //   - The candidate methods considered are only those methods that are applicable in their
            //     normal form (§7.5.3.1), and do not omit any optional parameters. Thus, candidate methods
            //     are ignored if they are applicable only in their expanded form, or if one or more of their
            //     optional parameters do not have a corresponding parameter in the targeted delegate type.
            //   
            // Therefore, we shouldn't get here unless the parameter count matches.

            // NOTE: Delegate type compatibility is important, but is not part of the existence check.

            Debug.Assert(method.ParameterCount == parameterCount + (methodGroup.IsExtensionMethodGroup ? 1 : 0));

            return new Conversion(ConversionKind.MethodGroup, method, methodGroup.IsExtensionMethodGroup);
        }

        public override Conversion GetStackAllocConversion(BoundStackAllocArrayCreation sourceExpression, TypeSymbol destination, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            if (sourceExpression.NeedsToBeConverted())
            {
                Debug.Assert((object)sourceExpression.Type == null);
                Debug.Assert((object)sourceExpression.ElementType != null);

                var sourceAsPointer = new PointerTypeSymbol(TypeWithAnnotations.Create(sourceExpression.ElementType));
                var pointerConversion = ClassifyImplicitConversionFromType(sourceAsPointer, destination, ref useSiteInfo);

                if (pointerConversion.IsValid)
                {
                    return Conversion.MakeStackAllocToPointerType(pointerConversion);
                }
                else
                {
                    var spanType = _binder.GetWellKnownType(WellKnownType.System_Span_T, ref useSiteInfo);
                    if (spanType.TypeKind == TypeKind.Struct && spanType.IsRefLikeType)
                    {
                        var spanType_T = spanType.Construct(sourceExpression.ElementType);
                        var spanConversion = ClassifyImplicitConversionFromType(spanType_T, destination, ref useSiteInfo);

                        if (spanConversion.Exists)
                        {
                            return Conversion.MakeStackAllocToSpanType(spanConversion);
                        }
                    }
                }
            }

            return Conversion.NoConversion;
        }
    }
}
