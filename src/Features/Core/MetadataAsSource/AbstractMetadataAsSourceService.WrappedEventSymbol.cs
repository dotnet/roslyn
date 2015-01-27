// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.DocumentationCommentFormatting;

namespace Microsoft.CodeAnalysis.MetadataAsSource
{
    internal partial class AbstractMetadataAsSourceService
    {
        private class WrappedEventSymbol : AbstractWrappedSymbol, IEventSymbol
        {
            private readonly IEventSymbol symbol;

            public WrappedEventSymbol(IEventSymbol eventSymbol, bool canImplementImplicitly, IDocumentationCommentFormattingService docCommentFormattingService)
                : base(eventSymbol, canImplementImplicitly, docCommentFormattingService)
            {
                this.symbol = eventSymbol;
            }

            public IMethodSymbol AddMethod
            {
                get
                {
                    return this.symbol.AddMethod;
                }
            }

            public ImmutableArray<IEventSymbol> ExplicitInterfaceImplementations
            {
                get
                {
                    return this.CanImplementImplicitly
                        ? ImmutableArray.Create<IEventSymbol>()
                        : this.symbol.ExplicitInterfaceImplementations;
                }
            }

            public bool IsWindowsRuntimeEvent
            {
                get
                {
                    return this.symbol.IsWindowsRuntimeEvent;
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
                    return this.symbol.OverriddenEvent;
                }
            }

            public IMethodSymbol RaiseMethod
            {
                get
                {
                    return this.symbol.RaiseMethod;
                }
            }

            public IMethodSymbol RemoveMethod
            {
                get
                {
                    return this.symbol.RemoveMethod;
                }
            }

            public ITypeSymbol Type
            {
                get
                {
                    return this.symbol.Type;
                }
            }
        }
    }
}
