// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class FunctionPointerMethodSymbol : MethodSymbol
    {
        private readonly ImmutableArray<FunctionPointerParameterSymbol> _parameters;
        private ImmutableHashSet<CustomModifier>? _lazyCallingConventionModifiers;

        public static FunctionPointerMethodSymbol CreateFromSource(FunctionPointerTypeSyntax syntax, Binder typeBinder, BindingDiagnosticBag diagnostics, ConsList<TypeSymbol> basesBeingResolved, bool suppressUseSiteDiagnostics)
        {
            ArrayBuilder<CustomModifier> customModifiers = ArrayBuilder<CustomModifier>.GetInstance();
            CallingConvention callingConvention = getCallingConvention(typeBinder.Compilation, syntax.CallingConvention, customModifiers, diagnostics);

            RefKind refKind = RefKind.None;
            TypeWithAnnotations returnType;

            if (syntax.ParameterList.Parameters.Count == 0)
            {
                returnType = TypeWithAnnotations.Create(typeBinder.CreateErrorType());
            }
            else
            {
                FunctionPointerParameterSyntax? returnTypeParameter = syntax.ParameterList.Parameters[^1];
                SyntaxTokenList modifiers = returnTypeParameter.Modifiers;
                for (int i = 0; i < modifiers.Count; i++)
                {
                    SyntaxToken modifier = modifiers[i];
                    switch (modifier.Kind())
                    {
                        case SyntaxKind.RefKeyword when refKind == RefKind.None:
                            if (modifiers.Count > i + 1 && modifiers[i + 1].Kind() == SyntaxKind.ReadOnlyKeyword)
                            {
                                i++;
                                refKind = RefKind.RefReadOnly;
                                customModifiers.AddRange(ParameterHelpers.CreateInModifiers(typeBinder, diagnostics, returnTypeParameter));
                            }
                            else
                            {
                                refKind = RefKind.Ref;
                            }

                            break;

                        case SyntaxKind.RefKeyword:
                            Debug.Assert(refKind != RefKind.None);
                            // A return type can only have one '{0}' modifier.
                            diagnostics.Add(ErrorCode.ERR_DupReturnTypeMod, modifier.GetLocation(), modifier.Text);
                            break;

                        default:
                            // '{0}' is not a valid function pointer return type modifier. Valid modifiers are 'ref' and 'ref readonly'.
                            diagnostics.Add(ErrorCode.ERR_InvalidFuncPointerReturnTypeModifier, modifier.GetLocation(), modifier.Text);
                            break;
                    }
                }

                returnType = typeBinder.BindType(returnTypeParameter.Type, diagnostics, basesBeingResolved, suppressUseSiteDiagnostics);

                if (returnType.IsVoidType() && refKind != RefKind.None)
                {
                    diagnostics.Add(ErrorCode.ERR_NoVoidHere, returnTypeParameter.Location);
                }
                else if (returnType.IsStatic)
                {
                    diagnostics.Add(ErrorFacts.GetStaticClassReturnCode(useWarning: false), returnTypeParameter.Location, returnType);
                }
                else if (returnType.IsRestrictedType(ignoreSpanLikeTypes: true))
                {
                    diagnostics.Add(ErrorCode.ERR_MethodReturnCantBeRefAny, returnTypeParameter.Location, returnType);
                }
            }

            var refCustomModifiers = ImmutableArray<CustomModifier>.Empty;
            if (refKind != RefKind.None)
            {
                refCustomModifiers = customModifiers.ToImmutableAndFree();
            }
            else
            {
                returnType = returnType.WithModifiers(customModifiers.ToImmutableAndFree());
            }

            return new FunctionPointerMethodSymbol(
                callingConvention,
                refKind,
                returnType,
                refCustomModifiers,
                syntax,
                typeBinder,
                diagnostics,
                suppressUseSiteDiagnostics,
                useUpdatedEscapeRules: typeBinder.UseUpdatedEscapeRules);

            static CallingConvention getCallingConvention(CSharpCompilation compilation, FunctionPointerCallingConventionSyntax? callingConventionSyntax, ArrayBuilder<CustomModifier> customModifiers, BindingDiagnosticBag diagnostics)
            {
                switch (callingConventionSyntax?.ManagedOrUnmanagedKeyword.Kind())
                {
                    case null:
                        return CallingConvention.Default;

                    case SyntaxKind.ManagedKeyword:
                        // Possible if we get a node not constructed by the parser
                        if (callingConventionSyntax.UnmanagedCallingConventionList is object && !callingConventionSyntax.ContainsDiagnostics)
                        {
                            diagnostics.Add(ErrorCode.ERR_CannotSpecifyManagedWithUnmanagedSpecifiers, callingConventionSyntax.UnmanagedCallingConventionList.GetLocation());
                        }
                        return CallingConvention.Default;

                    case SyntaxKind.UnmanagedKeyword:
                        // From the function pointers spec:
                        // C# recognizes 4 special identifiers that map to specific existing unmanaged CallKinds from ECMA 335.
                        // In order for this mapping to occur, these identifiers must be specified on their own, with no other
                        // identifiers, and this requirement is encoded into the spec for unmanaged_calling_conventions. These
                        // identifiers are Cdecl, Thiscall, Stdcall, and Fastcall, which correspond to unmanaged cdecl,
                        // unmanaged thiscall, unmanaged stdcall, and unmanaged fastcall, respectively. If more than one identifier
                        // is specified, or the single identifier is not of the specially recognized identifiers, we perform special
                        // name lookup on the identifier with the following rules:
                        //
                        //  * We prepend the identifier with the string CallConv
                        //  * We look only at types defined in the System.Runtime.CompilerServices namespace.
                        //  * We look only at types defined in the core library of the application, which is the library that defines
                        //    System.Object and has no dependencies.
                        //
                        // If lookup succeeds on all of the identifiers specified in an unmanaged_calling_convention, we encode the
                        // CallKind as unmanaged, and encode each of the resolved types in the set of modopts at the beginning of
                        // the function pointer signature.

                        switch (callingConventionSyntax.UnmanagedCallingConventionList)
                        {
                            case null:
                                checkUnmanagedSupport(compilation, callingConventionSyntax.ManagedOrUnmanagedKeyword.GetLocation(), diagnostics);
                                return CallingConvention.Unmanaged;

                            case { CallingConventions: { Count: 1 } specifiers }:
                                return specifiers[0].Name switch
                                {
                                    // Special identifiers cases
                                    { ValueText: "Cdecl" } => CallingConvention.CDecl,
                                    { ValueText: "Stdcall" } => CallingConvention.Standard,
                                    { ValueText: "Thiscall" } => CallingConvention.ThisCall,
                                    { ValueText: "Fastcall" } => CallingConvention.FastCall,

                                    // Unknown identifier case
                                    _ => handleSingleConvention(specifiers[0], compilation, customModifiers, diagnostics)
                                };

                            case { CallingConventions: { Count: 0 } } unmanagedList:
                                // Should never be possible from parser-constructed code (parser will always provide at least a missing identifier token),
                                // so diagnostic quality isn't hugely important
                                if (!unmanagedList.ContainsDiagnostics)
                                {
                                    diagnostics.Add(ErrorCode.ERR_InvalidFunctionPointerCallingConvention, unmanagedList.OpenBracketToken.GetLocation(), "");
                                }
                                return CallingConvention.Default;

                            case { CallingConventions: var specifiers }:
                                // More than one identifier case
                                checkUnmanagedSupport(compilation, callingConventionSyntax.ManagedOrUnmanagedKeyword.GetLocation(), diagnostics);
                                foreach (FunctionPointerUnmanagedCallingConventionSyntax? specifier in specifiers)
                                {
                                    CustomModifier? modifier = handleIndividualUnrecognizedSpecifier(specifier, compilation, diagnostics);
                                    if (modifier is object)
                                    {
                                        customModifiers.Add(modifier);
                                    }
                                }

                                return CallingConvention.Unmanaged;
                        }

                    case var unexpected:
                        throw ExceptionUtilities.UnexpectedValue(unexpected);
                }

                static CallingConvention handleSingleConvention(FunctionPointerUnmanagedCallingConventionSyntax specifier, CSharpCompilation compilation, ArrayBuilder<CustomModifier> customModifiers, BindingDiagnosticBag diagnostics)
                {
                    checkUnmanagedSupport(compilation, specifier.GetLocation(), diagnostics);
                    CustomModifier? modifier = handleIndividualUnrecognizedSpecifier(specifier, compilation, diagnostics);
                    if (modifier is object)
                    {
                        customModifiers.Add(modifier);
                    }
                    return CallingConvention.Unmanaged;
                }

                static CustomModifier? handleIndividualUnrecognizedSpecifier(FunctionPointerUnmanagedCallingConventionSyntax specifier, CSharpCompilation compilation, BindingDiagnosticBag diagnostics)
                {
                    string specifierText = specifier.Name.ValueText;
                    if (string.IsNullOrEmpty(specifierText))
                    {
                        return null;
                    }

                    string typeName = "CallConv" + specifierText;
                    var metadataName = MetadataTypeName.FromNamespaceAndTypeName("System.Runtime.CompilerServices", typeName, useCLSCompliantNameArityEncoding: true, forcedArity: 0);
                    NamedTypeSymbol? specifierType;
                    specifierType = compilation.Assembly.CorLibrary.LookupDeclaredTopLevelMetadataType(ref metadataName);
                    Debug.Assert(specifierType?.IsErrorType() != true);
                    Debug.Assert(specifierType is null || ReferenceEquals(specifierType.ContainingAssembly, compilation.Assembly.CorLibrary));

                    if (specifierType is null)
                    {
                        specifierType = new MissingMetadataTypeSymbol.TopLevel(compilation.Assembly.CorLibrary.Modules[0], ref metadataName, new CSDiagnosticInfo(ErrorCode.ERR_TypeNotFound, typeName));
                    }
                    else if (specifierType.DeclaredAccessibility != Accessibility.Public)
                    {
                        diagnostics.Add(ErrorCode.ERR_TypeMustBePublic, specifier.GetLocation(), specifierType);
                    }

                    diagnostics.Add(specifierType.GetUseSiteInfo(), specifier);

                    return CSharpCustomModifier.CreateOptional(specifierType);
                }

                static void checkUnmanagedSupport(CSharpCompilation compilation, Location errorLocation, BindingDiagnosticBag diagnostics)
                {
                    if (!compilation.Assembly.RuntimeSupportsUnmanagedSignatureCallingConvention)
                    {
                        diagnostics.Add(ErrorCode.ERR_RuntimeDoesNotSupportUnmanagedDefaultCallConv, errorLocation);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a function pointer method symbol from individual parts. This method should only be used when diagnostics are not needed.
        /// This should only be used from testing code.
        /// </summary>
        internal static FunctionPointerMethodSymbol CreateFromPartsForTest(
            CallingConvention callingConvention,
            TypeWithAnnotations returnType,
            ImmutableArray<CustomModifier> refCustomModifiers,
            RefKind returnRefKind,
            ImmutableArray<TypeWithAnnotations> parameterTypes,
            ImmutableArray<ImmutableArray<CustomModifier>> parameterRefCustomModifiers,
            ImmutableArray<RefKind> parameterRefKinds,
            CSharpCompilation compilation)
        {
            return new FunctionPointerMethodSymbol(
                callingConvention,
                returnRefKind,
                returnType,
                refCustomModifiers,
                parameterTypes,
                parameterRefCustomModifiers,
                parameterRefKinds,
                compilation);
        }

        /// <summary>
        /// Creates a function pointer method symbol from individual parts. This method should only be used when diagnostics are not needed.
        /// </summary>
        internal static FunctionPointerMethodSymbol CreateFromParts(
            CallingConvention callingConvention,
            ImmutableArray<CustomModifier> callingConventionModifiers,
            TypeWithAnnotations returnTypeWithAnnotations,
            RefKind returnRefKind,
            ImmutableArray<TypeWithAnnotations> parameterTypes,
            ImmutableArray<RefKind> parameterRefKinds,
            CSharpCompilation compilation)
        {
            var modifiersBuilder = ArrayBuilder<CustomModifier>.GetInstance();

            if (!callingConventionModifiers.IsDefaultOrEmpty)
            {
                Debug.Assert(callingConvention == CallingConvention.Unmanaged);
                modifiersBuilder.AddRange(callingConventionModifiers);
            }

            ImmutableArray<CustomModifier> refCustomModifiers;
            if (returnRefKind == RefKind.None)
            {
                refCustomModifiers = ImmutableArray<CustomModifier>.Empty;
                returnTypeWithAnnotations = returnTypeWithAnnotations.WithModifiers(modifiersBuilder.ToImmutableAndFree());
            }
            else
            {
                if (GetCustomModifierForRefKind(returnRefKind, compilation) is CustomModifier modifier)
                {
                    modifiersBuilder.Add(modifier);
                }
                refCustomModifiers = modifiersBuilder.ToImmutableAndFree();
            }

            return new FunctionPointerMethodSymbol(
                callingConvention,
                returnRefKind,
                returnTypeWithAnnotations,
                refCustomModifiers,
                parameterTypes,
                parameterRefCustomModifiers: default,
                parameterRefKinds,
                compilation);
        }

        private static CustomModifier? GetCustomModifierForRefKind(RefKind refKind, CSharpCompilation compilation)
        {
            Debug.Assert(refKind is RefKind.None or RefKind.In or RefKind.Ref or RefKind.Out);

            var attributeType = refKind switch
            {
                RefKind.In => compilation.GetWellKnownType(WellKnownType.System_Runtime_InteropServices_InAttribute),
                RefKind.Out => compilation.GetWellKnownType(WellKnownType.System_Runtime_InteropServices_OutAttribute),
                _ => null
            };

            if (attributeType is null)
            {
                Debug.Assert(refKind != RefKind.Out && refKind != RefKind.In);
                return null;
            }

            return CSharpCustomModifier.CreateRequired(attributeType);
        }

        public static FunctionPointerMethodSymbol CreateFromMetadata(ModuleSymbol containingModule, CallingConvention callingConvention, ImmutableArray<ParamInfo<TypeSymbol>> retAndParamTypes)
            => new FunctionPointerMethodSymbol(callingConvention, retAndParamTypes, useUpdatedEscapeRules: containingModule.UseUpdatedEscapeRules);

        public FunctionPointerMethodSymbol SubstituteParameterSymbols(
            TypeWithAnnotations substitutedReturnType,
            ImmutableArray<TypeWithAnnotations> substitutedParameterTypes,
            ImmutableArray<CustomModifier> refCustomModifiers = default,
            ImmutableArray<ImmutableArray<CustomModifier>> paramRefCustomModifiers = default)
            => new FunctionPointerMethodSymbol(
                this.CallingConvention,
                this.RefKind,
                substitutedReturnType,
                refCustomModifiers.IsDefault ? this.RefCustomModifiers : refCustomModifiers,
                this.Parameters,
                substitutedParameterTypes,
                paramRefCustomModifiers,
                this.UseUpdatedEscapeRules);

        internal FunctionPointerMethodSymbol MergeEquivalentTypes(FunctionPointerMethodSymbol signature, VarianceKind variance)
        {
            Debug.Assert(RefKind == signature.RefKind);
            var returnVariance = RefKind == RefKind.None ? variance : VarianceKind.None;
            var mergedReturnType = ReturnTypeWithAnnotations.MergeEquivalentTypes(signature.ReturnTypeWithAnnotations, returnVariance);

            var mergedParameterTypes = ImmutableArray<TypeWithAnnotations>.Empty;
            bool hasParamChanges = false;
            if (_parameters.Length > 0)
            {
                var paramMergedTypesBuilder = ArrayBuilder<TypeWithAnnotations>.GetInstance(_parameters.Length);
                for (int i = 0; i < _parameters.Length; i++)
                {
                    var thisParam = _parameters[i];
                    var otherParam = signature._parameters[i];
                    Debug.Assert(thisParam.RefKind == otherParam.RefKind);
                    var paramVariance = (variance, thisParam.RefKind) switch
                    {
                        (VarianceKind.In, RefKind.None) => VarianceKind.Out,
                        (VarianceKind.Out, RefKind.None) => VarianceKind.In,
                        _ => VarianceKind.None,
                    };

                    var mergedParameterType = thisParam.TypeWithAnnotations.MergeEquivalentTypes(otherParam.TypeWithAnnotations, paramVariance);
                    paramMergedTypesBuilder.Add(mergedParameterType);
                    if (!mergedParameterType.IsSameAs(thisParam.TypeWithAnnotations))
                    {
                        hasParamChanges = true;
                    }
                }

                if (hasParamChanges)
                {
                    mergedParameterTypes = paramMergedTypesBuilder.ToImmutableAndFree();
                }
                else
                {
                    paramMergedTypesBuilder.Free();
                    mergedParameterTypes = ParameterTypesWithAnnotations;
                }
            }

            if (hasParamChanges || !mergedReturnType.IsSameAs(ReturnTypeWithAnnotations))
            {
                return SubstituteParameterSymbols(mergedReturnType, mergedParameterTypes);
            }
            else
            {
                return this;
            }
        }

        public FunctionPointerMethodSymbol SetNullabilityForReferenceTypes(Func<TypeWithAnnotations, TypeWithAnnotations> transform)
        {
            var transformedReturn = transform(ReturnTypeWithAnnotations);

            var transformedParameterTypes = ImmutableArray<TypeWithAnnotations>.Empty;
            bool hasParamChanges = false;
            if (_parameters.Length > 0)
            {
                var paramTypesBuilder = ArrayBuilder<TypeWithAnnotations>.GetInstance(_parameters.Length);
                foreach (var param in _parameters)
                {
                    var transformedType = transform(param.TypeWithAnnotations);
                    paramTypesBuilder.Add(transformedType);
                    if (!transformedType.IsSameAs(param.TypeWithAnnotations))
                    {
                        hasParamChanges = true;
                    }
                }

                if (hasParamChanges)
                {
                    transformedParameterTypes = paramTypesBuilder.ToImmutableAndFree();
                }
                else
                {
                    paramTypesBuilder.Free();
                    transformedParameterTypes = ParameterTypesWithAnnotations;
                }

            }

            if (hasParamChanges || !transformedReturn.IsSameAs(ReturnTypeWithAnnotations))
            {
                return SubstituteParameterSymbols(transformedReturn, transformedParameterTypes);
            }
            else
            {
                return this;
            }
        }

        private FunctionPointerMethodSymbol(
            CallingConvention callingConvention,
            RefKind refKind,
            TypeWithAnnotations returnType,
            ImmutableArray<CustomModifier> refCustomModifiers,
            ImmutableArray<ParameterSymbol> originalParameters,
            ImmutableArray<TypeWithAnnotations> substitutedParameterTypes,
            ImmutableArray<ImmutableArray<CustomModifier>> substitutedRefCustomModifiers,
            bool useUpdatedEscapeRules)
        {
            Debug.Assert(originalParameters.Length == substitutedParameterTypes.Length);
            Debug.Assert(substitutedRefCustomModifiers.IsDefault || originalParameters.Length == substitutedRefCustomModifiers.Length);
            RefCustomModifiers = refCustomModifiers;
            CallingConvention = callingConvention;
            RefKind = refKind;
            ReturnTypeWithAnnotations = returnType;
            UseUpdatedEscapeRules = useUpdatedEscapeRules;

            if (originalParameters.Length > 0)
            {
                var paramsBuilder = ArrayBuilder<FunctionPointerParameterSymbol>.GetInstance(originalParameters.Length);
                for (int i = 0; i < originalParameters.Length; i++)
                {
                    var originalParam = originalParameters[i];
                    var substitutedType = substitutedParameterTypes[i];
                    var customModifiers = substitutedRefCustomModifiers.IsDefault ? originalParam.RefCustomModifiers : substitutedRefCustomModifiers[i];
                    paramsBuilder.Add(new FunctionPointerParameterSymbol(
                        substitutedType,
                        originalParam.RefKind,
                        originalParam.Ordinal,
                        containingSymbol: this,
                        customModifiers));
                }

                _parameters = paramsBuilder.ToImmutableAndFree();
            }
            else
            {
                _parameters = ImmutableArray<FunctionPointerParameterSymbol>.Empty;
            }
        }

        /// <summary>
        /// Creates a function pointer method symbol from individual parts. This method should only be used when diagnostics are not needed.
        /// </summary>
        private FunctionPointerMethodSymbol(
            CallingConvention callingConvention,
            RefKind refKind,
            TypeWithAnnotations returnTypeWithAnnotations,
            ImmutableArray<CustomModifier> refCustomModifiers,
            ImmutableArray<TypeWithAnnotations> parameterTypes,
            ImmutableArray<ImmutableArray<CustomModifier>> parameterRefCustomModifiers,
            ImmutableArray<RefKind> parameterRefKinds,
            CSharpCompilation compilation)
        {
            Debug.Assert(refKind != RefKind.Out);
            Debug.Assert(refCustomModifiers.IsDefaultOrEmpty || refKind != RefKind.None);
            Debug.Assert(parameterRefCustomModifiers.IsDefault || parameterRefCustomModifiers.Length == parameterTypes.Length);
            RefCustomModifiers = refCustomModifiers.IsDefault ? getCustomModifierArrayForRefKind(refKind, compilation) : refCustomModifiers;
            RefKind = refKind;
            CallingConvention = callingConvention;
            ReturnTypeWithAnnotations = returnTypeWithAnnotations;
            UseUpdatedEscapeRules = compilation.SourceModule.UseUpdatedEscapeRules;

            _parameters = parameterTypes.ZipAsArray(parameterRefKinds, (Method: this, Comp: compilation, ParamRefCustomModifiers: parameterRefCustomModifiers),
                (type, refKind, i, arg) =>
                {
                    var refCustomModifiers = arg.ParamRefCustomModifiers.IsDefault ? getCustomModifierArrayForRefKind(refKind, arg.Comp) : arg.ParamRefCustomModifiers[i];
                    Debug.Assert(refCustomModifiers.IsEmpty || refKind != RefKind.None);
                    return new FunctionPointerParameterSymbol(type, refKind, i, arg.Method, refCustomModifiers: refCustomModifiers);
                });

            static ImmutableArray<CustomModifier> getCustomModifierArrayForRefKind(RefKind refKind, CSharpCompilation compilation)
                => GetCustomModifierForRefKind(refKind, compilation) is { } modifier ? ImmutableArray.Create(modifier) : ImmutableArray<CustomModifier>.Empty;
        }

        private FunctionPointerMethodSymbol(
            CallingConvention callingConvention,
            RefKind refKind,
            TypeWithAnnotations returnType,
            ImmutableArray<CustomModifier> refCustomModifiers,
            FunctionPointerTypeSyntax syntax,
            Binder typeBinder,
            BindingDiagnosticBag diagnostics,
            bool suppressUseSiteDiagnostics,
            bool useUpdatedEscapeRules)
        {
            RefCustomModifiers = refCustomModifiers;
            CallingConvention = callingConvention;
            RefKind = refKind;
            ReturnTypeWithAnnotations = returnType;
            UseUpdatedEscapeRules = useUpdatedEscapeRules;
            _parameters = syntax.ParameterList.Parameters.Count > 1
                ? ParameterHelpers.MakeFunctionPointerParameters(
                    typeBinder,
                    this,
                    syntax.ParameterList.Parameters,
                    diagnostics,
                    suppressUseSiteDiagnostics)
                : ImmutableArray<FunctionPointerParameterSymbol>.Empty;
        }

        private FunctionPointerMethodSymbol(CallingConvention callingConvention, ImmutableArray<ParamInfo<TypeSymbol>> retAndParamTypes, bool useUpdatedEscapeRules)
        {
            Debug.Assert(retAndParamTypes.Length > 0);

            ParamInfo<TypeSymbol> retInfo = retAndParamTypes[0];
            var returnType = TypeWithAnnotations.Create(retInfo.Type, customModifiers: CSharpCustomModifier.Convert(retInfo.CustomModifiers));

            RefCustomModifiers = CSharpCustomModifier.Convert(retInfo.RefCustomModifiers);
            CallingConvention = callingConvention;
            ReturnTypeWithAnnotations = returnType;
            RefKind = getRefKind(retInfo, RefCustomModifiers, RefKind.RefReadOnly, RefKind.Ref, requiresLocationAllowed: false);
            Debug.Assert(RefKind != RefKind.Out);
            UseUpdatedEscapeRules = useUpdatedEscapeRules;
            _parameters = makeParametersFromMetadata(retAndParamTypes.AsSpan()[1..], this);

            static ImmutableArray<FunctionPointerParameterSymbol> makeParametersFromMetadata(ReadOnlySpan<ParamInfo<TypeSymbol>> parameterTypes, FunctionPointerMethodSymbol parent)
            {
                if (parameterTypes.Length > 0)
                {
                    var paramsBuilder = ArrayBuilder<FunctionPointerParameterSymbol>.GetInstance(parameterTypes.Length);

                    for (int i = 0; i < parameterTypes.Length; i++)
                    {
                        ParamInfo<TypeSymbol> param = parameterTypes[i];
                        var paramRefCustomMods = CSharpCustomModifier.Convert(param.RefCustomModifiers);
                        var paramType = TypeWithAnnotations.Create(param.Type, customModifiers: CSharpCustomModifier.Convert(param.CustomModifiers));
                        RefKind paramRefKind = getRefKind(param, paramRefCustomMods, RefKind.In, RefKind.Out, requiresLocationAllowed: true);
                        paramsBuilder.Add(new FunctionPointerParameterSymbol(paramType, paramRefKind, i, parent, paramRefCustomMods));
                    }

                    return paramsBuilder.ToImmutableAndFree();
                }
                else
                {
                    return ImmutableArray<FunctionPointerParameterSymbol>.Empty;
                }
            }

            static RefKind getRefKind(ParamInfo<TypeSymbol> param, ImmutableArray<CustomModifier> paramRefCustomMods, RefKind hasInRefKind, RefKind hasOutRefKind, bool requiresLocationAllowed)
            {
                return param.IsByRef switch
                {
                    false => RefKind.None,
                    true when CustomModifierUtils.HasInAttributeModifier(paramRefCustomMods) => hasInRefKind,
                    true when CustomModifierUtils.HasOutAttributeModifier(paramRefCustomMods) => hasOutRefKind,
                    true when requiresLocationAllowed && CustomModifierUtils.HasRequiresLocationAttributeModifier(paramRefCustomMods) => RefKind.RefReadOnlyParameter,
                    true => RefKind.Ref,
                };
            }
        }

        internal void AddNullableTransforms(ArrayBuilder<byte> transforms)
        {
            ReturnTypeWithAnnotations.AddNullableTransforms(transforms);
            foreach (var param in Parameters)
            {
                param.TypeWithAnnotations.AddNullableTransforms(transforms);
            }
        }

        internal FunctionPointerMethodSymbol ApplyNullableTransforms(byte defaultTransformFlag, ImmutableArray<byte> transforms, ref int position)
        {
            bool madeChanges = ReturnTypeWithAnnotations.ApplyNullableTransforms(defaultTransformFlag, transforms, ref position, out var newReturnType);
            var newParamTypes = ImmutableArray<TypeWithAnnotations>.Empty;
            if (!Parameters.IsEmpty)
            {
                var paramTypesBuilder = ArrayBuilder<TypeWithAnnotations>.GetInstance(Parameters.Length);
                bool madeParamChanges = false;
                foreach (var param in Parameters)
                {
                    madeParamChanges |= param.TypeWithAnnotations.ApplyNullableTransforms(defaultTransformFlag, transforms, ref position, out var newParamType);
                    paramTypesBuilder.Add(newParamType);
                }

                if (madeParamChanges)
                {
                    newParamTypes = paramTypesBuilder.ToImmutableAndFree();
                    madeChanges = true;
                }
                else
                {
                    paramTypesBuilder.Free();
                    newParamTypes = ParameterTypesWithAnnotations;
                }
            }

            if (madeChanges)
            {
                return SubstituteParameterSymbols(newReturnType, newParamTypes);
            }
            else
            {
                return this;
            }
        }

        internal override ImmutableArray<NamedTypeSymbol> UnmanagedCallingConventionTypes
        {
            get
            {
                if (!CallingConvention.IsCallingConvention(CallingConvention.Unmanaged))
                {
                    return ImmutableArray<NamedTypeSymbol>.Empty;
                }

                var modifiersToSearch = RefKind != RefKind.None ? RefCustomModifiers : ReturnTypeWithAnnotations.CustomModifiers;
                if (modifiersToSearch.IsEmpty)
                {
                    return ImmutableArray<NamedTypeSymbol>.Empty;
                }

                var builder = ArrayBuilder<NamedTypeSymbol>.GetInstance(modifiersToSearch.Length);
                foreach (CSharpCustomModifier modifier in modifiersToSearch)
                {
                    if (FunctionPointerTypeSymbol.IsCallingConventionModifier(modifier.ModifierSymbol))
                    {
                        builder.Add(modifier.ModifierSymbol);
                    }
                }

                return builder.ToImmutableAndFree();
            }
        }

        public ImmutableHashSet<CustomModifier> GetCallingConventionModifiers()
        {
            if (_lazyCallingConventionModifiers is null)
            {
                var modifiersToSearch = RefKind != RefKind.None ? RefCustomModifiers : ReturnTypeWithAnnotations.CustomModifiers;
                if (modifiersToSearch.IsEmpty || CallingConvention != CallingConvention.Unmanaged)
                {
                    _lazyCallingConventionModifiers = ImmutableHashSet<CustomModifier>.Empty;
                }
                else
                {
                    var builder = PooledHashSet<CustomModifier>.GetInstance();
                    foreach (var modifier in modifiersToSearch)
                    {
                        if (FunctionPointerTypeSymbol.IsCallingConventionModifier(((CSharpCustomModifier)modifier).ModifierSymbol))
                        {
                            builder.Add(modifier);
                        }
                    }

                    if (builder.Count == 0)
                    {
                        _lazyCallingConventionModifiers = ImmutableHashSet<CustomModifier>.Empty;
                    }
                    else
                    {
                        _lazyCallingConventionModifiers = builder.ToImmutableHashSet();
                    }

                    builder.Free();
                }
            }

            return _lazyCallingConventionModifiers;
        }

        public override bool Equals(Symbol other, TypeCompareKind compareKind)
        {
            if (!(other is FunctionPointerMethodSymbol method))
            {
                return false;
            }

            return Equals(method, compareKind);
        }

        internal bool Equals(FunctionPointerMethodSymbol other, TypeCompareKind compareKind)
        {
            return ReferenceEquals(this, other) ||
                (EqualsNoParameters(other, compareKind)
                 && _parameters.SequenceEqual(other._parameters, compareKind,
                     (param1, param2, compareKind) => param1.MethodEqualityChecks(param2, compareKind)));
        }

        private bool EqualsNoParameters(FunctionPointerMethodSymbol other, TypeCompareKind compareKind)
        {
            if (CallingConvention != other.CallingConvention
                || !FunctionPointerTypeSymbol.RefKindEquals(compareKind, RefKind, other.RefKind)
                || !ReturnTypeWithAnnotations.Equals(other.ReturnTypeWithAnnotations, compareKind))
            {
                return false;
            }

            // Calling convention modifiers are considered part of the equality of the function, even if the ignore
            // custom modifiers bit is set. If the bit is not set, then no need to do anything as it will be compared
            // with the rest of the modifiers. Order is significant in metadata, but at the type level ordering/duplication
            // is not significant for these modifiers
            if ((compareKind & TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds) != 0)
            {
                if (CallingConvention.IsCallingConvention(CallingConvention.Unmanaged)
                    && !GetCallingConventionModifiers().SetEqualsWithoutIntermediateHashSet(other.GetCallingConventionModifiers()))
                {
                    return false;
                }
            }
            else if (!RefCustomModifiers.SequenceEqual(other.RefCustomModifiers))
            {
                return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            var currentHash = GetHashCodeNoParameters();
            foreach (var param in _parameters)
            {
                currentHash = Hash.Combine(param.MethodHashCode(), currentHash);
            }
            return currentHash;
        }

        internal int GetHashCodeNoParameters()
            => Hash.Combine(ReturnType, Hash.Combine(((int)CallingConvention).GetHashCode(), ((int)FunctionPointerTypeSymbol.GetRefKindForHashCode(RefKind)).GetHashCode()));

        internal override CallingConvention CallingConvention { get; }
        internal override bool UseUpdatedEscapeRules { get; }

        public override bool ReturnsVoid => ReturnTypeWithAnnotations.IsVoidType();
        public override RefKind RefKind { get; }
        public override TypeWithAnnotations ReturnTypeWithAnnotations { get; }
        public override ImmutableArray<ParameterSymbol> Parameters =>
            _parameters.Cast<FunctionPointerParameterSymbol, ParameterSymbol>();
        public override ImmutableArray<CustomModifier> RefCustomModifiers { get; }
        public override MethodKind MethodKind => MethodKind.FunctionPointerSignature;

        internal override UseSiteInfo<AssemblySymbol> GetUseSiteInfo()
        {
            UseSiteInfo<AssemblySymbol> info = default;
            CalculateUseSiteDiagnostic(ref info);

            if (CallingConvention.IsCallingConvention(CallingConvention.ExtraArguments))
            {
                MergeUseSiteInfo(ref info, new UseSiteInfo<AssemblySymbol>(new CSDiagnosticInfo(ErrorCode.ERR_UnsupportedCallingConvention, this)));
            }

            // Check for `modopt(RequiresLocation)` combined with any required modifier.
            if (info.DiagnosticInfo?.Severity != DiagnosticSeverity.Error)
            {
                foreach (var parameter in this.Parameters)
                {
                    if (CustomModifierUtils.HasRequiresLocationAttributeModifier(parameter.RefCustomModifiers) &&
                        parameter.RefCustomModifiers.Any(static m => !m.IsOptional))
                    {
                        MergeUseSiteInfo(ref info, new UseSiteInfo<AssemblySymbol>(new CSDiagnosticInfo(ErrorCode.ERR_BindToBogus, this)));
                    }
                }
            }

            return info;
        }

        internal bool GetUnificationUseSiteDiagnosticRecursive(ref DiagnosticInfo? result, Symbol owner, ref HashSet<TypeSymbol> checkedTypes)
        {
            return ReturnType.GetUnificationUseSiteDiagnosticRecursive(ref result, owner, ref checkedTypes)
                || GetUnificationUseSiteDiagnosticRecursive(ref result, RefCustomModifiers, owner, ref checkedTypes)
                || GetUnificationUseSiteDiagnosticRecursive(ref result, Parameters, owner, ref checkedTypes);
        }

        public override bool IsVararg
        {
            get
            {
                var isVararg = CallingConvention.IsCallingConvention(CallingConvention.ExtraArguments);
                Debug.Assert(!isVararg || HasUseSiteError);
                return isVararg;
            }
        }

        public override Symbol? ContainingSymbol => null;
        // Function pointers cannot have type parameters
        public override int Arity => 0;
        public override ImmutableArray<TypeParameterSymbol> TypeParameters => ImmutableArray<TypeParameterSymbol>.Empty;
        public override bool IsExtensionMethod => false;
        public override bool HidesBaseMethodsByName => false;
        public override bool IsAsync => false;
        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations => ImmutableArray<MethodSymbol>.Empty;
        public override Symbol? AssociatedSymbol => null;
        public override ImmutableArray<Location> Locations => ImmutableArray<Location>.Empty;
        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => ImmutableArray<SyntaxReference>.Empty;
        public override Accessibility DeclaredAccessibility => Accessibility.NotApplicable;
        public override bool IsStatic => false;
        public override bool IsVirtual => false;
        public override bool IsOverride => false;
        public override bool IsAbstract => false;
        public override bool IsSealed => false;
        public override bool IsExtern => false;
        public override bool IsImplicitlyDeclared => true;
        public override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations => ImmutableArray<TypeWithAnnotations>.Empty;
        internal override bool HasSpecialName => false;
        internal override MethodImplAttributes ImplementationAttributes => default;
        internal override bool HasDeclarativeSecurity => false;
        internal override MarshalPseudoCustomAttributeData? ReturnValueMarshallingInformation => null;
        internal override bool RequiresSecurityObject => false;
        internal override bool IsDeclaredReadOnly => false;
        internal override bool IsInitOnly => false;
        internal override ImmutableArray<string> GetAppliedConditionalSymbols() => ImmutableArray<string>.Empty;
        public override FlowAnalysisAnnotations ReturnTypeFlowAnalysisAnnotations => FlowAnalysisAnnotations.None;
        public override ImmutableHashSet<string> ReturnNotNullIfParameterNotNull => ImmutableHashSet<string>.Empty;
        public override FlowAnalysisAnnotations FlowAnalysisAnnotations => FlowAnalysisAnnotations.None;
        internal override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false) => false;
        internal override bool IsMetadataVirtual(IsMetadataVirtualOption option = IsMetadataVirtualOption.None) => false;
        internal sealed override UnmanagedCallersOnlyAttributeData? GetUnmanagedCallersOnlyAttributeData(bool forceComplete) => null;
        internal sealed override bool HasSpecialNameAttribute => throw ExceptionUtilities.Unreachable();

        internal override bool GenerateDebugInfo => throw ExceptionUtilities.Unreachable();
        internal override ObsoleteAttributeData? ObsoleteAttributeData => throw ExceptionUtilities.Unreachable();

        public override bool AreLocalsZeroed => throw ExceptionUtilities.Unreachable();
        public override DllImportData GetDllImportData() => throw ExceptionUtilities.Unreachable();
        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree) => throw ExceptionUtilities.Unreachable();
        internal override IEnumerable<SecurityAttribute> GetSecurityInformation() => throw ExceptionUtilities.Unreachable();
        internal sealed override bool IsNullableAnalysisEnabled() => throw ExceptionUtilities.Unreachable();
        protected sealed override bool HasSetsRequiredMembersImpl => throw ExceptionUtilities.Unreachable();
        internal sealed override bool HasUnscopedRefAttribute => false;

        internal sealed override CallerUnsafeMode CallerUnsafeMode => CallerUnsafeMode.None;

        internal sealed override bool HasAsyncMethodBuilderAttribute(out TypeSymbol? builderArgument)
        {
            builderArgument = null;
            return false;
        }

        internal sealed override int TryGetOverloadResolutionPriority()
        {
            return 0;
        }
    }
}
