// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;
using Cci = Microsoft.Cci;

namespace Microsoft.CodeAnalysis.CodeGen
{
    /// <summary>
    /// An expression that creates an array instance in metadata. Only for use in custom attributes.
    /// </summary>
    internal sealed class MetadataCreateArray : Cci.IMetadataCreateArray
    {
        private readonly Cci.IArrayTypeReference arrayType;
        private readonly Cci.ITypeReference elementType;
        private ImmutableArray<Cci.IMetadataExpression> initializers;

        public MetadataCreateArray(Cci.IArrayTypeReference arrayType, Cci.ITypeReference elementType, ImmutableArray<Cci.IMetadataExpression> initializers)
        {
            this.arrayType = arrayType;
            this.elementType = elementType;
            this.initializers = initializers;
        }

        /// <summary>
        /// The element type of the array.
        /// </summary>
        Cci.ITypeReference Cci.IMetadataCreateArray.ElementType
        {
            get
            {
                return this.elementType;
            }
        }

        uint Cci.IMetadataCreateArray.ElementCount
        {
            get
            {
                return (uint)this.initializers.Length;
            }
        }

        /// <summary>
        /// The initial values of the array elements. May be empty.
        /// </summary>
        IEnumerable<Cci.IMetadataExpression> Cci.IMetadataCreateArray.Elements
        {
            get
            {
                return this.initializers;
            }
        }

        void Cci.IMetadataExpression.Dispatch(Cci.MetadataVisitor visitor)
        {
            visitor.Visit(this);
        }

        Cci.ITypeReference Cci.IMetadataExpression.Type
        {
            get { return this.arrayType; }
        }
    }
}
