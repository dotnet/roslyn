// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.DocumentationComments;

namespace Microsoft.CodeAnalysis.MetadataAsSource
{
    internal partial class AbstractMetadataAsSourceService
    {
        private class WrappedEventSymbol(IEventSymbol eventSymbol, bool canImplementImplicitly, IDocumentationCommentFormattingService docCommentFormattingService) : AbstractWrappedSymbol(eventSymbol, canImplementImplicitly, docCommentFormattingService), IEventSymbol
        {
            public ImmutableArray<IEventSymbol> ExplicitInterfaceImplementations
            {
                get
                {
                    return CanImplementImplicitly
                        ? ImmutableArray.Create<IEventSymbol>()
                        : eventSymbol.ExplicitInterfaceImplementations;
                }
            }

            public new IEventSymbol OriginalDefinition => this;

            public IMethodSymbol? AddMethod => eventSymbol.AddMethod;
            public bool IsWindowsRuntimeEvent => eventSymbol.IsWindowsRuntimeEvent;
            public IEventSymbol? OverriddenEvent => eventSymbol.OverriddenEvent;
            public IMethodSymbol? RaiseMethod => eventSymbol.RaiseMethod;
            public IMethodSymbol? RemoveMethod => eventSymbol.RemoveMethod;
            public ITypeSymbol Type => eventSymbol.Type;
            public NullableAnnotation NullableAnnotation => eventSymbol.NullableAnnotation;
        }
    }
}
