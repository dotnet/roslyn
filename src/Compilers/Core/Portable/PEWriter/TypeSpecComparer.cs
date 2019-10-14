// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;

namespace Microsoft.Cci
{
    internal sealed class TypeSpecComparer : IEqualityComparer<ITypeReference>
    {
        private readonly MetadataWriter _metadataWriter;

        internal TypeSpecComparer(MetadataWriter metadataWriter)
        {
            _metadataWriter = metadataWriter;
        }

        public bool Equals(ITypeReference x, ITypeReference y)
        {
            return x == y || _metadataWriter.GetTypeSpecSignatureIndex(x).Equals(_metadataWriter.GetTypeSpecSignatureIndex(y));
        }

        public int GetHashCode(ITypeReference typeReference)
        {
            return _metadataWriter.GetTypeSpecSignatureIndex(typeReference).GetHashCode();
        }
    }
}
