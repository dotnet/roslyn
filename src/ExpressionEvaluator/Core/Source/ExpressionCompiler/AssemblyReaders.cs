// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Reflection.Metadata;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal struct AssemblyReaders
    {
        public readonly MetadataReader MetadataReader;
        public readonly object SymReader;

        public AssemblyReaders(MetadataReader metadataReader, object symReader)
        {
            this.MetadataReader = metadataReader;
            this.SymReader = symReader;
        }
    }
}
