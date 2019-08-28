// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.DocumentationComments;

namespace Microsoft.CodeAnalysis.MetadataAsSource
{
    internal partial class AbstractMetadataAsSourceService
    {
        private class WrappedEventSymbol : AbstractWrappedSymbol, IEventSymbol
        {
            private readonly IEventSymbol _symbol;

            public WrappedEventSymbol(IEventSymbol eventSymbol, bool canImplementImplicitly, IDocumentationCommentFormattingService docCommentFormattingService)
                : base(eventSymbol, canImplementImplicitly, docCommentFormattingService)
            {
                _symbol = eventSymbol;
            }

            public ImmutableArray<IEventSymbol> ExplicitInterfaceImplementations
            {
                get
                {
                    return CanImplementImplicitly
                        ? ImmutableArray.Create<IEventSymbol>()
                        : _symbol.ExplicitInterfaceImplementations;
                }
            }

            public new IEventSymbol OriginalDefinition => this;

            public IMethodSymbol AddMethod => _symbol.AddMethod;
            public bool IsWindowsRuntimeEvent => _symbol.IsWindowsRuntimeEvent;
            public IEventSymbol OverriddenEvent => _symbol.OverriddenEvent;
            public IMethodSymbol RaiseMethod => _symbol.RaiseMethod;
            public IMethodSymbol RemoveMethod => _symbol.RemoveMethod;
            public ITypeSymbol Type => _symbol.Type;
            public NullableAnnotation NullableAnnotation => _symbol.NullableAnnotation;
        }
    }
}
