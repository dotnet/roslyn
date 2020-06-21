// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#nullable enable

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

        public static FunctionPointerMethodSymbol CreateFromSource(FunctionPointerTypeSyntax syntax, Binder typeBinder, DiagnosticBag diagnostics, ConsList<TypeSymbol> basesBeingResolved, bool suppressUseSiteDiagnostics)
        {
            var (callingConvention, conventionIsValid) = FunctionPointerTypeSymbol.GetCallingConvention(syntax.CallingConvention.Text);
            if (!conventionIsValid)
            {
                // '{0}' is not a valid calling convention for a function pointer. Valid conventions are 'cdecl', 'managed', 'thiscall', and 'stdcall'.
                diagnostics.Add(ErrorCode.ERR_InvalidFunctionPointerCallingConvention, syntax.CallingConvention.GetLocation(), syntax.CallingConvention.Text);
            }

            RefKind refKind = RefKind.None;
            TypeWithAnnotations returnType;
            var refReadonlyModifiers = ImmutableArray<CustomModifier>.Empty;

            if (syntax.Parameters.Count == 0)
            {
                returnType = TypeWithAnnotations.Create(typeBinder.CreateErrorType());
            }
            else
            {
                var returnTypeParameter = syntax.Parameters[^1];
                var modifiers = returnTypeParameter.Modifiers;
                for (int i = 0; i < modifiers.Count; i++)
                {
                    var modifier = modifiers[i];
                    switch (modifier.Kind())
                    {
                        case SyntaxKind.RefKeyword when refKind == RefKind.None:
                            if (modifiers.Count > i + 1 && modifiers[i + 1].Kind() == SyntaxKind.ReadOnlyKeyword)
                            {
                                i++;
                                refKind = RefKind.RefReadOnly;
                                refReadonlyModifiers = ParameterHelpers.CreateInModifiers(typeBinder, diagnostics, returnTypeParameter);
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
                    diagnostics.Add(ErrorCode.ERR_ReturnTypeIsStaticClass, returnTypeParameter.Location, returnType);
                }
                else if (returnType.IsRestrictedType(ignoreSpanLikeTypes: true))
                {
                    diagnostics.Add(ErrorCode.ERR_MethodReturnCantBeRefAny, returnTypeParameter.Location, returnType);
                }
            }

            return new FunctionPointerMethodSymbol(
                callingConvention,
                refKind,
                returnType,
                refReadonlyModifiers,
                syntax,
                typeBinder,
                diagnostics,
                suppressUseSiteDiagnostics);
        }

        /// <summary>
        /// Creates a function pointer method symbol from individual parts. This method should only be used when diagnostics are not needed.
        /// </summary>
        internal static FunctionPointerMethodSymbol CreateFromParts(
            TypeWithAnnotations returnType,
            RefKind returnRefKind,
            ImmutableArray<TypeWithAnnotations> parameterTypes,
            ImmutableArray<RefKind> parameterRefKinds,
            CSharpCompilation compilation)
        {
            return new FunctionPointerMethodSymbol(
                CallingConvention.Default,
                returnRefKind,
                returnType,
                parameterTypes,
                parameterRefKinds,
                compilation);
        }

        public static FunctionPointerMethodSymbol CreateFromMetadata(CallingConvention callingConvention, ImmutableArray<ParamInfo<TypeSymbol>> retAndParamTypes)
            => new FunctionPointerMethodSymbol(callingConvention, retAndParamTypes);

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
                paramRefCustomModifiers);

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
            ImmutableArray<ImmutableArray<CustomModifier>> substitutedRefCustomModifiers)
        {
            Debug.Assert(originalParameters.Length == substitutedParameterTypes.Length);
            Debug.Assert(substitutedRefCustomModifiers.IsDefault || originalParameters.Length == substitutedRefCustomModifiers.Length);
            RefCustomModifiers = refCustomModifiers;
            CallingConvention = callingConvention;
            RefKind = refKind;
            ReturnTypeWithAnnotations = returnType;

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
            ImmutableArray<TypeWithAnnotations> parameterTypes,
            ImmutableArray<RefKind> parameterRefKinds,
            CSharpCompilation compilation)
        {
            Debug.Assert(refKind != RefKind.Out);
            RefCustomModifiers = getCustomModifierForRefKind(refKind, compilation);
            RefKind = refKind;
            CallingConvention = callingConvention;
            ReturnTypeWithAnnotations = returnTypeWithAnnotations;
            _parameters = parameterTypes.ZipAsArray(parameterRefKinds, (Method: this, Comp: compilation), (type, refKind, i, arg) =>
                new FunctionPointerParameterSymbol(type, refKind, i, arg.Method, refCustomModifiers: getCustomModifierForRefKind(refKind, arg.Comp)));

            static ImmutableArray<CustomModifier> getCustomModifierForRefKind(RefKind refKind, CSharpCompilation compilation)
            {
                var attributeType = refKind switch
                {
                    RefKind.In => compilation.GetWellKnownType(WellKnownType.System_Runtime_InteropServices_InAttribute),
                    RefKind.Out => compilation.GetWellKnownType(WellKnownType.System_Runtime_InteropServices_OutAttribute),
                    _ => null
                };

                if (attributeType is null)
                {
                    Debug.Assert(refKind != RefKind.Out && refKind != RefKind.In);
                    return ImmutableArray<CustomModifier>.Empty;
                }

                return ImmutableArray.Create(CSharpCustomModifier.CreateRequired(attributeType));
            }
        }

        private FunctionPointerMethodSymbol(
            CallingConvention callingConvention,
            RefKind refKind,
            TypeWithAnnotations returnType,
            ImmutableArray<CustomModifier> refCustomModifiers,
            FunctionPointerTypeSyntax syntax,
            Binder typeBinder,
            DiagnosticBag diagnostics,
            bool suppressUseSiteDiagnostics)
        {
            RefCustomModifiers = refCustomModifiers;
            CallingConvention = callingConvention;
            RefKind = refKind;
            ReturnTypeWithAnnotations = returnType;

            _parameters = syntax.Parameters.Count > 1
                ? ParameterHelpers.MakeFunctionPointerParameters(
                    typeBinder,
                    this,
                    syntax.Parameters,
                    diagnostics,
                    suppressUseSiteDiagnostics)
                : ImmutableArray<FunctionPointerParameterSymbol>.Empty;
        }

        private FunctionPointerMethodSymbol(CallingConvention callingConvention, ImmutableArray<ParamInfo<TypeSymbol>> retAndParamTypes)
        {
            Debug.Assert(retAndParamTypes.Length > 0);

            ParamInfo<TypeSymbol> retInfo = retAndParamTypes[0];
            var returnType = TypeWithAnnotations.Create(retInfo.Type, customModifiers: CSharpCustomModifier.Convert(retInfo.CustomModifiers));

            RefCustomModifiers = CSharpCustomModifier.Convert(retInfo.RefCustomModifiers);
            CallingConvention = callingConvention;
            ReturnTypeWithAnnotations = returnType;
            RefKind = getRefKind(retInfo, RefCustomModifiers, RefKind.RefReadOnly, RefKind.Ref);
            Debug.Assert(RefKind != RefKind.Out);
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
                        RefKind paramRefKind = getRefKind(param, paramRefCustomMods, RefKind.In, RefKind.Out);
                        paramsBuilder.Add(new FunctionPointerParameterSymbol(paramType, paramRefKind, i, parent, paramRefCustomMods));
                    }

                    return paramsBuilder.ToImmutableAndFree();
                }
                else
                {
                    return ImmutableArray<FunctionPointerParameterSymbol>.Empty;
                }
            }

            static RefKind getRefKind(ParamInfo<TypeSymbol> param, ImmutableArray<CustomModifier> paramRefCustomMods, RefKind hasInRefKind, RefKind hasOutRefKind)
            {
                return param.IsByRef switch
                {
                    false => RefKind.None,
                    true when CustomModifierUtils.HasInAttributeModifier(paramRefCustomMods) => hasInRefKind,
                    true when CustomModifierUtils.HasOutAttributeModifier(paramRefCustomMods) => hasOutRefKind,
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

        public override bool Equals(Symbol other, TypeCompareKind compareKind)
        {
            if (!(other is FunctionPointerMethodSymbol method))
            {
                return false;
            }

            return Equals(method, compareKind, isValueTypeOverride: null);
        }

        internal bool Equals(FunctionPointerMethodSymbol other, TypeCompareKind compareKind, IReadOnlyDictionary<TypeParameterSymbol, bool>? isValueTypeOverride)
        {
            return ReferenceEquals(this, other) ||
                (EqualsNoParameters(other, compareKind, isValueTypeOverride)
                 && _parameters.SequenceEqual(other._parameters, (compareKind, isValueTypeOverride),
                     (param1, param2, args) => param1.MethodEqualityChecks(param2, args.compareKind, args.isValueTypeOverride)));
        }

        private bool EqualsNoParameters(FunctionPointerMethodSymbol other, TypeCompareKind compareKind, IReadOnlyDictionary<TypeParameterSymbol, bool>? isValueTypeOverride)
            => CallingConvention == other.CallingConvention
               && FunctionPointerTypeSymbol.RefKindEquals(compareKind, RefKind, other.RefKind)
               && ((compareKind & TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds) != 0
                    || RefCustomModifiers.SequenceEqual(other.RefCustomModifiers))
               && ReturnTypeWithAnnotations.Equals(other.ReturnTypeWithAnnotations, compareKind, isValueTypeOverride);

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
            => Hash.Combine(ReturnType, Hash.Combine(CallingConvention.GetHashCode(), FunctionPointerTypeSymbol.GetRefKindForHashCode(RefKind).GetHashCode()));

        internal override CallingConvention CallingConvention { get; }
        public override bool ReturnsVoid => ReturnTypeWithAnnotations.IsVoidType();
        public override RefKind RefKind { get; }
        public override TypeWithAnnotations ReturnTypeWithAnnotations { get; }
        public override ImmutableArray<ParameterSymbol> Parameters =>
            _parameters.Cast<FunctionPointerParameterSymbol, ParameterSymbol>();
        public override ImmutableArray<CustomModifier> RefCustomModifiers { get; }
        public override MethodKind MethodKind => MethodKind.FunctionPointerSignature;

        internal override DiagnosticInfo? GetUseSiteDiagnostic()
        {
            DiagnosticInfo? info = null;
            CalculateUseSiteDiagnostic(ref info);

            if (CallingConvention.IsCallingConvention(CallingConvention.ExtraArguments) ||
                CallingConvention.IsCallingConvention(CallingConvention.FastCall))
            {
                MergeUseSiteDiagnostics(ref info, new CSDiagnosticInfo(ErrorCode.ERR_UnsupportedCallingConvention, this));
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
        internal override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false) => false;

        internal override bool GenerateDebugInfo => throw ExceptionUtilities.Unreachable;
        internal override ObsoleteAttributeData? ObsoleteAttributeData => throw ExceptionUtilities.Unreachable;

        public override bool AreLocalsZeroed => throw ExceptionUtilities.Unreachable;
        public override DllImportData GetDllImportData() => throw ExceptionUtilities.Unreachable;
        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree) => throw ExceptionUtilities.Unreachable;
        internal override IEnumerable<SecurityAttribute> GetSecurityInformation() => throw ExceptionUtilities.Unreachable;
    }
}
