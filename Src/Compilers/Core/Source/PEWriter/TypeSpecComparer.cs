// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Cci
{
    internal sealed class TypeSpecComparer : IEqualityComparer<ITypeReference>
    {
        internal TypeSpecComparer(PeWriter peWriter)
        {
            this.peWriter = peWriter;
        }

        public bool Equals(ITypeReference x, ITypeReference y)
        {
            return x == y || this.peWriter.GetTypeSpecSignatureIndex(x) == this.peWriter.GetTypeSpecSignatureIndex(y);
        }

        public int GetHashCode(ITypeReference typeReference)
        {
            return (int)this.peWriter.GetTypeSpecSignatureIndex(typeReference);
        }

        private PeWriter peWriter;
    }
}