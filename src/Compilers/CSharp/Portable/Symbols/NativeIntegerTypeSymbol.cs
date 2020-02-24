// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    // PROTOTYPE: Handle retargeting these types.
    internal sealed class NativeIntegerTypeSymbol : WrappedNamedTypeSymbol, Cci.IReference
    {
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

        public override IEnumerable<string> MemberNames => Array.Empty<string>();

        /// <summary>
        /// There are no members on <see cref="System.IntPtr"/> that should be exposed directly on nint:
        /// constructors for nint other than the default parameterless constructor are not supported;
        /// operators are handled explicitly as built-in operators and conversions;
        /// 0 should be used instead of Zero;
        /// sizeof(nint) should be used instead of Size;
        /// + and - should be used instead of Add() and Subtract();
        /// ToInt32(), ToInt64(), ToPointer() should be used from System.IntPtr only;
        /// overridden methods Equals(), GetHashCode(), and ToString() are referenced from System.Object.
        /// The one remaining member is <see cref="System.IntPtr.ToString(string)"/> which we could expose if needed.
        /// </summary>
        public override ImmutableArray<Symbol> GetMembers() => ImmutableArray<Symbol>.Empty;

        public override ImmutableArray<Symbol> GetMembers(string name) => ImmutableArray<Symbol>.Empty;

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

        void Cci.IReference.Dispatch(Cci.MetadataVisitor visitor)
        {
            // NativeIntegerTypeSymbol should be used in emit.
            throw ExceptionUtilities.Unreachable;
        }
    }
}
