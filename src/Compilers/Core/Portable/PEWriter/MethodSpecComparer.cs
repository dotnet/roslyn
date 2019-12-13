// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

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
                _metadataWriter.GetMethodDefinitionOrReferenceHandle(x.GetGenericMethod(_metadataWriter.Context)) == _metadataWriter.GetMethodDefinitionOrReferenceHandle(y.GetGenericMethod(_metadataWriter.Context)) &&
                _metadataWriter.GetMethodSpecificationSignatureHandle(x) == _metadataWriter.GetMethodSpecificationSignatureHandle(y);
        }

        public int GetHashCode(IGenericMethodInstanceReference methodInstanceReference)
        {
            return Hash.Combine(
                _metadataWriter.GetMethodDefinitionOrReferenceHandle(methodInstanceReference.GetGenericMethod(_metadataWriter.Context)).GetHashCode(),
                _metadataWriter.GetMethodSpecificationSignatureHandle(methodInstanceReference).GetHashCode());
        }
    }
}
