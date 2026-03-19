// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CodeGen
{
    /// <summary>
    /// An expression that results in a System.Type instance.
    /// </summary>
    internal sealed class MetadataTypeOf : Cci.IMetadataExpression
    {
        private readonly Cci.ITypeReference _typeToGet;
        private readonly Cci.ITypeReference _systemType;

        public MetadataTypeOf(Cci.ITypeReference typeToGet, Cci.ITypeReference systemType)
        {
            _typeToGet = typeToGet;
            _systemType = systemType;
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
            get { return _systemType; }
        }
    }
}
