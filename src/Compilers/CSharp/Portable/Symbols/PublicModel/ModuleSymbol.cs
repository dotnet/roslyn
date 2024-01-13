// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.PublicModel
{
    internal sealed class ModuleSymbol : Symbol, IModuleSymbol
    {
        private readonly Symbols.ModuleSymbol _underlying;

        public ModuleSymbol(Symbols.ModuleSymbol underlying)
        {
            Debug.Assert(underlying is object);
            _underlying = underlying;
        }

        internal override CSharp.Symbol UnderlyingSymbol => _underlying;

        INamespaceSymbol IModuleSymbol.GlobalNamespace
        {
            get
            {
                return _underlying.GlobalNamespace.GetPublicSymbol();
            }
        }

        INamespaceSymbol IModuleSymbol.GetModuleNamespace(INamespaceSymbol namespaceSymbol)
        {
            return _underlying.GetModuleNamespace(namespaceSymbol).GetPublicSymbol();
        }

        ImmutableArray<IAssemblySymbol> IModuleSymbol.ReferencedAssemblySymbols
        {
            get
            {
                return _underlying.ReferencedAssemblySymbols.GetPublicSymbols();
            }
        }

        ImmutableArray<AssemblyIdentity> IModuleSymbol.ReferencedAssemblies => _underlying.ReferencedAssemblies;

        ModuleMetadata IModuleSymbol.GetMetadata() => _underlying.GetMetadata();

        #region ISymbol Members

        protected override void Accept(SymbolVisitor visitor)
        {
            visitor.VisitModule(this);
        }

        protected override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
            return visitor.VisitModule(this);
        }

        protected override TResult Accept<TArgument, TResult>(SymbolVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitModule(this, argument);
        }

        #endregion
    }
}
