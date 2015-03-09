// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Cci;

namespace Microsoft.CodeAnalysis.CodeGen
{
    /// <summary>
    /// An expression that results in a System.Type instance.
    /// </summary>
    internal sealed class MetadataTypeOf : IMetadataTypeOf
    {
        private readonly ITypeReference _systemType;

        public MetadataTypeOf(ITypeReference typeToGet, ITypeReference systemType)
        {
            TypeToGet = typeToGet;
            _systemType = systemType;
        }

        /// <summary>
        /// The type that will be represented by the System.Type instance.
        /// </summary>
        public ITypeReference TypeToGet { get; }

        void IMetadataExpression.Dispatch(MetadataVisitor visitor)
        {
            visitor.Visit(this);
        }

        ITypeReference IMetadataExpression.Type
        {
            get { return _systemType; }
        }
    }
}
