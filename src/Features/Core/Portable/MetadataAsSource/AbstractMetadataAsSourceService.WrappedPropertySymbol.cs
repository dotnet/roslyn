// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.DocumentationComments;

namespace Microsoft.CodeAnalysis.MetadataAsSource;

internal abstract partial class AbstractMetadataAsSourceService
{
    private sealed class WrappedPropertySymbol(IPropertySymbol propertySymbol, bool canImplementImplicitly, IDocumentationCommentFormattingService docCommentFormattingService) : AbstractWrappedSymbol(propertySymbol, canImplementImplicitly, docCommentFormattingService), IPropertySymbol
    {
        private readonly IPropertySymbol _symbol = propertySymbol;

        public ImmutableArray<IPropertySymbol> ExplicitInterfaceImplementations
        {
            get
            {
                return CanImplementImplicitly
                    ? []
                    : _symbol.ExplicitInterfaceImplementations;
            }
        }

        public IMethodSymbol GetMethod => _symbol.GetMethod;

        public bool IsIndexer => _symbol.IsIndexer;

        public bool IsReadOnly => _symbol.IsReadOnly;

        public bool IsWithEvents => _symbol.IsWithEvents;

        public bool IsWriteOnly => _symbol.IsWriteOnly;

        public bool IsRequired => _symbol.IsRequired;

        public bool ReturnsByRef => _symbol.ReturnsByRef;

        public bool ReturnsByRefReadonly => _symbol.ReturnsByRefReadonly;

        public RefKind RefKind => _symbol.RefKind;

        public IPropertySymbol OverriddenProperty => _symbol.OverriddenProperty;

        public ImmutableArray<IParameterSymbol> Parameters => _symbol.Parameters;

        public IMethodSymbol SetMethod => _symbol.SetMethod;

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

        public IPropertySymbol PartialDefinitionPart => _symbol.PartialDefinitionPart;

        public IPropertySymbol PartialImplementationPart => _symbol.PartialImplementationPart;

        public bool IsPartialDefinition => _symbol.IsPartialDefinition;
    }
}
