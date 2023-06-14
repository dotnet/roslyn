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
        private class WrappedPropertySymbol(IPropertySymbol propertySymbol, bool canImplementImplicitly, IDocumentationCommentFormattingService docCommentFormattingService) : AbstractWrappedSymbol(propertySymbol, canImplementImplicitly, docCommentFormattingService), IPropertySymbol
        {
            public ImmutableArray<IPropertySymbol> ExplicitInterfaceImplementations
            {
                get
                {
                    return CanImplementImplicitly
                        ? ImmutableArray.Create<IPropertySymbol>()
                        : propertySymbol.ExplicitInterfaceImplementations;
                }
            }

            public IMethodSymbol GetMethod => propertySymbol.GetMethod;

            public bool IsIndexer => propertySymbol.IsIndexer;

            public bool IsReadOnly => propertySymbol.IsReadOnly;

            public bool IsWithEvents => propertySymbol.IsWithEvents;

            public bool IsWriteOnly => propertySymbol.IsWriteOnly;

            public bool IsRequired => propertySymbol.IsRequired;

            public bool ReturnsByRef => propertySymbol.ReturnsByRef;

            public bool ReturnsByRefReadonly => propertySymbol.ReturnsByRefReadonly;

            public RefKind RefKind => propertySymbol.RefKind;

            public IPropertySymbol OverriddenProperty => propertySymbol.OverriddenProperty;

            public ImmutableArray<IParameterSymbol> Parameters => propertySymbol.Parameters;

            public IMethodSymbol SetMethod => propertySymbol.SetMethod;

            public ITypeSymbol Type => propertySymbol.Type;

            public NullableAnnotation NullableAnnotation => propertySymbol.NullableAnnotation;

            public ImmutableArray<CustomModifier> RefCustomModifiers => propertySymbol.RefCustomModifiers;

            public ImmutableArray<CustomModifier> TypeCustomModifiers => propertySymbol.TypeCustomModifiers;

            ISymbol ISymbol.OriginalDefinition => propertySymbol.OriginalDefinition;

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
