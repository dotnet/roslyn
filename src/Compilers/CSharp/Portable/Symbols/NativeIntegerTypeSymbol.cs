// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class NativeIntegerTypeSymbol : WrappedNamedTypeSymbol
    {
        internal NativeIntegerTypeSymbol(NamedTypeSymbol underlying) : base(underlying)
        {
            Debug.Assert(!underlying.IsNativeInt);
            Debug.Assert(underlying.SpecialType == SpecialType.System_IntPtr || underlying.SpecialType == SpecialType.System_UIntPtr);
            Debug.Assert(this.Equals(underlying));
            Debug.Assert(underlying.Equals(this));
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters => ImmutableArray<TypeParameterSymbol>.Empty;

        public override NamedTypeSymbol ConstructedFrom => _underlyingType.ConstructedFrom;

        public override Symbol ContainingSymbol => _underlyingType.ContainingSymbol;

        internal override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotationsNoUseSiteDiagnostics => ImmutableArray<TypeWithAnnotations>.Empty;

        internal override bool IsComImport => _underlyingType.IsComImport;

        internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics => _underlyingType.BaseTypeNoUseSiteDiagnostics;

        public override SpecialType SpecialType => _underlyingType.SpecialType;

        public override IEnumerable<string> MemberNames => throw new NotImplementedException();

        public override ImmutableArray<Symbol> GetMembers() => _underlyingType.GetMembers();

        public override ImmutableArray<Symbol> GetMembers(string name) => _underlyingType.GetMembers(name);

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers() => _underlyingType.GetTypeMembers();

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name) => _underlyingType.GetTypeMembers(name);

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name, int arity) => _underlyingType.GetTypeMembers(name, arity);

        internal override NamedTypeSymbol GetDeclaredBaseType(ConsList<TypeSymbol> basesBeingResolved) => _underlyingType.GetDeclaredBaseType(basesBeingResolved);

        internal override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<TypeSymbol> basesBeingResolved) => _underlyingType.GetDeclaredInterfaces(basesBeingResolved);

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers() => throw ExceptionUtilities.Unreachable;

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers(string name) => throw ExceptionUtilities.Unreachable;

        internal override IEnumerable<FieldSymbol> GetFieldsToEmit() => throw ExceptionUtilities.Unreachable;

        internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit() => throw ExceptionUtilities.Unreachable;

        internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<TypeSymbol> basesBeingResolved = null) => _underlyingType.InterfacesNoUseSiteDiagnostics(basesBeingResolved);

        internal override bool IsNativeInt => true;

        internal override NamedTypeSymbol AsNativeInt(bool asNativeInt) => asNativeInt ? this : _underlyingType;

        internal override bool Equals(TypeSymbol t2, TypeCompareKind comparison, IReadOnlyDictionary<TypeParameterSymbol, bool> isValueTypeOverrideOpt = null) => _underlyingType.Equals(t2, comparison, isValueTypeOverrideOpt);
    }
}
