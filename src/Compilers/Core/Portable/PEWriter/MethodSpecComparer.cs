// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.Cci
{
    internal sealed class MethodSpecComparer : IEqualityComparer<IGenericMethodInstanceReference>
    {
        private readonly MetadataWriter _metadataWriter;

        internal MethodSpecComparer(MetadataWriter metadataWriter)
        {
            _metadataWriter = metadataWriter;
        }

        public bool Equals(IGenericMethodInstanceReference x, IGenericMethodInstanceReference y)
        {
            if (x == y)
            {
                return true;
            }

            return
                _metadataWriter.GetMethodDefOrRefCodedIndex(x.GetGenericMethod(_metadataWriter.Context)) == _metadataWriter.GetMethodDefOrRefCodedIndex(y.GetGenericMethod(_metadataWriter.Context)) &&
                _metadataWriter.GetMethodInstanceSignatureIndex(x) == _metadataWriter.GetMethodInstanceSignatureIndex(y);
        }

        public int GetHashCode(IGenericMethodInstanceReference methodInstanceReference)
        {
            return Hash.Combine(
                (int)_metadataWriter.GetMethodDefOrRefCodedIndex(methodInstanceReference.GetGenericMethod(_metadataWriter.Context)),
                _metadataWriter.GetMethodInstanceSignatureIndex(methodInstanceReference).GetHashCode());
        }
    }
}
