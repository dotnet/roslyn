// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Cci
{
    internal sealed class MethodSpecComparer : IEqualityComparer<IGenericMethodInstanceReference>
    {
        private readonly MetadataWriter metadataWriter;

        internal MethodSpecComparer(MetadataWriter metadataWriter)
        {
            this.metadataWriter = metadataWriter;
        }

        public bool Equals(IGenericMethodInstanceReference x, IGenericMethodInstanceReference y)
        {
            if (x == y)
            {
                return true;
            }

            return
                this.metadataWriter.GetMethodDefOrRefCodedIndex(x.GetGenericMethod(metadataWriter.Context)) == this.metadataWriter.GetMethodDefOrRefCodedIndex(y.GetGenericMethod(metadataWriter.Context)) &&
                this.metadataWriter.GetMethodInstanceSignatureIndex(x) == this.metadataWriter.GetMethodInstanceSignatureIndex(y);
        }

        public int GetHashCode(IGenericMethodInstanceReference methodInstanceReference)
        {
            return (int)((this.metadataWriter.GetMethodDefOrRefCodedIndex(methodInstanceReference.GetGenericMethod(metadataWriter.Context)) << 2) ^
              this.metadataWriter.GetMethodInstanceSignatureIndex(methodInstanceReference));
        }
    }
}