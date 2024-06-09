// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CodeGen
{
    /// <summary>
    /// When erasing an extension type E with underlying type U,
    /// we need to emit `[ExtensionErasure("E")] U`.
    /// The serialized type encoding the un-erased extension type is
    /// similar to a `typeof` expression in an attribute but adds
    /// support for type parameter references.
    /// For example: `[ExtensionErasure("E'1[!0]")]`.
    /// When we synthesize the attribute, we use a `TypedConstant` with type `string`
    /// but holding a `TypeSymbol` as the value. The serialization to string is delayed
    /// so that we can track the references from this un-erased type.
    ///
    /// MetadataSerializedType represents this information in the CCI model.
    /// </summary>
    internal sealed class MetadataSerializedType : Cci.IMetadataExpression
    {
        private readonly Cci.ITypeReference _typeToGet;
        private readonly Cci.ITypeReference _systemString;

        public MetadataSerializedType(Cci.ITypeReference typeToGet, Cci.ITypeReference systemString)
        {
            _typeToGet = typeToGet;
            _systemString = systemString;
        }

        /// <summary>
        /// The type that will be represented by the System.Type instance.
        /// </summary>
        public Cci.ITypeReference TypeToGet
        {
            get
            {
                return _typeToGet;
            }
        }

        void Cci.IMetadataExpression.Dispatch(Cci.MetadataVisitor visitor)
        {
            visitor.Visit(this);
        }

        Cci.ITypeReference Cci.IMetadataExpression.Type
        {
            get { return _systemString; }
        }
    }
}
