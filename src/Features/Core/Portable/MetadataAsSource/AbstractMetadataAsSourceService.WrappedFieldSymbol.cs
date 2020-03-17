// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.DocumentationComments;

namespace Microsoft.CodeAnalysis.MetadataAsSource
{
    internal partial class AbstractMetadataAsSourceService
    {
        private class WrappedFieldSymbol : AbstractWrappedSymbol, IFieldSymbol
        {
            private readonly IFieldSymbol _symbol;

            public WrappedFieldSymbol(IFieldSymbol fieldSymbol, IDocumentationCommentFormattingService docCommentFormattingService)
                : base(fieldSymbol, canImplementImplicitly: false, docCommentFormattingService: docCommentFormattingService)
            {
                _symbol = fieldSymbol;
            }

            public new IFieldSymbol OriginalDefinition => _symbol.OriginalDefinition;

            public IFieldSymbol CorrespondingTupleField => null;

            public ISymbol AssociatedSymbol => _symbol.AssociatedSymbol;

            public object ConstantValue => _symbol.ConstantValue;

            public ImmutableArray<CustomModifier> CustomModifiers => _symbol.CustomModifiers;

            public bool HasConstantValue => _symbol.HasConstantValue;

            public bool IsConst => _symbol.IsConst;

            public bool IsReadOnly => _symbol.IsReadOnly;

            public bool IsVolatile => _symbol.IsVolatile;

            public bool IsFixedSizeBuffer => _symbol.IsFixedSizeBuffer;

            public ITypeSymbol Type => _symbol.Type;

            public NullableAnnotation NullableAnnotation => _symbol.NullableAnnotation;
        }
    }
}
