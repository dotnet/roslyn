// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.DocumentationComments;

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
                    return CanImplementImplicitly
                        ? ImmutableArray.Create<IPropertySymbol>()
                        : _symbol.ExplicitInterfaceImplementations;
                }
            }

            public IMethodSymbol? GetMethod => _symbol.GetMethod;

            public bool IsIndexer => _symbol.IsIndexer;

            public bool IsReadOnly => _symbol.IsReadOnly;

            public bool IsWithEvents => _symbol.IsWithEvents;

            public bool IsWriteOnly => _symbol.IsWriteOnly;

            public bool ReturnsByRef => _symbol.ReturnsByRef;

            public bool ReturnsByRefReadonly => _symbol.ReturnsByRefReadonly;

            public RefKind RefKind => _symbol.RefKind;

            public IPropertySymbol? OverriddenProperty => _symbol.OverriddenProperty;

            public ImmutableArray<IParameterSymbol> Parameters => _symbol.Parameters;

            public IMethodSymbol? SetMethod => _symbol.SetMethod;

            public ITypeSymbol Type => _symbol.Type;

            public NullableAnnotation NullableAnnotation => _symbol.NullableAnnotation;

            public ImmutableArray<CustomModifier> RefCustomModifiers => _symbol.RefCustomModifiers;

            public ImmutableArray<CustomModifier> TypeCustomModifiers => _symbol.TypeCustomModifiers;

            ISymbol ISymbol.OriginalDefinition => _symbol.OriginalDefinition;

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
