// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE
{
    /// <summary>
    /// The class to represent all generic type parameters imported from a PE/module.
    /// </summary>
    /// <remarks></remarks>
    internal sealed class PETypeParameterSymbol
        : TypeParameterSymbol
    {
        private readonly Symbol _containingSymbol; // Could be PENamedType or a PEMethod
        private readonly GenericParameterHandle _handle;

        #region Metadata
        private readonly string _name;
        private readonly ushort _ordinal; // 0 for first, 1 for second, ...
        #endregion

        /// <summary>
        /// First error calculating bounds.
        /// </summary>
        private CachedUseSiteInfo<AssemblySymbol> _lazyCachedConstraintsUseSiteInfo = CachedUseSiteInfo<AssemblySymbol>.Uninitialized;

        private readonly GenericParameterAttributes _flags;
        private ThreeState _lazyHasIsUnmanagedConstraint;
        private TypeParameterBounds _lazyBounds = TypeParameterBounds.Unset;
        private ImmutableArray<TypeWithAnnotations> _lazyDeclaredConstraintTypes;
        private ImmutableArray<CSharpAttributeData> _lazyCustomAttributes;

        internal PETypeParameterSymbol(
            PEModuleSymbol moduleSymbol,
            PENamedTypeSymbol definingNamedType,
            ushort ordinal,
            GenericParameterHandle handle)
            : this(moduleSymbol, (Symbol)definingNamedType, ordinal, handle)
        {
        }

        internal PETypeParameterSymbol(
            PEModuleSymbol moduleSymbol,
            PEMethodSymbol definingMethod,
            ushort ordinal,
            GenericParameterHandle handle)
            : this(moduleSymbol, (Symbol)definingMethod, ordinal, handle)
        {
        }

        private PETypeParameterSymbol(
            PEModuleSymbol moduleSymbol,
            Symbol definingSymbol,
            ushort ordinal,
            GenericParameterHandle handle)
        {
            Debug.Assert((object)moduleSymbol != null);
            Debug.Assert((object)definingSymbol != null);
            Debug.Assert(ordinal >= 0);
            Debug.Assert(!handle.IsNil);

            _containingSymbol = definingSymbol;

            GenericParameterAttributes flags = 0;

            try
            {
                moduleSymbol.Module.GetGenericParamPropsOrThrow(handle, out _name, out flags);
            }
            catch (BadImageFormatException)
            {
                if ((object)_name == null)
                {
                    _name = string.Empty;
                }

                _lazyCachedConstraintsUseSiteInfo.Initialize(new CSDiagnosticInfo(ErrorCode.ERR_BindToBogus, this));
            }

            // Clear the '.ctor' flag if both '.ctor' and 'valuetype' are
            // set since '.ctor' is redundant in that case.
            _flags = ((flags & GenericParameterAttributes.NotNullableValueTypeConstraint) == 0) ? flags : (flags & ~GenericParameterAttributes.DefaultConstructorConstraint);

            _ordinal = ordinal;
            _handle = handle;
        }

        public override TypeParameterKind TypeParameterKind
        {
            get
            {
                return this.ContainingSymbol.Kind == SymbolKind.Method
                    ? TypeParameterKind.Method
                    : TypeParameterKind.Type;
            }
        }

        public override int Ordinal
        {
            get { return _ordinal; }
        }

        public override string Name
        {
            get
            {
                return _name;
            }
        }

        public override int MetadataToken
        {
            get { return MetadataTokens.GetToken(_handle); }
        }

        internal GenericParameterHandle Handle
        {
            get
            {
                return _handle;
            }
        }

        public override Symbol ContainingSymbol
        {
            get { return _containingSymbol; }
        }

        public override AssemblySymbol ContainingAssembly
        {
            get
            {
                return _containingSymbol.ContainingAssembly;
            }
        }

        private ImmutableArray<TypeWithAnnotations> GetDeclaredConstraintTypes(ConsList<PETypeParameterSymbol> inProgress)
        {
            Debug.Assert(!inProgress.ContainsReference(this));
            Debug.Assert(!inProgress.Any() || ReferenceEquals(inProgress.Head.ContainingSymbol, this.ContainingSymbol));

            if (RoslynImmutableInterlocked.VolatileRead(ref _lazyDeclaredConstraintTypes).IsDefault)
            {
                ImmutableArray<TypeWithAnnotations> declaredConstraintTypes;

                var moduleSymbol = ((PEModuleSymbol)this.ContainingModule);
                PEModule peModule = moduleSymbol.Module;
                GenericParameterConstraintHandleCollection constraints = GetConstraintHandleCollection(peModule);

                bool hasUnmanagedModreqPattern = false;

                if (constraints.Count > 0)
                {
                    var symbolsBuilder = ArrayBuilder<TypeWithAnnotations>.GetInstance();
                    MetadataDecoder tokenDecoder = GetDecoder(moduleSymbol);

                    TypeWithAnnotations bestObjectConstraint = default;

                    var metadataReader = peModule.MetadataReader;
                    foreach (var constraintHandle in constraints)
                    {
                        TypeWithAnnotations type = GetConstraintTypeOrDefault(moduleSymbol, metadataReader, tokenDecoder, constraintHandle, ref hasUnmanagedModreqPattern);

                        if (!type.HasType)
                        {
                            // Dropped 'System.ValueType' constraint type when the 'valuetype' constraint was also specified.
                            continue;
                        }

                        // Drop 'System.Object' constraint type.
                        if (ConstraintsHelper.IsObjectConstraint(type, ref bestObjectConstraint))
                        {
                            continue;
                        }

                        symbolsBuilder.Add(type);
                    }

                    if (bestObjectConstraint.HasType)
                    {
                        // See if we need to put Object! or Object~ back in order to preserve nullability information for the type parameter.
                        if (ConstraintsHelper.IsObjectConstraintSignificant(CalculateIsNotNullableFromNonTypeConstraints(), bestObjectConstraint))
                        {
                            Debug.Assert(!HasNotNullConstraint && !HasValueTypeConstraint);
                            if (symbolsBuilder.Count == 0)
                            {
                                if (bestObjectConstraint.NullableAnnotation.IsOblivious() && !HasReferenceTypeConstraint)
                                {
                                    bestObjectConstraint = default;
                                }
                            }
                            else
                            {
                                inProgress = inProgress.Prepend(this);
                                foreach (TypeWithAnnotations constraintType in symbolsBuilder)
                                {
                                    if (!ConstraintsHelper.IsObjectConstraintSignificant(IsNotNullableFromConstraintType(constraintType, inProgress, out _), bestObjectConstraint))
                                    {
                                        bestObjectConstraint = default;
                                        break;
                                    }
                                }
                            }

                            if (bestObjectConstraint.HasType)
                            {
                                symbolsBuilder.Insert(0, bestObjectConstraint);
                            }
                        }
                    }

                    declaredConstraintTypes = symbolsBuilder.ToImmutableAndFree();
                }
                else
                {
                    declaredConstraintTypes = ImmutableArray<TypeWithAnnotations>.Empty;
                }

                // - presence of unmanaged pattern has to be matched with `valuetype`
                // - IsUnmanagedAttribute is allowed if there is an unmanaged pattern
                if (hasUnmanagedModreqPattern && (_flags & GenericParameterAttributes.NotNullableValueTypeConstraint) == 0 ||
                    hasUnmanagedModreqPattern != peModule.HasIsUnmanagedAttribute(_handle))
                {
                    // we do not recognize these combinations as "unmanaged"
                    hasUnmanagedModreqPattern = false;
                    _lazyCachedConstraintsUseSiteInfo.InterlockedInitializeFromSentinel(primaryDependency: null, new UseSiteInfo<AssemblySymbol>(new CSDiagnosticInfo(ErrorCode.ERR_BindToBogus, this)));
                }

                _lazyHasIsUnmanagedConstraint = hasUnmanagedModreqPattern.ToThreeState();
                ImmutableInterlocked.InterlockedInitialize(ref _lazyDeclaredConstraintTypes, declaredConstraintTypes);
            }

            return _lazyDeclaredConstraintTypes;
        }

        private MetadataDecoder GetDecoder(PEModuleSymbol moduleSymbol)
        {
            MetadataDecoder tokenDecoder;
            if (_containingSymbol.Kind == SymbolKind.Method)
            {
                tokenDecoder = new MetadataDecoder(moduleSymbol, (PEMethodSymbol)_containingSymbol);
            }
            else
            {
                tokenDecoder = new MetadataDecoder(moduleSymbol, (PENamedTypeSymbol)_containingSymbol);
            }

            return tokenDecoder;
        }

        private TypeWithAnnotations GetConstraintTypeOrDefault(PEModuleSymbol moduleSymbol, MetadataReader metadataReader, MetadataDecoder tokenDecoder, GenericParameterConstraintHandle constraintHandle, ref bool hasUnmanagedModreqPattern)
        {
            var constraint = metadataReader.GetGenericParameterConstraint(constraintHandle);
            var typeSymbol = tokenDecoder.DecodeGenericParameterConstraint(constraint.Type, out ImmutableArray<ModifierInfo<TypeSymbol>> modifiers);

            if (!modifiers.IsDefaultOrEmpty && modifiers.Length > 1)
            {
                typeSymbol = new UnsupportedMetadataTypeSymbol();
            }
            else if (typeSymbol.SpecialType == SpecialType.System_ValueType)
            {
                // recognize "(class [mscorlib]System.ValueType modreq([mscorlib]System.Runtime.InteropServices.UnmanagedType" pattern as "unmanaged"
                if (!modifiers.IsDefaultOrEmpty)
                {
                    ModifierInfo<TypeSymbol> m = modifiers.Single();
                    if (!m.IsOptional && m.Modifier.IsWellKnownTypeUnmanagedType())
                    {
                        hasUnmanagedModreqPattern = true;
                    }
                    else
                    {
                        // Any other modifiers, optional or not, are not allowed: http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528856
                        typeSymbol = new UnsupportedMetadataTypeSymbol();
                    }
                }

                // Drop 'System.ValueType' constraint type if the 'valuetype' constraint was also specified.
                if (typeSymbol.SpecialType == SpecialType.System_ValueType && ((_flags & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0))
                {
                    return default;
                }
            }
            else if (!modifiers.IsDefaultOrEmpty)
            {
                // Other modifiers, optional or not, are not allowed: http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528856
                typeSymbol = new UnsupportedMetadataTypeSymbol();
            }

            var type = TypeWithAnnotations.Create(typeSymbol);
            type = NullableTypeDecoder.TransformType(type, constraintHandle, moduleSymbol, accessSymbol: _containingSymbol, nullableContext: _containingSymbol);
            type = TupleTypeDecoder.DecodeTupleTypesIfApplicable(type, constraintHandle, moduleSymbol);
            return type;
        }

        private static bool? IsNotNullableFromConstraintType(TypeWithAnnotations constraintType, ConsList<PETypeParameterSymbol> inProgress, out bool isNonNullableValueType)
        {
            if (!(constraintType.Type is PETypeParameterSymbol typeParameter) ||
                (object)typeParameter.ContainingSymbol != inProgress.Head.ContainingSymbol ||
                typeParameter.GetConstraintHandleCollection().Count == 0)
            {
                return IsNotNullableFromConstraintType(constraintType, out isNonNullableValueType);
            }

            bool? isNotNullable = typeParameter.CalculateIsNotNullable(inProgress, out isNonNullableValueType);

            if (isNonNullableValueType)
            {
                Debug.Assert(isNotNullable == true);
                return true;
            }

            if (constraintType.NullableAnnotation.IsAnnotated() || isNotNullable == false)
            {
                return false;
            }
            else if (constraintType.NullableAnnotation.IsOblivious() || isNotNullable == null)
            {
                return null;
            }

            return true;
        }

        private bool? CalculateIsNotNullable(ConsList<PETypeParameterSymbol> inProgress, out bool isNonNullableValueType)
        {
            if (inProgress.ContainsReference(this))
            {
                isNonNullableValueType = false;
                return false;
            }

            if (this.HasValueTypeConstraint)
            {
                isNonNullableValueType = true;
                return true;
            }

            bool? fromNonTypeConstraints = CalculateIsNotNullableFromNonTypeConstraints();

            ImmutableArray<TypeWithAnnotations> constraintTypes = this.GetDeclaredConstraintTypes(inProgress);

            if (constraintTypes.IsEmpty)
            {
                isNonNullableValueType = false;
                return fromNonTypeConstraints;
            }

            bool? fromTypes = IsNotNullableFromConstraintTypes(constraintTypes, inProgress, out isNonNullableValueType);

            if (isNonNullableValueType)
            {
                Debug.Assert(fromTypes == true);
                return true;
            }

            if (fromTypes == true || fromNonTypeConstraints == false)
            {
                return fromTypes;
            }

            Debug.Assert(fromNonTypeConstraints == null || fromNonTypeConstraints == true);
            Debug.Assert(fromTypes != true);
            return fromNonTypeConstraints;
        }

        private static bool? IsNotNullableFromConstraintTypes(ImmutableArray<TypeWithAnnotations> constraintTypes, ConsList<PETypeParameterSymbol> inProgress, out bool isNonNullableValueType)
        {
            Debug.Assert(!constraintTypes.IsDefaultOrEmpty);

            isNonNullableValueType = false;
            bool? result = false;
            foreach (TypeWithAnnotations constraintType in constraintTypes)
            {
                bool? fromType = IsNotNullableFromConstraintType(constraintType, inProgress, out isNonNullableValueType);

                if (isNonNullableValueType)
                {
                    Debug.Assert(fromType == true);
                    return true;
                }

                if (fromType == true)
                {
                    result = true;
                }
                else if (fromType == null && result == false)
                {
                    result = null;
                }
            }

            return result;
        }

        private GenericParameterConstraintHandleCollection GetConstraintHandleCollection(PEModule module)
        {
            GenericParameterConstraintHandleCollection constraints;

            try
            {
                constraints = module.MetadataReader.GetGenericParameter(_handle).GetConstraints();
            }
            catch (BadImageFormatException)
            {
                constraints = default(GenericParameterConstraintHandleCollection);
                _lazyCachedConstraintsUseSiteInfo.InterlockedInitializeFromSentinel(primaryDependency: null, new UseSiteInfo<AssemblySymbol>(new CSDiagnosticInfo(ErrorCode.ERR_BindToBogus, this)));
            }

            return constraints;
        }

        private GenericParameterConstraintHandleCollection GetConstraintHandleCollection()
        {
            return GetConstraintHandleCollection(((PEModuleSymbol)this.ContainingModule).Module);
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return _containingSymbol.Locations;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return ImmutableArray<SyntaxReference>.Empty;
            }
        }

        public override bool HasConstructorConstraint
        {
            get
            {
                return (_flags & GenericParameterAttributes.DefaultConstructorConstraint) != 0;
            }
        }

        public override bool HasReferenceTypeConstraint
        {
            get
            {
                return (_flags & GenericParameterAttributes.ReferenceTypeConstraint) != 0;
            }
        }

        public override bool IsReferenceTypeFromConstraintTypes
        {
            get
            {
                return CalculateIsReferenceTypeFromConstraintTypes(ConstraintTypesNoUseSiteDiagnostics);
            }
        }

        /// <summary>
        /// Returns the byte value from the (single byte) NullableAttribute or nearest
        /// NullableContextAttribute. Returns 0 if neither attribute is specified.
        /// </summary>
        private byte GetNullableAttributeValue()
        {
            if (((PEModuleSymbol)this.ContainingModule).Module.HasNullableAttribute(_handle, out byte value, out _))
            {
                return value;
            }
            return _containingSymbol.GetNullableContextValue() ?? 0;
        }

        internal override bool? ReferenceTypeConstraintIsNullable
        {
            get
            {
                if (!HasReferenceTypeConstraint)
                {
                    return false;
                }

                switch (GetNullableAttributeValue())
                {
                    case NullableAnnotationExtensions.AnnotatedAttributeValue:
                        return true;
                    case NullableAnnotationExtensions.NotAnnotatedAttributeValue:
                        return false;
                }

                return null;
            }
        }

        public override bool HasNotNullConstraint
        {
            get
            {
                return (_flags & (GenericParameterAttributes.NotNullableValueTypeConstraint | GenericParameterAttributes.ReferenceTypeConstraint)) == 0 &&
                       GetNullableAttributeValue() == NullableAnnotationExtensions.NotAnnotatedAttributeValue;
            }
        }

        internal override bool? IsNotNullable
        {
            get
            {
                if ((_flags & (GenericParameterAttributes.NotNullableValueTypeConstraint | GenericParameterAttributes.ReferenceTypeConstraint)) == 0 &&
                    !HasNotNullConstraint)
                {
                    var moduleSymbol = ((PEModuleSymbol)this.ContainingModule);
                    PEModule module = moduleSymbol.Module;
                    GenericParameterConstraintHandleCollection constraints = GetConstraintHandleCollection(module);

                    if (constraints.Count == 0)
                    {
                        if (GetNullableAttributeValue() == NullableAnnotationExtensions.AnnotatedAttributeValue)
                        {
                            return false;
                        }

                        return null;
                    }
                    else if (GetDeclaredConstraintTypes(ConsList<PETypeParameterSymbol>.Empty).IsEmpty)
                    {
                        // We must have filtered out some Object constraints, lets calculate nullability from them.
                        var symbolsBuilder = ArrayBuilder<TypeWithAnnotations>.GetInstance();
                        MetadataDecoder tokenDecoder = GetDecoder(moduleSymbol);

                        bool hasUnmanagedModreqPattern = false;
                        var metadataReader = module.MetadataReader;
                        foreach (var constraintHandle in constraints)
                        {
                            TypeWithAnnotations type = GetConstraintTypeOrDefault(moduleSymbol, metadataReader, tokenDecoder, constraintHandle, ref hasUnmanagedModreqPattern);

                            Debug.Assert(type.HasType && type.SpecialType == SpecialType.System_Object);
                            if (!type.HasType)
                            {
                                continue;
                            }

                            symbolsBuilder.Add(type);
                        }

                        return IsNotNullableFromConstraintTypes(symbolsBuilder.ToImmutableAndFree());
                    }
                }

                return CalculateIsNotNullable();
            }
        }

        public override bool HasValueTypeConstraint
        {
            get
            {
                return (_flags & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0;
            }
        }

        public override bool AllowsRefLikeType
        {
            get
            {
                return (_flags & GenericParameterAttributes.AllowByRefLike) != 0;
            }
        }

        public override bool IsValueTypeFromConstraintTypes
        {
            get
            {
                Debug.Assert(!HasValueTypeConstraint);
                return CalculateIsValueTypeFromConstraintTypes(ConstraintTypesNoUseSiteDiagnostics);
            }
        }

        public override bool HasUnmanagedTypeConstraint
        {
            get
            {
                GetDeclaredConstraintTypes(ConsList<PETypeParameterSymbol>.Empty);
                return this._lazyHasIsUnmanagedConstraint.Value();
            }
        }

        public override VarianceKind Variance
        {
            get
            {
                return (VarianceKind)(_flags & GenericParameterAttributes.VarianceMask);
            }
        }

        internal override void EnsureAllConstraintsAreResolved()
        {
            if (!_lazyBounds.IsSet())
            {
                var typeParameters = (_containingSymbol.Kind == SymbolKind.Method) ?
                    ((PEMethodSymbol)_containingSymbol).TypeParameters :
                    ((PENamedTypeSymbol)_containingSymbol).TypeParameters;
                EnsureAllConstraintsAreResolved(typeParameters);
            }
        }

        internal override ImmutableArray<TypeWithAnnotations> GetConstraintTypes(ConsList<TypeParameterSymbol> inProgress)
        {
            var bounds = this.GetBounds(inProgress);
            return (bounds != null) ? bounds.ConstraintTypes : ImmutableArray<TypeWithAnnotations>.Empty;
        }

        internal override ImmutableArray<NamedTypeSymbol> GetInterfaces(ConsList<TypeParameterSymbol> inProgress)
        {
            var bounds = this.GetBounds(inProgress);
            return (bounds != null) ? bounds.Interfaces : ImmutableArray<NamedTypeSymbol>.Empty;
        }

        internal override NamedTypeSymbol GetEffectiveBaseClass(ConsList<TypeParameterSymbol> inProgress)
        {
            var bounds = this.GetBounds(inProgress);
            return (bounds != null) ? bounds.EffectiveBaseClass : this.GetDefaultBaseType();
        }

        internal override TypeSymbol GetDeducedBaseType(ConsList<TypeParameterSymbol> inProgress)
        {
            var bounds = this.GetBounds(inProgress);
            return (bounds != null) ? bounds.DeducedBaseType : this.GetDefaultBaseType();
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            if (_lazyCustomAttributes.IsDefault)
            {
                var containingPEModuleSymbol = (PEModuleSymbol)this.ContainingModule;

                var loadedCustomAttributes = containingPEModuleSymbol.GetCustomAttributesForToken(
                    Handle,
                    out _,
                    // Filter out [IsUnmanagedAttribute]
                    HasUnmanagedTypeConstraint ? AttributeDescription.IsUnmanagedAttribute : default);

                ImmutableInterlocked.InterlockedInitialize(ref _lazyCustomAttributes, loadedCustomAttributes);
            }

            return _lazyCustomAttributes;
        }

        private TypeParameterBounds GetBounds(ConsList<TypeParameterSymbol> inProgress)
        {
            Debug.Assert(!inProgress.ContainsReference(this));
            Debug.Assert(!inProgress.Any() || ReferenceEquals(inProgress.Head.ContainingSymbol, this.ContainingSymbol));

            if (_lazyBounds == TypeParameterBounds.Unset)
            {
                var constraintTypes = GetDeclaredConstraintTypes(ConsList<PETypeParameterSymbol>.Empty);
                Debug.Assert(!constraintTypes.IsDefault);

                var diagnostics = ArrayBuilder<TypeParameterDiagnosticInfo>.GetInstance();
                ArrayBuilder<TypeParameterDiagnosticInfo> useSiteDiagnosticsBuilder = null;
                bool inherited = (_containingSymbol.Kind == SymbolKind.Method) && ((MethodSymbol)_containingSymbol).IsOverride;
                var bounds = this.ResolveBounds(this.ContainingAssembly.CorLibrary, inProgress.Prepend(this), constraintTypes, inherited, currentCompilation: null,
                                                diagnosticsBuilder: diagnostics, useSiteDiagnosticsBuilder: ref useSiteDiagnosticsBuilder, template: default);

                if (useSiteDiagnosticsBuilder != null)
                {
                    diagnostics.AddRange(useSiteDiagnosticsBuilder);
                }

                AssemblySymbol primaryDependency = PrimaryDependency;
                var useSiteInfo = new UseSiteInfo<AssemblySymbol>(primaryDependency);

                foreach (var diag in diagnostics)
                {
                    MergeUseSiteInfo(ref useSiteInfo, diag.UseSiteInfo);
                    if (useSiteInfo.DiagnosticInfo?.Severity == DiagnosticSeverity.Error)
                    {
                        break;
                    }
                }

                diagnostics.Free();

                _lazyCachedConstraintsUseSiteInfo.InterlockedInitializeFromSentinel(primaryDependency, useSiteInfo);
                Interlocked.CompareExchange(ref _lazyBounds, bounds, TypeParameterBounds.Unset);
            }

            Debug.Assert(_lazyCachedConstraintsUseSiteInfo.IsInitialized);
            return _lazyBounds;
        }

        internal override UseSiteInfo<AssemblySymbol> GetConstraintsUseSiteErrorInfo()
        {
            EnsureAllConstraintsAreResolved();
            Debug.Assert(_lazyCachedConstraintsUseSiteInfo.IsInitialized);
            return _lazyCachedConstraintsUseSiteInfo.ToUseSiteInfo(PrimaryDependency);
        }

        private NamedTypeSymbol GetDefaultBaseType()
        {
            return this.ContainingAssembly.GetSpecialType(SpecialType.System_Object);
        }

        internal sealed override CSharpCompilation DeclaringCompilation // perf, not correctness
        {
            get { return null; }
        }

#nullable enable
        internal DiagnosticInfo? DeriveCompilerFeatureRequiredDiagnostic(MetadataDecoder decoder)
            => PEUtilities.DeriveCompilerFeatureRequiredAttributeDiagnostic(this, (PEModuleSymbol)ContainingModule, Handle, CompilerFeatureRequiredFeatures.None, decoder);

        public override bool HasUnsupportedMetadata
        {
            get
            {
                var containingModule = (PEModuleSymbol)ContainingModule;
                return DeriveCompilerFeatureRequiredDiagnostic(GetDecoder(containingModule)) is { Code: (int)ErrorCode.ERR_UnsupportedCompilerFeature } || base.HasUnsupportedMetadata;
            }
        }
    }
}
