// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class NativeIntegerTypeSymbol : WrappedNamedTypeSymbol
    {
        private ImmutableArray<Symbol> _lazyMembers;

        internal NativeIntegerTypeSymbol(NamedTypeSymbol underlying) : base(underlying, tupleData: null)
        {
            Debug.Assert(underlying.TupleData is null);
            Debug.Assert(!underlying.IsNativeInt);
            Debug.Assert(underlying.SpecialType == SpecialType.System_IntPtr || underlying.SpecialType == SpecialType.System_UIntPtr);
            Debug.Assert(this.Equals(underlying));
            Debug.Assert(underlying.Equals(this));
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters => ImmutableArray<TypeParameterSymbol>.Empty;

        public override NamedTypeSymbol ConstructedFrom => this;

        public override Symbol ContainingSymbol => _underlyingType.ContainingSymbol;

        internal override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotationsNoUseSiteDiagnostics => ImmutableArray<TypeWithAnnotations>.Empty;

        internal override bool IsComImport => _underlyingType.IsComImport;

        internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics => _underlyingType.BaseTypeNoUseSiteDiagnostics;

        public override SpecialType SpecialType => _underlyingType.SpecialType;

        public override IEnumerable<string> MemberNames => GetMembers().SelectAsArray(m => m.Name);

        /// <summary>
        /// There only members of <see cref="System.IntPtr"/> that are exposed directly on nint
        /// are the overridden methods Equals(), GetHashCode(), and ToString(). For the other members:
        /// constructors for nint other than the default parameterless constructor are not supported;
        /// operators are handled explicitly as built-in operators and conversions;
        /// 0 should be used instead of Zero;
        /// sizeof(nint) should be used instead of Size;
        /// + and - should be used instead of Add() and Subtract();
        /// ToInt32(), ToInt64(), ToPointer() should be used from System.IntPtr only.
        /// The one remaining member is <see cref="System.IntPtr.ToString(string)"/> which we could expose if needed.
        /// </summary>
        public override ImmutableArray<Symbol> GetMembers()
        {
            if (_lazyMembers.IsDefault)
            {
                ImmutableInterlocked.InterlockedInitialize(ref _lazyMembers, CalculateMembers(this, _underlyingType));
            }
            return _lazyMembers;
        }

        public override ImmutableArray<Symbol> GetMembers(string name) => GetMembers().WhereAsArray(m => m.Name == name);

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers() => ImmutableArray<NamedTypeSymbol>.Empty;

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name) => ImmutableArray<NamedTypeSymbol>.Empty;

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name, int arity) => ImmutableArray<NamedTypeSymbol>.Empty;

        internal override NamedTypeSymbol GetDeclaredBaseType(ConsList<TypeSymbol> basesBeingResolved) => _underlyingType.GetDeclaredBaseType(basesBeingResolved);

        internal override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<TypeSymbol> basesBeingResolved) => _underlyingType.GetDeclaredInterfaces(basesBeingResolved);

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers() => throw ExceptionUtilities.Unreachable;

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers(string name) => throw ExceptionUtilities.Unreachable;

        internal override IEnumerable<FieldSymbol> GetFieldsToEmit() => throw ExceptionUtilities.Unreachable;

        internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit() => throw ExceptionUtilities.Unreachable;

        // PROTOTYPE: Include certain interfaces defined on the underlying type, with substitution
        // of [U]IntPtr (for instance, IEquatable<nint> rather than IEquatable<IntPtr>).
        internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<TypeSymbol>? basesBeingResolved = null) => ImmutableArray<NamedTypeSymbol>.Empty;

        protected override NamedTypeSymbol WithTupleDataCore(TupleExtraData newData) => throw ExceptionUtilities.Unreachable;

        public override bool AreLocalsZeroed => throw ExceptionUtilities.Unreachable;

        internal override bool IsNativeInt => true;

        internal override NamedTypeSymbol AsNativeInt(bool asNativeInt) => asNativeInt ? this : _underlyingType;

        internal override bool Equals(TypeSymbol t2, TypeCompareKind comparison, IReadOnlyDictionary<TypeParameterSymbol, bool>? isValueTypeOverrideOpt = null) => _underlyingType.Equals(t2, comparison, isValueTypeOverrideOpt);

        public override int GetHashCode() => _underlyingType.GetHashCode();

        private static ImmutableArray<Symbol> CalculateMembers(NativeIntegerTypeSymbol containingType, NamedTypeSymbol underlyingType)
        {
            var underlyingMembers = underlyingType.GetMembers();
            var builder = ArrayBuilder<Symbol>.GetInstance();
            addMethodIfAny(SpecialMember.System_Object__GetHashCode);
            addMethodIfAny(SpecialMember.System_Object__Equals);
            addMethodIfAny(SpecialMember.System_Object__ToString);
            return builder.ToImmutableAndFree();

            void addMethodIfAny(SpecialMember id)
            {
                var method = (MethodSymbol?)CSharpCompilation.GetRuntimeMember(
                    underlyingMembers,
                    SpecialMembers.GetDescriptor(id),
                    CSharpCompilation.SpecialMembersSignatureComparer.Instance,
                    accessWithinOpt: null);
                if (method is object)
                {
                    builder.Add(new NativeIntegerMethodSymbol(containingType, method));
                }
            }
        }

        private sealed class NativeIntegerMethodSymbol : WrappedMethodSymbol
        {
            private readonly NativeIntegerTypeSymbol _containingType;
            private readonly MethodSymbol _underlyingMethod;
            private ImmutableArray<ParameterSymbol> _lazyParameters;

            internal NativeIntegerMethodSymbol(NativeIntegerTypeSymbol containingType, MethodSymbol underlyingMethod)
            {
                Debug.Assert(underlyingMethod.TypeParameters.IsEmpty);
                Debug.Assert(underlyingMethod.AssociatedSymbol is null);

                _containingType = containingType;
                _underlyingMethod = underlyingMethod;
            }

            public override MethodSymbol UnderlyingMethod => _underlyingMethod;

            public override TypeWithAnnotations ReturnTypeWithAnnotations => _underlyingMethod.ReturnTypeWithAnnotations;

            public override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations => ImmutableArray<TypeWithAnnotations>.Empty;

            public override ImmutableArray<TypeParameterSymbol> TypeParameters => ImmutableArray<TypeParameterSymbol>.Empty;

            public override ImmutableArray<ParameterSymbol> Parameters
            {
                get
                {
                    if (_lazyParameters.IsDefault)
                    {
                        ImmutableInterlocked.InterlockedInitialize(
                            ref _lazyParameters,
                            _underlyingMethod.Parameters.SelectAsArray((p, m) => (ParameterSymbol)new NativeIntegerParameterSymbol(m, p), this));
                    }
                    return _lazyParameters;
                }
            }

            public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations => ImmutableArray<MethodSymbol>.Empty;

            public override ImmutableArray<CustomModifier> RefCustomModifiers => ImmutableArray<CustomModifier>.Empty;

            public override Symbol? AssociatedSymbol => null;

            public override Symbol ContainingSymbol => _containingType;

            internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree) => throw ExceptionUtilities.Unreachable;
        }

        private sealed class NativeIntegerParameterSymbol : WrappedParameterSymbol
        {
            private readonly NativeIntegerMethodSymbol _containingMethod;

            internal NativeIntegerParameterSymbol(NativeIntegerMethodSymbol containingMethod, ParameterSymbol underlyingParameter) :
                base(underlyingParameter)
            {
                _containingMethod = containingMethod;
            }

            public override Symbol ContainingSymbol => _containingMethod;
        }
    }
}
