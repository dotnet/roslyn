// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

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

#nullable enable
        protected override CSharpCompilation Compilation { get { return _binder.Compilation; } }

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

            Debug.Assert(methodSymbol == ((NamedTypeSymbol)destination).DelegateInvokeMethod);

            if (methodSymbol.OriginalDefinition is SynthesizedDelegateInvokeMethod invoke)
            {
                // If synthesizing a delegate with `params` array, check that `ParamArrayAttribute` is available.
                if (invoke.Parameters is [.., { IsParamsArray: true }])
                {
                    Binder.AddUseSiteDiagnosticForSynthesizedAttribute(
                        Compilation,
                        WellKnownMember.System_ParamArrayAttribute__ctor,
                        ref useSiteInfo);
                }

                // If synthesizing a delegate with `decimal`/`DateTime` default value,
                // check that the corresponding `*ConstantAttribute` is available.
                foreach (var p in invoke.Parameters)
                {
                    var defaultValue = p.ExplicitDefaultConstantValue;
                    if (defaultValue != ConstantValue.NotAvailable)
                    {
                        WellKnownMember? member = defaultValue.SpecialType switch
                        {
                            SpecialType.System_Decimal => WellKnownMember.System_Runtime_CompilerServices_DecimalConstantAttribute__ctor,
                            SpecialType.System_DateTime => WellKnownMember.System_Runtime_CompilerServices_DateTimeConstantAttribute__ctor,
                            _ => null
                        };
                        if (member != null)
                        {
                            Binder.AddUseSiteDiagnosticForSynthesizedAttribute(
                                Compilation,
                                member.GetValueOrDefault(),
                                ref useSiteInfo);
                        }
                    }
                }

                // If synthesizing a delegate with an [UnscopedRef] parameter, check the attribute is available.
                if (invoke.Parameters.Any(p => p.HasUnscopedRefAttribute))
                {
                    Binder.AddUseSiteDiagnosticForSynthesizedAttribute(
                        Compilation,
                        WellKnownMember.System_Diagnostics_CodeAnalysis_UnscopedRefAttribute__ctor,
                        ref useSiteInfo);
                }
            }

            var resolution = ResolveDelegateOrFunctionPointerMethodGroup(_binder, source, methodSymbol, isFunctionPointer, callingConventionInfo, ref useSiteInfo);
            Debug.Assert(!resolution.IsNonMethodExtensionMember(out _));

            var conversion = (resolution.IsEmpty || resolution.HasAnyErrors) ?
                Conversion.NoConversion :
                ToConversion(resolution.OverloadResolutionResult, resolution.MethodGroup, methodSymbol.ParameterCount);
            resolution.Free();
            return conversion;
        }
#nullable disable

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

        protected override Conversion GetInterpolatedStringConversion(BoundExpression source, TypeSymbol destination, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            if (_binder.InParameterDefaultValue || _binder.InAttributeArgument)
            {
                // We don't consider when we're in default parameter values or attributes to avoid cycles. This is an error scenario,
                // so we don't care if we accidentally miss a parameter being applicable.
                return Conversion.NoConversion;
            }

            if (destination is NamedTypeSymbol { IsInterpolatedStringHandlerType: true })
            {
                return Conversion.InterpolatedStringHandler;
            }

            if (source is BoundBinaryOperator)
            {
                return Conversion.NoConversion;
            }

            // An interpolated string expression may be converted to the types
            // System.IFormattable and System.FormattableString
            Debug.Assert(source is BoundUnconvertedInterpolatedString);
            return (TypeSymbol.Equals(destination, Compilation.GetWellKnownType(WellKnownType.System_IFormattable), TypeCompareKind.ConsiderEverything) ||
                    TypeSymbol.Equals(destination, Compilation.GetWellKnownType(WellKnownType.System_FormattableString), TypeCompareKind.ConsiderEverything))
                ? Conversion.InterpolatedString : Conversion.NoConversion;
        }

#nullable enable
        protected override Conversion GetCollectionExpressionConversion(
            BoundUnconvertedCollectionExpression node,
            TypeSymbol targetType,
            ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            var syntax = node.Syntax;
            var collectionTypeKind = GetCollectionExpressionTypeKind(Compilation, targetType, out TypeWithAnnotations elementTypeWithAnnotations);
            var elementType = elementTypeWithAnnotations.Type;
            switch (collectionTypeKind)
            {
                case CollectionExpressionTypeKind.None:
                    return Conversion.NoConversion;

                case CollectionExpressionTypeKind.ImplementsIEnumerable:
                case CollectionExpressionTypeKind.CollectionBuilder:
                    {
                        _binder.TryGetCollectionIterationType(syntax, targetType, out elementTypeWithAnnotations);
                        elementType = elementTypeWithAnnotations.Type;
                        if (elementType is null)
                        {
                            return Conversion.NoConversion;
                        }
                    }
                    break;
            }

            Debug.Assert(elementType is { });
            var elements = node.Elements;

            MethodSymbol? constructor = null;
            bool isExpanded = false;

            if (collectionTypeKind == CollectionExpressionTypeKind.ImplementsIEnumerable)
            {
                if (!_binder.HasCollectionExpressionApplicableConstructor(syntax, targetType, out constructor, out isExpanded, BindingDiagnosticBag.Discarded))
                {
                    return Conversion.NoConversion;
                }

                if (elements.Length > 0 &&
                    !_binder.HasCollectionExpressionApplicableAddMethod(syntax, targetType, addMethods: out _, BindingDiagnosticBag.Discarded))
                {
                    return Conversion.NoConversion;
                }
            }

            var builder = ArrayBuilder<Conversion>.GetInstance(elements.Length);
            foreach (var element in elements)
            {
                Conversion elementConversion = convertElement(element, elementType, ref useSiteInfo);
                if (!elementConversion.Exists)
                {
                    builder.Free();
                    return Conversion.NoConversion;
                }

                builder.Add(elementConversion);
            }

            return Conversion.CreateCollectionExpressionConversion(collectionTypeKind, elementType, constructor, isExpanded, builder.ToImmutableAndFree());

            Conversion convertElement(BoundNode element, TypeSymbol elementType, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
            {
                return element switch
                {
                    BoundCollectionExpressionSpreadElement spreadElement => GetCollectionExpressionSpreadElementConversion(spreadElement, elementType, ref useSiteInfo),
                    _ => ClassifyImplicitConversionFromExpression((BoundExpression)element, elementType, ref useSiteInfo),
                };
            }
        }

        internal Conversion GetCollectionExpressionSpreadElementConversion(
            BoundCollectionExpressionSpreadElement element,
            TypeSymbol targetType,
            ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            var enumeratorInfo = element.EnumeratorInfoOpt;
            if (enumeratorInfo is null)
            {
                return Conversion.NoConversion;
            }
            return ClassifyImplicitConversionFromExpression(
                new BoundValuePlaceholder(element.Syntax, enumeratorInfo.ElementType),
                targetType,
                ref useSiteInfo);
        }
#nullable disable

        /// <summary>
        /// Resolve method group based on the optional delegate invoke method.
        /// If the invoke method is null, ignore arguments in resolution.
        /// </summary>
        private static MethodGroupResolution ResolveDelegateOrFunctionPointerMethodGroup(Binder binder, BoundMethodGroup source, MethodSymbol delegateInvokeMethodOpt, bool isFunctionPointer, in CallingConventionInfo callingConventionInfo, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            MethodGroupResolution resolution;
            if ((object)delegateInvokeMethodOpt != null)
            {
                var analyzedArguments = AnalyzedArguments.GetInstance();
                GetDelegateOrFunctionPointerArguments(source.Syntax, analyzedArguments, delegateInvokeMethodOpt.Parameters, binder.Compilation);
                resolution = binder.ResolveMethodGroup(source, analyzedArguments, useSiteInfo: ref useSiteInfo,
                    options: OverloadResolution.Options.InferWithDynamic | OverloadResolution.Options.IsMethodGroupConversion |
                             (isFunctionPointer ? OverloadResolution.Options.IsFunctionPointerResolution : OverloadResolution.Options.None),
                    acceptOnlyMethods: true, returnRefKind: delegateInvokeMethodOpt.RefKind, returnType: delegateInvokeMethodOpt.ReturnType,
                    callingConventionInfo: callingConventionInfo);
                analyzedArguments.Free();
            }
            else
            {
                resolution = binder.ResolveMethodGroup(source, analyzedArguments: null, useSiteInfo: ref useSiteInfo, options: OverloadResolution.Options.IsMethodGroupConversion, acceptOnlyMethods: true);
            }

            Debug.Assert(!resolution.IsNonMethodExtensionMember(out _));
            return resolution;
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
            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = binder.GetNewCompoundUseSiteInfo(diagnostics);
            var (invokeMethodOpt, isFunctionPointer, callingConventionInfo) = GetDelegateInvokeOrFunctionPointerMethodIfAvailable(targetType);
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
                            Debug.Assert(method.IsExtensionMethod || method.IsExtensionBlockMember());

                            ParameterSymbol thisParameter;

                            if (method.IsExtensionMethod)
                            {
                                thisParameter = method.Parameters[0];
                            }
                            else if (method.IsStatic)
                            {
                                thisParameter = null;
                            }
                            else
                            {
                                thisParameter = method.ContainingType.ExtensionParameter;
                            }

                            if (thisParameter?.Type.IsReferenceType == false)
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
                        
                        // Even if no diagnostics were reported, check if any candidate method has an error return type.
                        // This can happen when ERR_BadRetType is suppressed for methods with omitted type arguments.
                        if (!hasErrors && result.Results.Length > 0)
                        {
                            foreach (var candidate in result.Results)
                            {
                                if (candidate.Member.ReturnType.ContainsErrorType())
                                {
                                    hasErrors = true;
                                    break;
                                }
                            }
                        }
                        
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
                options: OverloadResolution.Options.IsMethodGroupConversion,
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
                        TypeWithAnnotations.Create(compilation.GetSpecialType(SpecialType.System_Object), customModifiers: parameter.TypeWithAnnotations.CustomModifiers), parameter.RefCustomModifiers,
                                                   isParamsArray: parameter.IsParamsArray, isParamsCollection: parameter.IsParamsCollection, parameter.RefKind);
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

            if (methodGroup.IsExtensionMethodGroup)
            {
                if (!(method.IsExtensionBlockMember() && method.IsStatic) && !Binder.GetReceiverParameter(method).Type.IsReferenceType)
                {
                    return Conversion.NoConversion;
                }
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

            bool isExtensionMethod = methodGroup.IsExtensionMethodGroup && !method.IsExtensionBlockMember();
            Debug.Assert(method.ParameterCount == parameterCount + (isExtensionMethod ? 1 : 0));

            return new Conversion(ConversionKind.MethodGroup, method, isExtensionMethod: isExtensionMethod);
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

        /// <summary>
        /// Returns this instance if includeNullability is correct, and returns a
        /// cached clone of this instance with distinct IncludeNullability otherwise.
        /// </summary>
        internal new Conversions WithNullability(bool includeNullability)
        {
            return (Conversions)base.WithNullability(includeNullability);
        }

        protected override bool IsAttributeArgumentBinding => _binder.InAttributeArgument;

        protected override bool IsParameterDefaultValueBinding => _binder.InParameterDefaultValue;
    }
}
