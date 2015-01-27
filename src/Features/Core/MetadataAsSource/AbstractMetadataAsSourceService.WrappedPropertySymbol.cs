// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.DocumentationCommentFormatting;

namespace Microsoft.CodeAnalysis.MetadataAsSource
{
    internal partial class AbstractMetadataAsSourceService
    {
        private class WrappedPropertySymbol : AbstractWrappedSymbol, IPropertySymbol
        {
            private IPropertySymbol symbol;

            public WrappedPropertySymbol(IPropertySymbol propertySymbol, bool canImplementImplicitly, IDocumentationCommentFormattingService docCommentFormattingService)
                : base(propertySymbol, canImplementImplicitly, docCommentFormattingService)
            {
                this.symbol = propertySymbol;
            }

            public ImmutableArray<IPropertySymbol> ExplicitInterfaceImplementations
            {
                get
                {
                    return this.CanImplementImplicitly
                        ? ImmutableArray.Create<IPropertySymbol>()
                        : this.symbol.ExplicitInterfaceImplementations;
                }
            }

            public IMethodSymbol GetMethod
            {
                get
                {
                    return this.symbol.GetMethod;
                }
            }

            public bool IsIndexer
            {
                get
                {
                    return this.symbol.IsIndexer;
                }
            }

            public bool IsReadOnly
            {
                get
                {
                    return this.symbol.IsReadOnly;
                }
            }

            public bool IsWithEvents
            {
                get
                {
                    return this.symbol.IsWithEvents;
                }
            }

            public bool IsWriteOnly
            {
                get
                {
                    return this.symbol.IsWriteOnly;
                }
            }

            public IPropertySymbol OverriddenProperty
            {
                get
                {
                    return this.symbol.OverriddenProperty;
                }
            }

            public ImmutableArray<IParameterSymbol> Parameters
            {
                get
                {
                    return this.symbol.Parameters;
                }
            }

            public IMethodSymbol SetMethod
            {
                get
                {
                    return this.symbol.SetMethod;
                }
            }

            public ITypeSymbol Type
            {
                get
                {
                    return this.symbol.Type;
                }
            }

            public ImmutableArray<CustomModifier> TypeCustomModifiers
            {
                get
                {
                    return this.symbol.TypeCustomModifiers;
                }
            }

            ISymbol ISymbol.OriginalDefinition
            {
                get
                {
                    return this.symbol.OriginalDefinition;
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
