// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

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
