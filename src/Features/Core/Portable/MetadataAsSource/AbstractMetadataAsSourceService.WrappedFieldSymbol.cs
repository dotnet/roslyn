// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.DocumentationComments;

namespace Microsoft.CodeAnalysis.MetadataAsSource;

internal partial class AbstractMetadataAsSourceService
{
    private class WrappedFieldSymbol(IFieldSymbol fieldSymbol, IDocumentationCommentFormattingService docCommentFormattingService) : AbstractWrappedSymbol(fieldSymbol, canImplementImplicitly: false, docCommentFormattingService: docCommentFormattingService), IFieldSymbol
    {
        private readonly IFieldSymbol _symbol = fieldSymbol;

        public new IFieldSymbol OriginalDefinition => _symbol.OriginalDefinition;

        public IFieldSymbol CorrespondingTupleField => null;

        public ISymbol AssociatedSymbol => _symbol.AssociatedSymbol;

        public object ConstantValue => _symbol.ConstantValue;

        public RefKind RefKind => _symbol.RefKind;

        public ImmutableArray<CustomModifier> RefCustomModifiers => _symbol.RefCustomModifiers;

        public ImmutableArray<CustomModifier> CustomModifiers => _symbol.CustomModifiers;

        public bool HasConstantValue => _symbol.HasConstantValue;

        public bool IsConst => _symbol.IsConst;

        public bool IsReadOnly => _symbol.IsReadOnly;

        public bool IsVolatile => _symbol.IsVolatile;

        public bool IsRequired => _symbol.IsRequired;

        public bool IsFixedSizeBuffer => _symbol.IsFixedSizeBuffer;

        public int FixedSize => _symbol.FixedSize;

        public ITypeSymbol Type => _symbol.Type;

        public NullableAnnotation NullableAnnotation => _symbol.NullableAnnotation;

        public bool IsExplicitlyNamedTupleElement => _symbol.IsExplicitlyNamedTupleElement;
    }
}
