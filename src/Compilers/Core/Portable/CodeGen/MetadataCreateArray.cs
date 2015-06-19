// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly Cci.IArrayTypeReference _arrayType;
        private readonly Cci.ITypeReference _elementType;
        private ImmutableArray<Cci.IMetadataExpression> _initializers;

        public MetadataCreateArray(Cci.IArrayTypeReference arrayType, Cci.ITypeReference elementType, ImmutableArray<Cci.IMetadataExpression> initializers)
        {
            _arrayType = arrayType;
            _elementType = elementType;
            _initializers = initializers;
        }

        /// <summary>
        /// The element type of the array.
        /// </summary>
        Cci.ITypeReference Cci.IMetadataCreateArray.ElementType => _elementType;

        uint Cci.IMetadataCreateArray.ElementCount => (uint)_initializers.Length;

        /// <summary>
        /// The initial values of the array elements. May be empty.
        /// </summary>
        IEnumerable<Cci.IMetadataExpression> Cci.IMetadataCreateArray.Elements => _initializers;

        void Cci.IMetadataExpression.Dispatch(Cci.MetadataVisitor visitor)
        {
            visitor.Visit(this);
        }

        Cci.ITypeReference Cci.IMetadataExpression.Type => _arrayType;
    }
}
