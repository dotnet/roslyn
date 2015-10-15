// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.DocumentationCommentFormatting;

namespace Microsoft.CodeAnalysis.MetadataAsSource
{
    internal partial class AbstractMetadataAsSourceService
    {
        private class WrappedPropertySymbol : AbstractWrappedSymbol, IPropertySymbol
        {
            private readonly IPropertySymbol _symbol;

            public WrappedPropertySymbol(IPropertySymbol propertySymbol, bool canImplementImplicitly, IDocumentationCommentFormattingService docCommentFormattingService)
                : base(propertySymbol, canImplementImplicitly, docCommentFormattingService)
            {
                _symbol = propertySymbol;
            }

            public ImmutableArray<IPropertySymbol> ExplicitInterfaceImplementations
            {
                get
                {
                    return this.CanImplementImplicitly
                        ? ImmutableArray.Create<IPropertySymbol>()
                        : _symbol.ExplicitInterfaceImplementations;
                }
            }

            public IMethodSymbol GetMethod
            {
                get
                {
                    return _symbol.GetMethod;
                }
            }

            public bool IsIndexer
            {
                get
                {
                    return _symbol.IsIndexer;
                }
            }

            public bool IsReadOnly
            {
                get
                {
                    return _symbol.IsReadOnly;
                }
            }

            public bool IsWithEvents
            {
                get
                {
                    return _symbol.IsWithEvents;
                }
            }

            public bool IsWriteOnly
            {
                get
                {
                    return _symbol.IsWriteOnly;
                }
            }

            public IPropertySymbol OverriddenProperty
            {
                get
                {
                    return _symbol.OverriddenProperty;
                }
            }

            public ImmutableArray<IParameterSymbol> Parameters
            {
                get
                {
                    return _symbol.Parameters;
                }
            }

            public IMethodSymbol SetMethod
            {
                get
                {
                    return _symbol.SetMethod;
                }
            }

            public ITypeSymbol Type
            {
                get
                {
                    return _symbol.Type;
                }
            }

            public ImmutableArray<CustomModifier> TypeCustomModifiers
            {
                get
                {
                    return _symbol.TypeCustomModifiers;
                }
            }

            ISymbol ISymbol.OriginalDefinition
            {
                get
                {
                    return _symbol.OriginalDefinition;
                }
            }

            public new IPropertySymbol OriginalDefinition
            {
                get
                {
                    return this;
                }
            }
        }
    }
}
