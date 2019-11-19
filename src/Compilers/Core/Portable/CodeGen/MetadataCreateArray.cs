// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;
using Cci = Microsoft.Cci;

namespace Microsoft.CodeAnalysis.CodeGen
{
    /// <summary>
    /// An expression that creates an array instance in metadata. Only for use in custom attributes.
    /// </summary>
    internal sealed class MetadataCreateArray : Cci.IMetadataExpression
    {
        public Cci.IArrayTypeReference ArrayType { get; }
        public Cci.ITypeReference ElementType { get; }
        public ImmutableArray<Cci.IMetadataExpression> Elements { get; }

        public MetadataCreateArray(Cci.IArrayTypeReference arrayType, Cci.ITypeReference elementType, ImmutableArray<Cci.IMetadataExpression> initializers)
        {
            ArrayType = arrayType;
            ElementType = elementType;
            Elements = initializers;
        }

        Cci.ITypeReference Cci.IMetadataExpression.Type => ArrayType;
        void Cci.IMetadataExpression.Dispatch(Cci.MetadataVisitor visitor) => visitor.Visit(this);
    }
}
