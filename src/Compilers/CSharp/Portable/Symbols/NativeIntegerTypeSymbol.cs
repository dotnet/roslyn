// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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
    internal sealed class NativeIntegerTypeSymbol : WrappedNamedTypeSymbol, Cci.IReference
    {
        private ImmutableArray<NamedTypeSymbol> _lazyInterfaces;
        private ImmutableArray<Symbol> _lazyMembers;
        private NativeIntegerTypeMap? _lazyTypeMap;

        internal NativeIntegerTypeSymbol(NamedTypeSymbol underlyingType) : base(underlyingType, tupleData: null)
        {
            Debug.Assert(underlyingType.TupleData is null);
            Debug.Assert(!underlyingType.IsNativeIntegerType);
            Debug.Assert(underlyingType.SpecialType == SpecialType.System_IntPtr || underlyingType.SpecialType == SpecialType.System_UIntPtr);
            Debug.Assert(!this.Equals(underlyingType, TypeCompareKind.ConsiderEverything));
            Debug.Assert(!underlyingType.Equals(this, TypeCompareKind.ConsiderEverything));
            Debug.Assert(this.Equals(underlyingType, TypeCompareKind.IgnoreNativeIntegers));
            Debug.Assert(underlyingType.Equals(this, TypeCompareKind.IgnoreNativeIntegers));
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters => ImmutableArray<TypeParameterSymbol>.Empty;

        public override NamedTypeSymbol ConstructedFrom => this;

        public override Symbol ContainingSymbol => _underlyingType.ContainingSymbol;

        internal override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotationsNoUseSiteDiagnostics => ImmutableArray<TypeWithAnnotations>.Empty;

        internal override bool IsComImport => _underlyingType.IsComImport;

        internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics => _underlyingType.BaseTypeNoUseSiteDiagnostics;

        public override SpecialType SpecialType => _underlyingType.SpecialType;

        public override IEnumerable<string> MemberNames => GetMembers().Select(m => m.Name);

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
                builder.Add(new SynthesizedInstanceConstructor(this));
                foreach (var underlyingMember in underlyingMembers)
                {
                    Debug.Assert(_underlyingType.Equals(underlyingMember.ContainingSymbol));
                    switch (underlyingMember)
                    {
                        case MethodSymbol underlyingMethod:
                            if (underlyingMethod.IsGenericMethod)
                            {
                                break;
                            }
                            switch (underlyingMethod.MethodKind)
                            {
                                case MethodKind.Ordinary:
                                    if (underlyingMethod.IsOverride)
                                    {
                                        addMethodIfAny(builder, underlyingMethod);
                                    }
                                    else
                                    {
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
                                                addMethodIfAny(builder, underlyingMethod);
                                                break;
                                        }
                                    }
                                    break;
                                case MethodKind.ExplicitInterfaceImplementation:
                                    addMethodIfAny(builder, underlyingMethod);
                                    break;
                            }
                            break;
                        case PropertySymbol underlyingProperty:
                            if (underlyingProperty.ParameterCount == 0 &&
                                underlyingProperty.Name != "Size")
                            {
                                var property = new NativeIntegerPropertySymbol(this, underlyingProperty);
                                builder.Add(property);
                                addMethodIfAny(builder, underlyingProperty.GetMethod, property);
                                addMethodIfAny(builder, underlyingProperty.SetMethod, property);
                            }
                            break;
                    }
                }
                return builder.ToImmutableAndFree();
            }

            void addMethodIfAny(ArrayBuilder<Symbol> builder, MethodSymbol underlyingMethod, PropertySymbol? associatedProperty = null)
            {
                Debug.Assert(associatedProperty is null || (object)associatedProperty.ContainingSymbol == this);
                if (underlyingMethod is { })
                {
                    builder.Add(new NativeIntegerMethodSymbol(this, underlyingMethod, associatedProperty));
                }
            }
        }

        public override ImmutableArray<Symbol> GetMembers(string name) => GetMembers().WhereAsArray((member, name) => member.Name == name, name);

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers() => ImmutableArray<NamedTypeSymbol>.Empty;

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name) => ImmutableArray<NamedTypeSymbol>.Empty;

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name, int arity) => ImmutableArray<NamedTypeSymbol>.Empty;

        internal override NamedTypeSymbol GetDeclaredBaseType(ConsList<TypeSymbol> basesBeingResolved) => _underlyingType.GetDeclaredBaseType(basesBeingResolved);

        internal override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<TypeSymbol> basesBeingResolved) => GetInterfaces(basesBeingResolved);

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers() => throw ExceptionUtilities.Unreachable;

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers(string name) => throw ExceptionUtilities.Unreachable;

        internal override IEnumerable<FieldSymbol> GetFieldsToEmit() => throw ExceptionUtilities.Unreachable;

        internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit() => throw ExceptionUtilities.Unreachable;

        internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<TypeSymbol>? basesBeingResolved = null) => GetInterfaces(basesBeingResolved);

        protected override NamedTypeSymbol WithTupleDataCore(TupleExtraData newData) => throw ExceptionUtilities.Unreachable;

        internal override DiagnosticInfo? GetUseSiteDiagnostic()
        {
            var diagnostic = _underlyingType.GetUseSiteDiagnostic();
            Debug.Assert(diagnostic is null); // If assert fails, add unit test for GetUseSiteDiagnostic().
            return diagnostic;
        }

        public override bool AreLocalsZeroed => throw ExceptionUtilities.Unreachable;

        internal override bool IsNativeIntegerType => true;

        internal sealed override NamedTypeSymbol AsNativeInteger() => throw ExceptionUtilities.Unreachable;

        internal sealed override NamedTypeSymbol NativeIntegerUnderlyingType => _underlyingType;

        internal override bool Equals(TypeSymbol other, TypeCompareKind comparison, IReadOnlyDictionary<TypeParameterSymbol, bool>? isValueTypeOverrideOpt = null)
        {
            if ((object)other == null)
            {
                return false;
            }
            if ((object)this == other)
            {
                return true;
            }
            if (!_underlyingType.Equals(other, comparison, isValueTypeOverrideOpt))
            {
                return false;
            }
            return (comparison & TypeCompareKind.IgnoreNativeIntegers) != 0 ||
                other.IsNativeIntegerType;
        }

        public override int GetHashCode() => _underlyingType.GetHashCode();

        void Cci.IReference.Dispatch(Cci.MetadataVisitor visitor)
        {
            // NativeIntegerTypeSymbol should not be used in emit.
            throw ExceptionUtilities.Unreachable;
        }

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
        }
    }

    internal sealed class NativeIntegerMethodSymbol : WrappedMethodSymbol
    {
        private readonly NativeIntegerTypeSymbol _container;
        private readonly Symbol? _associatedSymbol;

        internal NativeIntegerMethodSymbol(NativeIntegerTypeSymbol container, MethodSymbol underlyingMethod, Symbol? associatedSymbol)
        {
            Debug.Assert(!underlyingMethod.IsGenericMethod);
            _container = container;
            _associatedSymbol = associatedSymbol;
            UnderlyingMethod = underlyingMethod;
        }

        public override Symbol ContainingSymbol => _container;

        public override MethodSymbol UnderlyingMethod { get; }

        public override TypeWithAnnotations ReturnTypeWithAnnotations => _container.SubstituteUnderlyingType(UnderlyingMethod.ReturnTypeWithAnnotations);

        public override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations => ImmutableArray<TypeWithAnnotations>.Empty;

        public override ImmutableArray<TypeParameterSymbol> TypeParameters => ImmutableArray<TypeParameterSymbol>.Empty;

        public override ImmutableArray<ParameterSymbol> Parameters =>
            UnderlyingMethod.Parameters.SelectAsArray((p, m) => (ParameterSymbol)new NativeIntegerParameterSymbol(m._container, m, p), this);

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations =>
            UnderlyingMethod.ExplicitInterfaceImplementations.SelectAsArray(
                (method, map) => method.OriginalDefinition.AsMember(_container.SubstituteUnderlyingType(method.ContainingType)),
                _container);

        public override ImmutableArray<CustomModifier> RefCustomModifiers => UnderlyingMethod.RefCustomModifiers;

        public override Symbol? AssociatedSymbol => _associatedSymbol;

        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
        {
            throw new NotImplementedException();
        }
    }

    internal sealed class NativeIntegerPropertySymbol : WrappedPropertySymbol
    {
        private readonly NativeIntegerTypeSymbol _container;

        internal NativeIntegerPropertySymbol(NativeIntegerTypeSymbol container, PropertySymbol underlyingProperty) : base(underlyingProperty)
        {
            Debug.Assert(underlyingProperty.ParameterCount == 0);
            _container = container;
        }

        public override Symbol ContainingSymbol => _container;

        public override TypeWithAnnotations TypeWithAnnotations => _container.SubstituteUnderlyingType(_underlyingProperty.TypeWithAnnotations);

        public override ImmutableArray<CustomModifier> RefCustomModifiers => UnderlyingProperty.RefCustomModifiers;

        public override ImmutableArray<ParameterSymbol> Parameters => ImmutableArray<ParameterSymbol>.Empty;

        public override MethodSymbol? GetMethod => MakeAccessor(UnderlyingProperty.GetMethod);

        public override MethodSymbol? SetMethod => MakeAccessor(UnderlyingProperty.SetMethod);

        public override ImmutableArray<PropertySymbol> ExplicitInterfaceImplementations =>
            UnderlyingProperty.ExplicitInterfaceImplementations.SelectAsArray(
                (property, map) => property.OriginalDefinition.AsMember(_container.SubstituteUnderlyingType(property.ContainingType)),
                _container);

        internal override bool MustCallMethodsDirectly => _underlyingProperty.MustCallMethodsDirectly;

        private NativeIntegerMethodSymbol? MakeAccessor(MethodSymbol? accessor) => accessor is null ? null : new NativeIntegerMethodSymbol(_container, accessor, this);
    }

    internal sealed class NativeIntegerParameterSymbol : WrappedParameterSymbol
    {
        private readonly NativeIntegerTypeSymbol _containingType;
        private readonly Symbol _container;

        internal NativeIntegerParameterSymbol(NativeIntegerTypeSymbol containingType, Symbol container, ParameterSymbol underlyingParameter) : base(underlyingParameter)
        {
            _containingType = containingType;
            _container = container;
        }

        public override Symbol ContainingSymbol => _container;

        public override TypeWithAnnotations TypeWithAnnotations => _containingType.SubstituteUnderlyingType(UnderlyingParameter.TypeWithAnnotations);
    }
}
