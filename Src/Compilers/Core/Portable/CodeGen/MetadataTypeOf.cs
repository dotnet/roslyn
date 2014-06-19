// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;
using Cci = Microsoft.Cci;

namespace Microsoft.CodeAnalysis.CodeGen
{
    /// <summary>
    /// An expression that results in a System.Type instance.
    /// </summary>
    internal sealed class MetadataTypeOf : Cci.IMetadataTypeOf
    {
        private readonly Cci.ITypeReference typeToGet;
        private readonly Cci.ITypeReference systemType;

        public MetadataTypeOf(Cci.ITypeReference typeToGet, Cci.ITypeReference systemType)
        {
            this.typeToGet = typeToGet;
            this.systemType = systemType;
        }

        /// <summary>
        /// The type that will be represented by the System.Type instance.
        /// </summary>
        public Cci.ITypeReference TypeToGet
        {
            get
            {
                return this.typeToGet;
            }
        }

        void Cci.IMetadataExpression.Dispatch(Cci.MetadataVisitor visitor)
        {
            visitor.Visit(this);
        }

        Cci.ITypeReference Cci.IMetadataExpression.Type
        {
            get { return this.systemType; }
        }
    }
}
