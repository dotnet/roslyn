// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    internal class MockNamespaceSymbol : NamespaceSymbol, IMockSymbol
    {
        private NamespaceSymbol _container;
        private readonly NamespaceExtent _extent;
        private readonly IEnumerable<Symbol> _children;
        private readonly string _name;

        public MockNamespaceSymbol(string name, NamespaceExtent extent, IEnumerable<Symbol> children)
        {
            _name = name;
            _extent = extent;
            _children = children;
        }

        public void SetContainer(Symbol container)
        {
            _container = (NamespaceSymbol)container;
        }

        public override string Name
        {
            get
            {
                return _name;
            }
        }

        internal override NamespaceExtent Extent
        {
            get
            {
                return _extent;
            }
        }

        public override ImmutableArray<Symbol> GetMembers()
        {
            return _children.AsImmutable();
        }

        public override ImmutableArray<Symbol> GetMembers(string name)
        {
            return _children.Where(ns => (ns.Name == name)).ToArray().AsImmutableOrNull();
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers()
        {
            return (from c in _children
                    where c is NamedTypeSymbol
                    select (NamedTypeSymbol)c).ToArray().AsImmutableOrNull();
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name)
        {
            return (from c in _children
                    where c is NamedTypeSymbol && c.Name == name
                    select (NamedTypeSymbol)c).ToArray().AsImmutableOrNull();
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return _container;
            }
        }

        public override AssemblySymbol ContainingAssembly
        {
            get
            {
                return _container.ContainingAssembly;
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return ImmutableArray.Create<Location>();
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return ImmutableArray.Create<SyntaxReference>();
            }
        }
    }
}
