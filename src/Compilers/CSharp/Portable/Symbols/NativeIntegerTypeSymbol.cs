// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// This wrapper is only used on platforms where System.IntPtr isn't considered
    /// a numeric type (as indicated by a RuntimeFeature flag).
    /// </summary>
    internal sealed class NativeIntegerTypeSymbol : WrappedNamedTypeSymbol
#if !DEBUG
        , Cci.IReference
#endif
    {
        private ImmutableArray<NamedTypeSymbol> _lazyInterfaces;
        private ImmutableArray<Symbol> _lazyMembers;
        private NativeIntegerTypeMap? _lazyTypeMap;

        internal NativeIntegerTypeSymbol(NamedTypeSymbol underlyingType) : base(underlyingType, tupleData: null)
        {
            Debug.Assert(underlyingType.TupleData is null);
            Debug.Assert(!underlyingType.IsNativeIntegerType);
            Debug.Assert(!underlyingType.IsExtension);
            Debug.Assert(underlyingType.SpecialType == SpecialType.System_IntPtr || underlyingType.SpecialType == SpecialType.System_UIntPtr);
            Debug.Assert(!underlyingType.ContainingAssembly.RuntimeSupportsNumericIntPtr);
            VerifyEquality(this, underlyingType);
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters => ImmutableArray<TypeParameterSymbol>.Empty;

        public override NamedTypeSymbol ConstructedFrom => this;

        public override Symbol ContainingSymbol => _underlyingType.ContainingSymbol;

        internal override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotationsNoUseSiteDiagnostics => ImmutableArray<TypeWithAnnotations>.Empty;

        internal override bool IsComImport => _underlyingType.IsComImport;

        internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics => _underlyingType.BaseTypeNoUseSiteDiagnostics;

        public override ExtendedSpecialType ExtendedSpecialType => _underlyingType.ExtendedSpecialType;

        public override IEnumerable<string> MemberNames => GetMembers().Select(m => m.Name);

        internal override bool HasDeclaredRequiredMembers => false;

        /// <summary>
        /// Certain members from the underlying types are not exposed from the native integer types:
        ///   constructors other than the default parameterless constructor are not supported;
        ///   operators are handled explicitly as built-in operators and conversions;
        ///   0 should be used instead of Zero;
        ///   sizeof() should be used instead of Size;
        ///   + and - should be used instead of Add() and Subtract();
        ///   ToInt32(), ToInt64(), ToPointer() should be used from underlying types only.
        /// The remaining members are exposed on the native integer types with appropriate
        /// substitution of underlying types in the signatures.
        /// Specifically, we expose public, non-generic instance and static methods and properties
        /// other than those named above.
        /// </summary>
        public override ImmutableArray<Symbol> GetMembers()
        {
            if (_lazyMembers.IsDefault)
            {
                ImmutableInterlocked.InterlockedInitialize(ref _lazyMembers, makeMembers(_underlyingType.GetMembers()));
            }
            return _lazyMembers;

            ImmutableArray<Symbol> makeMembers(ImmutableArray<Symbol> underlyingMembers)
            {
                var builder = ArrayBuilder<Symbol>.GetInstance();
                foreach (var underlyingMember in underlyingMembers)
                {
                    Debug.Assert(_underlyingType.Equals(underlyingMember.ContainingSymbol));
                    if (underlyingMember.DeclaredAccessibility != Accessibility.Public)
                    {
                        continue;
                    }
                    switch (underlyingMember)
                    {
                        case MethodSymbol underlyingMethod:
                            if (underlyingMethod.IsGenericMethod || underlyingMethod.IsAccessor())
                            {
                                break;
                            }
                            switch (underlyingMethod.MethodKind)
                            {
                                case MethodKind.Ordinary:
                                    switch (underlyingMethod.Name)
                                    {
                                        case "Add":
                                        case "Subtract":
                                        case "ToInt32":
                                        case "ToInt64":
                                        case "ToUInt32":
                                        case "ToUInt64":
                                        case "ToPointer":
                                            break;
                                        default:
                                            builder.Add(new NativeIntegerMethodSymbol(this, underlyingMethod, associatedSymbol: null));
                                            break;
                                    }
                                    break;

                                case MethodKind.Constructor:
                                    if (underlyingMethod.ParameterCount == 0)
                                    {
                                        builder.Add(new NativeIntegerMethodSymbol(this, underlyingMethod, associatedSymbol: null));
                                    }
                                    break;
                            }
                            break;
                        case PropertySymbol underlyingProperty:
                            if (underlyingProperty.ParameterCount == 0 &&
                                underlyingProperty.Name != "Size")
                            {
                                var property = new NativeIntegerPropertySymbol(
                                    this,
                                    underlyingProperty,
                                    (container, property, underlyingAccessor) => underlyingAccessor is null ? null : new NativeIntegerMethodSymbol(container, underlyingAccessor, property));
                                builder.Add(property);
                                builder.AddIfNotNull(property.GetMethod);
                                builder.AddIfNotNull(property.SetMethod);
                            }
                            break;
                    }
                }
                return builder.ToImmutableAndFree();
            }
        }

        public override ImmutableArray<Symbol> GetMembers(string name) => GetMembers().WhereAsArray((member, name) => member.Name == name, name);

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers() => ImmutableArray<NamedTypeSymbol>.Empty;

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(ReadOnlyMemory<char> name) => ImmutableArray<NamedTypeSymbol>.Empty;

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(ReadOnlyMemory<char> name, int arity) => ImmutableArray<NamedTypeSymbol>.Empty;

        internal override NamedTypeSymbol GetDeclaredBaseType(ConsList<TypeSymbol> basesBeingResolved) => _underlyingType.GetDeclaredBaseType(basesBeingResolved);

        internal override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<TypeSymbol> basesBeingResolved) => GetInterfaces(basesBeingResolved);

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers() => throw ExceptionUtilities.Unreachable();

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers(string name) => throw ExceptionUtilities.Unreachable();

        internal override IEnumerable<FieldSymbol> GetFieldsToEmit() => throw ExceptionUtilities.Unreachable();

        internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit() => throw ExceptionUtilities.Unreachable();

        internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<TypeSymbol>? basesBeingResolved = null) => GetInterfaces(basesBeingResolved);

        protected override NamedTypeSymbol WithTupleDataCore(TupleExtraData newData) => throw ExceptionUtilities.Unreachable();

        internal override UseSiteInfo<AssemblySymbol> GetUseSiteInfo()
        {
            var useSiteInfo = _underlyingType.GetUseSiteInfo();
            Debug.Assert(useSiteInfo.DiagnosticInfo is null); // If assert fails, add unit test for use site diagnostic.
            return useSiteInfo;
        }

        public override bool AreLocalsZeroed => throw ExceptionUtilities.Unreachable();

        internal override bool IsNativeIntegerWrapperType => true;

        internal sealed override NamedTypeSymbol AsNativeInteger() => throw ExceptionUtilities.Unreachable();

        internal sealed override NamedTypeSymbol NativeIntegerUnderlyingType => _underlyingType;

        // note: there is no supported way to create a native integer type whose underlying type is file-local.
        internal sealed override bool IsFileLocal => false;
        internal sealed override FileIdentifier? AssociatedFileIdentifier => null;

        internal sealed override bool IsRecord => false;
        internal sealed override bool IsRecordStruct => false;
        internal sealed override bool IsExtension => false;
        internal override bool IsExplicitExtension => false;

        internal sealed override TypeSymbol? GetExtendedTypeNoUseSiteDiagnostics(ConsList<TypeSymbol>? basesBeingResolved) => null;

        internal sealed override TypeSymbol? GetDeclaredExtensionUnderlyingType()
            => throw ExceptionUtilities.Unreachable();

        internal sealed override Symbol? TryGetCorrespondingStaticMetadataExtensionMember(Symbol member) => null;

        internal sealed override bool HasPossibleWellKnownCloneMethod() => false;

        internal override bool Equals(TypeSymbol? other, TypeCompareKind comparison)
        {
            if (other is null)
            {
                return false;
            }
            if ((object)this == other)
            {
                return true;
            }
            if (!_underlyingType.Equals(other, comparison))
            {
                return false;
            }

            return (comparison & TypeCompareKind.IgnoreNativeIntegers) != 0 ||
                other.IsNativeIntegerWrapperType;
        }

        public override int GetHashCode() => _underlyingType.GetHashCode();

#if !DEBUG
        void Cci.IReference.Dispatch(Cci.MetadataVisitor visitor)
        {
            // Emit should use underlying symbol only.
            throw ExceptionUtilities.Unreachable();
        }
#endif

        private ImmutableArray<NamedTypeSymbol> GetInterfaces(ConsList<TypeSymbol>? basesBeingResolved)
        {
            if (_lazyInterfaces.IsDefault)
            {
                var interfaces = _underlyingType.InterfacesNoUseSiteDiagnostics(basesBeingResolved).SelectAsArray((type, map) => map.SubstituteNamedType(type), GetTypeMap());
                ImmutableInterlocked.InterlockedInitialize(ref _lazyInterfaces, interfaces);
            }
            return _lazyInterfaces;
        }

        private NativeIntegerTypeMap GetTypeMap()
        {
            if (_lazyTypeMap is null)
            {
                Interlocked.CompareExchange(ref _lazyTypeMap, new NativeIntegerTypeMap(this), null);
            }
            return _lazyTypeMap;
        }

        /// <summary>
        /// Replaces references to underlying type with references to native integer type.
        /// </summary>
        internal TypeWithAnnotations SubstituteUnderlyingType(TypeWithAnnotations type) => type.SubstituteType(GetTypeMap());

        /// <summary>
        /// Replaces references to underlying type with references to native integer type.
        /// </summary>
        internal NamedTypeSymbol SubstituteUnderlyingType(NamedTypeSymbol type) => GetTypeMap().SubstituteNamedType(type);

        internal static bool EqualsHelper<TSymbol>(TSymbol symbol, Symbol? other, TypeCompareKind comparison, Func<TSymbol, Symbol> getUnderlyingSymbol)
            where TSymbol : Symbol
        {
            if (other is null)
            {
                return false;
            }
            if ((object)symbol == other)
            {
                return true;
            }
            if (!getUnderlyingSymbol(symbol).Equals(other, comparison))
            {
                return false;
            }
            return (comparison & TypeCompareKind.IgnoreNativeIntegers) != 0 ||
                other is TSymbol;
        }

        [Conditional("DEBUG")]
        internal static void VerifyEquality(Symbol symbolA, Symbol symbolB)
        {
            Debug.Assert(!symbolA.Equals(symbolB, TypeCompareKind.ConsiderEverything));
            Debug.Assert(!symbolB.Equals(symbolA, TypeCompareKind.ConsiderEverything));
            Debug.Assert(symbolA.Equals(symbolB, TypeCompareKind.IgnoreNativeIntegers));
            Debug.Assert(symbolB.Equals(symbolA, TypeCompareKind.IgnoreNativeIntegers));
            Debug.Assert(symbolA.GetHashCode() == symbolB.GetHashCode());
        }

        internal override IEnumerable<(MethodSymbol Body, MethodSymbol Implemented)> SynthesizedInterfaceMethodImpls()
        {
            return SpecializedCollections.EmptyEnumerable<(MethodSymbol Body, MethodSymbol Implemented)>();
        }

        internal override bool HasInlineArrayAttribute(out int length)
        {
            length = 0;
            return false;
        }

        internal sealed override bool HasCollectionBuilderAttribute(out TypeSymbol? builderType, out string? methodName)
        {
            builderType = null;
            methodName = null;
            return false;
        }

        internal sealed override bool HasAsyncMethodBuilderAttribute(out TypeSymbol? builderArgument)
        {
            builderArgument = null;
            return false;
        }

        private sealed class NativeIntegerTypeMap : AbstractTypeMap
        {
            private readonly NativeIntegerTypeSymbol _type;
            private readonly SpecialType _specialType;

            internal NativeIntegerTypeMap(NativeIntegerTypeSymbol type)
            {
                _type = type;
                _specialType = _type.UnderlyingNamedType.SpecialType;

                Debug.Assert(_specialType == SpecialType.System_IntPtr || _specialType == SpecialType.System_UIntPtr);
            }

            internal override NamedTypeSymbol SubstituteTypeDeclaration(NamedTypeSymbol previous)
            {
                return previous.SpecialType == _specialType ?
                    _type :
                    base.SubstituteTypeDeclaration(previous);
            }

            internal override ImmutableArray<CustomModifier> SubstituteCustomModifiers(ImmutableArray<CustomModifier> customModifiers)
            {
                return customModifiers;
            }
        }
    }

    internal sealed class NativeIntegerMethodSymbol : WrappedMethodSymbol
#if !DEBUG
        , Cci.IReference
#endif
    {
        private readonly NativeIntegerTypeSymbol _container;
        private readonly NativeIntegerPropertySymbol? _associatedSymbol;
        private ImmutableArray<ParameterSymbol> _lazyParameters;

        internal NativeIntegerMethodSymbol(NativeIntegerTypeSymbol container, MethodSymbol underlyingMethod, NativeIntegerPropertySymbol? associatedSymbol)
        {
            Debug.Assert(!underlyingMethod.IsGenericMethod);
            _container = container;
            _associatedSymbol = associatedSymbol;
            UnderlyingMethod = underlyingMethod;
            NativeIntegerTypeSymbol.VerifyEquality(this, underlyingMethod);
        }

        public override Symbol ContainingSymbol => _container;

        public override MethodSymbol UnderlyingMethod { get; }

        public override TypeWithAnnotations ReturnTypeWithAnnotations => _container.SubstituteUnderlyingType(UnderlyingMethod.ReturnTypeWithAnnotations);

        public override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations => ImmutableArray<TypeWithAnnotations>.Empty;

        public override ImmutableArray<TypeParameterSymbol> TypeParameters => ImmutableArray<TypeParameterSymbol>.Empty;

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                if (_lazyParameters.IsDefault)
                {
                    var parameters = UnderlyingMethod.Parameters.SelectAsArray((p, m) => (ParameterSymbol)new NativeIntegerParameterSymbol(m._container, m, p), this);
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyParameters, parameters);
                }
                return _lazyParameters;
            }
        }

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations => ImmutableArray<MethodSymbol>.Empty;

        public override ImmutableArray<CustomModifier> RefCustomModifiers => UnderlyingMethod.RefCustomModifiers;

        internal override UnmanagedCallersOnlyAttributeData? GetUnmanagedCallersOnlyAttributeData(bool forceComplete) => UnderlyingMethod.GetUnmanagedCallersOnlyAttributeData(forceComplete);

        public override Symbol? AssociatedSymbol => _associatedSymbol;

        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal override bool IsNullableAnalysisEnabled() => throw ExceptionUtilities.Unreachable();

        public override bool Equals(Symbol? other, TypeCompareKind comparison) => NativeIntegerTypeSymbol.EqualsHelper(this, other, comparison, symbol => symbol.UnderlyingMethod);

        public override int GetHashCode() => UnderlyingMethod.GetHashCode();

#if !DEBUG
        void Cci.IReference.Dispatch(Cci.MetadataVisitor visitor)
        {
            // Emit should use underlying symbol only.
            throw ExceptionUtilities.Unreachable();
        }
#endif

        internal sealed override bool HasAsyncMethodBuilderAttribute(out TypeSymbol? builderArgument)
        {
            builderArgument = null;
            return false;
        }
    }

    internal sealed class NativeIntegerParameterSymbol : WrappedParameterSymbol
#if !DEBUG
        , Cci.IReference
#endif
    {
        private readonly NativeIntegerTypeSymbol _containingType;
        private readonly NativeIntegerMethodSymbol _container;

        internal NativeIntegerParameterSymbol(NativeIntegerTypeSymbol containingType, NativeIntegerMethodSymbol container, ParameterSymbol underlyingParameter) : base(underlyingParameter)
        {
            Debug.Assert(container != null);

            _containingType = containingType;
            _container = container;
            NativeIntegerTypeSymbol.VerifyEquality(this, underlyingParameter);
        }

        public override Symbol ContainingSymbol => _container;

        public override TypeWithAnnotations TypeWithAnnotations => _containingType.SubstituteUnderlyingType(_underlyingParameter.TypeWithAnnotations);

        public override ImmutableArray<CustomModifier> RefCustomModifiers => _underlyingParameter.RefCustomModifiers;

        internal override bool IsCallerLineNumber => _underlyingParameter.IsCallerLineNumber;

        internal override bool IsCallerFilePath => _underlyingParameter.IsCallerFilePath;

        internal override bool IsCallerMemberName => _underlyingParameter.IsCallerMemberName;

        internal override int CallerArgumentExpressionParameterIndex => _underlyingParameter.CallerArgumentExpressionParameterIndex;

        internal override ImmutableArray<int> InterpolatedStringHandlerArgumentIndexes => _underlyingParameter.InterpolatedStringHandlerArgumentIndexes;

        internal override bool HasInterpolatedStringHandlerArgumentError => _underlyingParameter.HasInterpolatedStringHandlerArgumentError;

        public override bool Equals(Symbol? other, TypeCompareKind comparison) => NativeIntegerTypeSymbol.EqualsHelper(this, other, comparison, symbol => symbol._underlyingParameter);

        public override int GetHashCode() => _underlyingParameter.GetHashCode();

#if !DEBUG
        void Cci.IReference.Dispatch(Cci.MetadataVisitor visitor)
        {
            // Emit should use underlying symbol only.
            throw ExceptionUtilities.Unreachable();
        }
#endif
    }

    internal sealed class NativeIntegerPropertySymbol : WrappedPropertySymbol
#if !DEBUG
        , Cci.IReference
#endif
    {
        private readonly NativeIntegerTypeSymbol _container;

        internal NativeIntegerPropertySymbol(
            NativeIntegerTypeSymbol container,
            PropertySymbol underlyingProperty,
            Func<NativeIntegerTypeSymbol, NativeIntegerPropertySymbol, MethodSymbol?, NativeIntegerMethodSymbol?> getAccessor) :
            base(underlyingProperty)
        {
            Debug.Assert(underlyingProperty.ParameterCount == 0);
            _container = container;
            GetMethod = getAccessor(container, this, underlyingProperty.GetMethod);
            SetMethod = getAccessor(container, this, underlyingProperty.SetMethod);
            NativeIntegerTypeSymbol.VerifyEquality(this, underlyingProperty);
        }

        public override Symbol ContainingSymbol => _container;

        public override TypeWithAnnotations TypeWithAnnotations => _container.SubstituteUnderlyingType(_underlyingProperty.TypeWithAnnotations);

        public override ImmutableArray<CustomModifier> RefCustomModifiers => UnderlyingProperty.RefCustomModifiers;

        public override ImmutableArray<ParameterSymbol> Parameters => ImmutableArray<ParameterSymbol>.Empty;

        public override MethodSymbol? GetMethod { get; }

        public override MethodSymbol? SetMethod { get; }

        public override ImmutableArray<PropertySymbol> ExplicitInterfaceImplementations => ImmutableArray<PropertySymbol>.Empty;

        internal override bool MustCallMethodsDirectly => _underlyingProperty.MustCallMethodsDirectly;

        public override bool Equals(Symbol? other, TypeCompareKind comparison) => NativeIntegerTypeSymbol.EqualsHelper(this, other, comparison, symbol => symbol._underlyingProperty);

        public override int GetHashCode() => _underlyingProperty.GetHashCode();

#if !DEBUG
        void Cci.IReference.Dispatch(Cci.MetadataVisitor visitor)
        {
            // Emit should use underlying symbol only.
            throw ExceptionUtilities.Unreachable();
        }
#endif
    }
}
