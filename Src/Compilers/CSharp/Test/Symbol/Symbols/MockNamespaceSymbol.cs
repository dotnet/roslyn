// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    internal class MockNamespaceSymbol : NamespaceSymbol, IMockSymbol
    {
        private NamespaceSymbol container;
        private NamespaceExtent extent;
        private readonly IEnumerable<Symbol> children;
        private readonly string name;

        public MockNamespaceSymbol(string name, NamespaceExtent extent, IEnumerable<Symbol> children)
        {
            this.name = name;
            this.extent = extent;
            this.children = children;
        }

        public void SetContainer(Symbol container)
        {
            this.container = (NamespaceSymbol)container;
        }

        public override string Name
        {
            get
            {
                return name;
            }
        }

        internal override NamespaceExtent Extent
        {
            get
            {
                return extent;
            }
        }

        public override ImmutableArray<Symbol> GetMembers()
        {
            return children.AsImmutable();
        }

        public override ImmutableArray<Symbol> GetMembers(string name)
        {
            return children.Where(ns => (ns.Name == name)).ToArray().AsImmutableOrNull();
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers()
        {
            return (from c in children
                    where c is NamedTypeSymbol
                    select (NamedTypeSymbol)c).ToArray().AsImmutableOrNull();
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name)
        {
            return (from c in children
                    where c is NamedTypeSymbol && c.Name == name
                    select (NamedTypeSymbol)c).ToArray().AsImmutableOrNull();
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return container;
            }
        }

        public override AssemblySymbol ContainingAssembly
        {
            get
            {
                return container.ContainingAssembly;
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