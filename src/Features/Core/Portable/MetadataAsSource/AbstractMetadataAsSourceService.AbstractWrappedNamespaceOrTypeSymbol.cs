// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.DocumentationComments;

namespace Microsoft.CodeAnalysis.MetadataAsSource;

internal partial class AbstractMetadataAsSourceService
{
    private abstract class AbstractWrappedNamespaceOrTypeSymbol : AbstractWrappedSymbol, INamespaceOrTypeSymbol
    {
        private readonly INamespaceOrTypeSymbol _symbol;

        protected AbstractWrappedNamespaceOrTypeSymbol(INamespaceOrTypeSymbol symbol, bool canImplementImplicitly, IDocumentationCommentFormattingService docCommentFormattingService)
            : base(symbol, canImplementImplicitly, docCommentFormattingService)
        {
            _symbol = symbol;
        }

        public abstract ImmutableArray<ISymbol> GetMembers();
        public abstract ImmutableArray<ISymbol> GetMembers(string name);
        public abstract ImmutableArray<INamedTypeSymbol> GetTypeMembers();
        public abstract ImmutableArray<INamedTypeSymbol> GetTypeMembers(string name);
        public abstract ImmutableArray<INamedTypeSymbol> GetTypeMembers(string name, int arity);

        public bool IsNamespace => _symbol.IsNamespace;

        public bool IsType => _symbol.IsType;
    }
}
