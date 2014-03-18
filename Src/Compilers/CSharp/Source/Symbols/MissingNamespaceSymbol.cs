// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A <see cref="MissingNamespaceSymbol"/> is a special kind of <see cref="NamespaceSymbol"/> that represents
    /// a namespace that couldn't be found.
    /// </summary>
    internal class MissingNamespaceSymbol : NamespaceSymbol
    {
        private readonly string name;
        private readonly Symbol containingSymbol;

        public MissingNamespaceSymbol(MissingModuleSymbol containingModule)
        {
            Debug.Assert((object)containingModule != null);

            this.containingSymbol = containingModule;
            this.name = string.Empty;
        }

        public MissingNamespaceSymbol(NamespaceSymbol containingNamespace, string name)
        {
            Debug.Assert((object)containingNamespace != null);
            Debug.Assert(name != null);

            this.containingSymbol = containingNamespace;
            this.name = name;
        }

        public override string Name
        {
            get
            {
                return name;
            }
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return containingSymbol;
            }
        }

        public override AssemblySymbol ContainingAssembly
        {
            get
            {
                return ContainingModule.ContainingAssembly;
            }
        }

        internal override NamespaceExtent Extent
        {
            get
            {
                return new NamespaceExtent(ContainingModule);
            }
        }

        internal override ModuleSymbol ContainingModule
        {
            get
            {
                if (containingSymbol.Kind == SymbolKind.NetModule)
                {
                    return (ModuleSymbol)containingSymbol;
                }

                return containingSymbol.ContainingModule;
            }
        }

        public override int GetHashCode()
        {
            return Hash.Combine(containingSymbol.GetHashCode(), name.GetHashCode());
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            MissingNamespaceSymbol other = obj as MissingNamespaceSymbol;

            return (object)other != null && name.Equals(other.name) && containingSymbol.Equals(other.containingSymbol);
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return ImmutableArray<Location>.Empty;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return ImmutableArray<SyntaxReference>.Empty;
            }
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers()
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name)
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name, int arity)
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        public override ImmutableArray<Symbol> GetMembers()
        {
            return ImmutableArray<Symbol>.Empty;
        }

        public override ImmutableArray<Symbol> GetMembers(string name)
        {
            return ImmutableArray<Symbol>.Empty;
        }
    }
}