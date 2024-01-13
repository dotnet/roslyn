// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Roslyn.Utilities;
using System.Diagnostics;
using System;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Synthesized namespace that contains synthesized types or subnamespaces.
    /// All its members are stored in a table on <see cref="CommonPEModuleBuilder"/>.
    /// </summary>
    internal sealed class SynthesizedNamespaceSymbol : NamespaceSymbol
    {
        private readonly string _name;
        private readonly NamespaceSymbol _containingSymbol;

        public SynthesizedNamespaceSymbol(NamespaceSymbol containingNamespace, string name)
        {
            Debug.Assert(containingNamespace is object);
            Debug.Assert(name is object);

            _containingSymbol = containingNamespace;
            _name = name;
        }

        public override int GetHashCode()
            => Hash.Combine(_containingSymbol.GetHashCode(), _name.GetHashCode());

        public override bool Equals(Symbol obj, TypeCompareKind compareKind)
            => obj is SynthesizedNamespaceSymbol other && Equals(other);

        public bool Equals(SynthesizedNamespaceSymbol other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return other is object && _name.Equals(other._name) && _containingSymbol.Equals(other._containingSymbol);
        }

        internal override NamespaceExtent Extent
            => _containingSymbol.Extent;

        public override string Name
            => _name;

        public override Symbol ContainingSymbol
            => _containingSymbol;

        public override AssemblySymbol ContainingAssembly
            => _containingSymbol.ContainingAssembly;

        public override ImmutableArray<Location> Locations
            => ImmutableArray<Location>.Empty;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
            => ImmutableArray<SyntaxReference>.Empty;

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers()
            => ImmutableArray<NamedTypeSymbol>.Empty;

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(ReadOnlyMemory<char> name)
            => ImmutableArray<NamedTypeSymbol>.Empty;

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(ReadOnlyMemory<char> name, int arity)
            => ImmutableArray<NamedTypeSymbol>.Empty;

        public override ImmutableArray<Symbol> GetMembers()
            => ImmutableArray<Symbol>.Empty;

        public override ImmutableArray<Symbol> GetMembers(ReadOnlyMemory<char> name)
            => ImmutableArray<Symbol>.Empty;
    }
}
