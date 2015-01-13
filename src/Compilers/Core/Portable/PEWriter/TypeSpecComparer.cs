// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Cci
{
    internal sealed class TypeSpecComparer : IEqualityComparer<ITypeReference>
    {
        private readonly MetadataWriter metadataWriter;

        internal TypeSpecComparer(MetadataWriter metadataWriter)
        {
            this.metadataWriter = metadataWriter;
        }

        public bool Equals(ITypeReference x, ITypeReference y)
        {
            return x == y || this.metadataWriter.GetTypeSpecSignatureIndex(x) == this.metadataWriter.GetTypeSpecSignatureIndex(y);
        }

        public int GetHashCode(ITypeReference typeReference)
        {
            return (int)this.metadataWriter.GetTypeSpecSignatureIndex(typeReference);
        }
    }
}