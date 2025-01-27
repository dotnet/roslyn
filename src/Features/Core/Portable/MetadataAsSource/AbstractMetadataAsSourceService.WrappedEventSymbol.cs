// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.DocumentationComments;

namespace Microsoft.CodeAnalysis.MetadataAsSource;

internal abstract partial class AbstractMetadataAsSourceService
{
    private sealed class WrappedEventSymbol(IEventSymbol eventSymbol, bool canImplementImplicitly, IDocumentationCommentFormattingService docCommentFormattingService) : AbstractWrappedSymbol(eventSymbol, canImplementImplicitly, docCommentFormattingService), IEventSymbol
    {
        private readonly IEventSymbol _symbol = eventSymbol;

        public ImmutableArray<IEventSymbol> ExplicitInterfaceImplementations
        {
            get
            {
                return CanImplementImplicitly
                    ? []
                    : _symbol.ExplicitInterfaceImplementations;
            }
        }

        public new IEventSymbol OriginalDefinition => this;

        public IMethodSymbol? AddMethod => _symbol.AddMethod;
        public bool IsWindowsRuntimeEvent => _symbol.IsWindowsRuntimeEvent;
        public IEventSymbol? OverriddenEvent => _symbol.OverriddenEvent;
        public IMethodSymbol? RaiseMethod => _symbol.RaiseMethod;
        public IMethodSymbol? RemoveMethod => _symbol.RemoveMethod;
        public ITypeSymbol Type => _symbol.Type;
        public NullableAnnotation NullableAnnotation => _symbol.NullableAnnotation;
    }
}
