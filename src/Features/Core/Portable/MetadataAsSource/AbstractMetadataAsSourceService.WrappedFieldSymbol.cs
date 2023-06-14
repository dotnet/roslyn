// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.DocumentationComments;

namespace Microsoft.CodeAnalysis.MetadataAsSource
{
    internal partial class AbstractMetadataAsSourceService
    {
        private class WrappedFieldSymbol(IFieldSymbol fieldSymbol, IDocumentationCommentFormattingService docCommentFormattingService) : AbstractWrappedSymbol(fieldSymbol, canImplementImplicitly: false, docCommentFormattingService: docCommentFormattingService), IFieldSymbol
        {
            public new IFieldSymbol OriginalDefinition => fieldSymbol.OriginalDefinition;

            public IFieldSymbol CorrespondingTupleField => null;

            public ISymbol AssociatedSymbol => fieldSymbol.AssociatedSymbol;

            public object ConstantValue => fieldSymbol.ConstantValue;

            public RefKind RefKind => fieldSymbol.RefKind;

            public ImmutableArray<CustomModifier> RefCustomModifiers => fieldSymbol.RefCustomModifiers;

            public ImmutableArray<CustomModifier> CustomModifiers => fieldSymbol.CustomModifiers;

            public bool HasConstantValue => fieldSymbol.HasConstantValue;

            public bool IsConst => fieldSymbol.IsConst;

            public bool IsReadOnly => fieldSymbol.IsReadOnly;

            public bool IsVolatile => fieldSymbol.IsVolatile;

            public bool IsRequired => fieldSymbol.IsRequired;

            public bool IsFixedSizeBuffer => fieldSymbol.IsFixedSizeBuffer;

            public int FixedSize => fieldSymbol.FixedSize;

            public ITypeSymbol Type => fieldSymbol.Type;

            public NullableAnnotation NullableAnnotation => fieldSymbol.NullableAnnotation;

            public bool IsExplicitlyNamedTupleElement => fieldSymbol.IsExplicitlyNamedTupleElement;
        }
    }
}
