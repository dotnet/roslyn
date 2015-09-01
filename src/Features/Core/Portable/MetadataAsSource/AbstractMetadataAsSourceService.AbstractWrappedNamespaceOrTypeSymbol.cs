// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.DocumentationCommentFormatting;

namespace Microsoft.CodeAnalysis.MetadataAsSource
{
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

            public bool IsNamespace
            {
                get
                {
                    return _symbol.IsNamespace;
                }
            }

            public bool IsType
            {
                get
                {
                    return _symbol.IsType;
                }
            }
        }
    }
}
