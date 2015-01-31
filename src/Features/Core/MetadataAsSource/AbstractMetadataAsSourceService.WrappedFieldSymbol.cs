// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.DocumentationCommentFormatting;

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

            public new IFieldSymbol OriginalDefinition
            {
                get
                {
                    return _symbol.OriginalDefinition;
                }
            }

            public ISymbol AssociatedSymbol
            {
                get
                {
                    return _symbol.AssociatedSymbol;
                }
            }

            public object ConstantValue
            {
                get
                {
                    return _symbol.ConstantValue;
                }
            }

            public ImmutableArray<CustomModifier> CustomModifiers
            {
                get
                {
                    return _symbol.CustomModifiers;
                }
            }

            public bool HasConstantValue
            {
                get
                {
                    return _symbol.HasConstantValue;
                }
            }

            public bool IsConst
            {
                get
                {
                    return _symbol.IsConst;
                }
            }

            public bool IsReadOnly
            {
                get
                {
                    return _symbol.IsReadOnly;
                }
            }

            public bool IsVolatile
            {
                get
                {
                    return _symbol.IsVolatile;
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
