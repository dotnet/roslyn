// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.PublicModel
{
    internal sealed class NamespaceSymbol : NamespaceOrTypeSymbol, INamespaceSymbol
    {
        private readonly Symbols.NamespaceSymbol _underlying;

        public NamespaceSymbol(Symbols.NamespaceSymbol underlying)
        {
            Debug.Assert(underlying is object);
            _underlying = underlying;
        }

        internal override CSharp.Symbol UnderlyingSymbol => _underlying;
        internal override Symbols.NamespaceOrTypeSymbol UnderlyingNamespaceOrTypeSymbol => _underlying;
        internal Symbols.NamespaceSymbol UnderlyingNamespaceSymbol => _underlying;

        bool INamespaceSymbol.IsGlobalNamespace => _underlying.IsGlobalNamespace;

        NamespaceKind INamespaceSymbol.NamespaceKind => _underlying.NamespaceKind;

        Compilation INamespaceSymbol.ContainingCompilation => _underlying.ContainingCompilation;

        ImmutableArray<INamespaceSymbol> INamespaceSymbol.ConstituentNamespaces
        {
            get
            {
                return _underlying.ConstituentNamespaces.GetPublicSymbols();
            }
        }

        IEnumerable<INamespaceOrTypeSymbol> INamespaceSymbol.GetMembers()
        {
            foreach (var n in _underlying.GetMembers())
            {
                yield return ((Symbols.NamespaceOrTypeSymbol)n).GetPublicSymbol();
            }
        }

        IEnumerable<INamespaceOrTypeSymbol> INamespaceSymbol.GetMembers(string name)
        {
            foreach (var n in _underlying.GetMembers(name))
            {
                yield return ((Symbols.NamespaceOrTypeSymbol)n).GetPublicSymbol();
            }
        }

        IEnumerable<INamespaceSymbol> INamespaceSymbol.GetNamespaceMembers()
        {
            foreach (var n in _underlying.GetNamespaceMembers())
            {
                yield return n.GetPublicSymbol();
            }
        }

        #region ISymbol Members

        protected override void Accept(SymbolVisitor visitor)
        {
            visitor.VisitNamespace(this);
        }

        protected override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
            return visitor.VisitNamespace(this);
        }

        protected override TResult Accept<TArgument, TResult>(SymbolVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNamespace(this, argument);
        }

        #endregion
    }
}
