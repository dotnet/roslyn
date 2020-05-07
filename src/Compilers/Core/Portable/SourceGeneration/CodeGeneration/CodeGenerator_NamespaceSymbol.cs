// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.SourceGeneration
{
    internal static partial class CodeGenerator
    {
        public static INamespaceSymbol GlobalNamespace(
            ImmutableArray<INamespaceOrTypeSymbol> imports = default,
            ImmutableArray<INamespaceOrTypeSymbol> members = default)
        {
            return Namespace("", imports, members);
        }

        public static INamespaceSymbol Namespace(
            string name,
            ImmutableArray<INamespaceOrTypeSymbol> imports = default,
            ImmutableArray<INamespaceOrTypeSymbol> members = default)
        {
            return new NamespaceSymbol(
                name,
                imports,
                members);
        }

        public static INamespaceSymbol With(
            this INamespaceSymbol symbol,
            Optional<string> name = default,
            Optional<ImmutableArray<INamespaceOrTypeSymbol>> imports = default,
            Optional<ImmutableArray<INamespaceOrTypeSymbol>> members = default)
        {
            return new NamespaceSymbol(
                name.GetValueOr(symbol.Name),
                imports.GetValueOr(GetImports(symbol)),
                members.GetValueOr(symbol.GetMembers().ToImmutableArray()));
        }

        internal static ImmutableArray<INamespaceOrTypeSymbol> GetImports(INamespaceSymbol symbol)
            => symbol is NamespaceSymbol nsSymbol ? nsSymbol.Imports : ImmutableArray<INamespaceOrTypeSymbol>.Empty;

        private class NamespaceSymbol : NamespaceOrTypeSymbol, INamespaceSymbol
        {
            public readonly ImmutableArray<INamespaceOrTypeSymbol> Imports;

            private readonly ImmutableArray<INamespaceOrTypeSymbol> _members;

            public NamespaceSymbol(
                string name,
                ImmutableArray<INamespaceOrTypeSymbol> imports,
                ImmutableArray<INamespaceOrTypeSymbol> members)
            {
                Name = name;
                Imports = imports.NullToEmpty();
                _members = members.NullToEmpty();
            }

            public bool IsGlobalNamespace => Name == "";

            public override SymbolKind Kind => SymbolKind.Namespace;
            public override string Name { get; }

            public override ImmutableArray<ISymbol> GetMembers()
                => ImmutableArray<ISymbol>.CastUp(_members);

            IEnumerable<INamespaceOrTypeSymbol> INamespaceSymbol.GetMembers()
                => _members;

            public override void Accept(SymbolVisitor visitor)
                => visitor.VisitNamespace(this);

            public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
                => visitor.VisitNamespace(this);

            #region default implementation

            public NamespaceKind NamespaceKind => throw new System.NotImplementedException();

            public Compilation ContainingCompilation => throw new System.NotImplementedException();

            public ImmutableArray<INamespaceSymbol> ConstituentNamespaces => throw new System.NotImplementedException();

            public IEnumerable<INamespaceSymbol> GetNamespaceMembers()
            {
                throw new System.NotImplementedException();
            }

            IEnumerable<INamespaceOrTypeSymbol> INamespaceSymbol.GetMembers(string name)
            {
                throw new System.NotImplementedException();
            }

            #endregion
        }
    }
}
