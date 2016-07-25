// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// HierarchicalChecksumObject indicates this type is collection of checksumObject types.
    /// 
    /// <see cref="Asset"/> represents actual data (leaf node of hierarchical checksum tree) 
    /// </summary>
    internal abstract class HierarchicalChecksumObject : ChecksumObject
    {
        protected readonly Serializer Serializer;

        public HierarchicalChecksumObject(Serializer serializer, Checksum checksum, string kind) : base(checksum, kind)
        {
            Serializer = serializer;
        }
    }
}
