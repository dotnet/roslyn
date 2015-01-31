// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.DocumentationCommentFormatting;

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

            public IMethodSymbol AddMethod
            {
                get
                {
                    return _symbol.AddMethod;
                }
            }

            public ImmutableArray<IEventSymbol> ExplicitInterfaceImplementations
            {
                get
                {
                    return this.CanImplementImplicitly
                        ? ImmutableArray.Create<IEventSymbol>()
                        : _symbol.ExplicitInterfaceImplementations;
                }
            }

            public bool IsWindowsRuntimeEvent
            {
                get
                {
                    return _symbol.IsWindowsRuntimeEvent;
                }
            }

            public new IEventSymbol OriginalDefinition
            {
                get
                {
                    return this;
                }
            }

            public IEventSymbol OverriddenEvent
            {
                get
                {
                    return _symbol.OverriddenEvent;
                }
            }

            public IMethodSymbol RaiseMethod
            {
                get
                {
                    return _symbol.RaiseMethod;
                }
            }

            public IMethodSymbol RemoveMethod
            {
                get
                {
                    return _symbol.RemoveMethod;
                }
            }

            public ITypeSymbol Type
            {
                get
                {
                    return _symbol.Type;
                }
            }
        }
    }
}
