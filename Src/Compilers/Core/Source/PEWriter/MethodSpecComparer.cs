// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Cci = Microsoft.Cci;

namespace Microsoft.Cci
{
    internal class MethodSpecComparer : IEqualityComparer<IGenericMethodInstanceReference>
    {
        internal MethodSpecComparer(PeWriter peWriter)
        {
            this.peWriter = peWriter;
        }

        public bool Equals(IGenericMethodInstanceReference x, IGenericMethodInstanceReference y)
        {
            if (x == y)
            {
                return true;
            }

            return
                this.peWriter.GetMethodDefOrRefCodedIndex(x.GetGenericMethod(peWriter.Context)) == this.peWriter.GetMethodDefOrRefCodedIndex(y.GetGenericMethod(peWriter.Context)) &&
                this.peWriter.GetMethodInstanceSignatureIndex(x) == this.peWriter.GetMethodInstanceSignatureIndex(y);
        }

        public int GetHashCode(IGenericMethodInstanceReference methodInstanceReference)
        {
            return (int)((this.peWriter.GetMethodDefOrRefCodedIndex(methodInstanceReference.GetGenericMethod(peWriter.Context)) << 2) ^
              this.peWriter.GetMethodInstanceSignatureIndex(methodInstanceReference));
        }

        private PeWriter peWriter;
    }
}