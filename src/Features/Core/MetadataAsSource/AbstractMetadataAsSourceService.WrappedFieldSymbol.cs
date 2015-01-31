// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.DocumentationCommentFormatting;

namespace Microsoft.CodeAnalysis.MetadataAsSource
{
    internal partial class AbstractMetadataAsSourceService
    {
        private class WrappedFieldSymbol : AbstractWrappedSymbol, IFieldSymbol
        {
            private readonly IFieldSymbol symbol;

            public WrappedFieldSymbol(IFieldSymbol fieldSymbol, IDocumentationCommentFormattingService docCommentFormattingService)
                : base(fieldSymbol, canImplementImplicitly: false, docCommentFormattingService: docCommentFormattingService)
            {
                this.symbol = fieldSymbol;
            }

            public new IFieldSymbol OriginalDefinition
            {
                get
                {
                    return this.symbol.OriginalDefinition;
                }
            }

            public ISymbol AssociatedSymbol
            {
                get
                {
                    return this.symbol.AssociatedSymbol;
                }
            }

            public object ConstantValue
            {
                get
                {
                    return this.symbol.ConstantValue;
                }
            }

            public ImmutableArray<CustomModifier> CustomModifiers
            {
                get
                {
                    return this.symbol.CustomModifiers;
                }
            }

            public bool HasConstantValue
            {
                get
                {
                    return this.symbol.HasConstantValue;
                }
            }

            public bool IsConst
            {
                get
                {
                    return this.symbol.IsConst;
                }
            }

            public bool IsReadOnly
            {
                get
                {
                    return this.symbol.IsReadOnly;
                }
            }

            public bool IsVolatile
            {
                get
                {
                    return this.symbol.IsVolatile;
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
