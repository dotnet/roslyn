// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal partial class
#if DEBUG
        NamespaceSymbolAdapter : SymbolAdapter,
#else
        NamespaceSymbol :
#endif 
        Cci.INamespace
    {
        Cci.INamespace Cci.INamespace.ContainingNamespace => AdaptedNamespaceSymbol.ContainingNamespace?.GetCciAdapter();

        string Cci.INamedEntity.Name => AdaptedNamespaceSymbol.MetadataName;

        CodeAnalysis.Symbols.INamespaceSymbolInternal Cci.INamespace.GetInternalSymbol() => AdaptedNamespaceSymbol;
    }

    internal partial class NamespaceSymbol
    {
#if DEBUG
        private NamespaceSymbolAdapter _lazyAdapter;

        protected sealed override SymbolAdapter GetCciAdapterImpl() => GetCciAdapter();
        internal new NamespaceSymbolAdapter GetCciAdapter()
        {
            if (_lazyAdapter is null)
            {
                return InterlockedOperations.Initialize(ref _lazyAdapter, new NamespaceSymbolAdapter(this));
            }

            return _lazyAdapter;
        }
#else
        internal NamespaceSymbol AdaptedNamespaceSymbol => this;

        internal new NamespaceSymbol GetCciAdapter()
        {
            return this;
        }
#endif 
    }

#if DEBUG
    internal partial class NamespaceSymbolAdapter
    {
        internal NamespaceSymbolAdapter(NamespaceSymbol underlyingNamespaceSymbol)
        {
            AdaptedNamespaceSymbol = underlyingNamespaceSymbol;
        }

        internal sealed override Symbol AdaptedSymbol => AdaptedNamespaceSymbol;
        internal NamespaceSymbol AdaptedNamespaceSymbol { get; }
    }
#endif
}
