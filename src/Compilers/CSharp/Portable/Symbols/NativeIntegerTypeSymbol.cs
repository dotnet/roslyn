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
    internal sealed class NativeIntegerTypeSymbol : WrappedNamedTypeSymbol, Cci.IReference
    {
        private ImmutableArray<NamedTypeSymbol> _lazyInterfaces;

        internal NativeIntegerTypeSymbol(NamedTypeSymbol underlyingType) : base(underlyingType, tupleData: null)
        {
            Debug.Assert(underlyingType.TupleData is null);
            Debug.Assert(!underlyingType.IsNativeIntegerType);
            Debug.Assert(underlyingType.SpecialType == SpecialType.System_IntPtr || underlyingType.SpecialType == SpecialType.System_UIntPtr);
            Debug.Assert(this.Equals(underlyingType));
            Debug.Assert(underlyingType.Equals(this));
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

        internal override bool Equals(TypeSymbol t2, TypeCompareKind comparison, IReadOnlyDictionary<TypeParameterSymbol, bool>? isValueTypeOverrideOpt = null) => _underlyingType.Equals(t2, comparison, isValueTypeOverrideOpt);

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
                ImmutableInterlocked.InterlockedCompareExchange(ref _lazyInterfaces, makeInterfaces(_underlyingType.InterfacesNoUseSiteDiagnostics(basesBeingResolved)), default(ImmutableArray<NamedTypeSymbol>));
            }
            return _lazyInterfaces;

            // Return IEquatable<n[u]int> if the underlying type implemented IEquatable<System.[U]IntPtr>.
            ImmutableArray<NamedTypeSymbol> makeInterfaces(ImmutableArray<NamedTypeSymbol> underlyingInterfaces)
            {
                Debug.Assert(_underlyingType.SpecialType == SpecialType.System_IntPtr || _underlyingType.SpecialType == SpecialType.System_UIntPtr);

                foreach (var underlyingInterface in underlyingInterfaces)
                {
                    // Is the underlying interface IEquatable<System.[U]IntPtr>?
                    if (underlyingInterface.Name != "IEquatable")
                    {
                        continue;
                    }
                    var typeArgs = underlyingInterface.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics;
                    if (typeArgs.Length == 1 && _underlyingType.SpecialType == typeArgs[0].Type.SpecialType)
                    {
                        var def = underlyingInterface.OriginalDefinition;
                        if (def.ContainingSymbol is NamespaceSymbol { Name: "System", ContainingSymbol: NamespaceSymbol { IsGlobalNamespace: true } })
                        {
                            // Return IEquatable<n[u]int>.
                            return ImmutableArray.Create(def.Construct(ImmutableArray.Create(TypeWithAnnotations.Create(this))));
                        }
                    }
                }

                return ImmutableArray<NamedTypeSymbol>.Empty;
            }
        }
    }
}
