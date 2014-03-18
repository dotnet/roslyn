// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Roslyn.Utilities;
using Cci = Microsoft.Cci;

namespace Microsoft.Cci
{
    internal class MemberRefComparer : IEqualityComparer<ITypeMemberReference>
    {
        internal MemberRefComparer(PeWriter peWriter)
        {
            this.peWriter = peWriter;
        }

        public bool Equals(ITypeMemberReference x, ITypeMemberReference y)
        {
            if (x == y)
            {
                return true;
            }

            if (x.GetContainingType(peWriter.Context) != y.GetContainingType(peWriter.Context))
            {
                if (this.peWriter.GetMemberRefParentCodedIndex(x) != this.peWriter.GetMemberRefParentCodedIndex(y))
                {
                    return false;
                }
            }

            if (x.Name != y.Name)
            {
                return false;
            }

            IFieldReference/*?*/ xf = x as IFieldReference;
            IFieldReference/*?*/ yf = y as IFieldReference;
            if (xf != null && yf != null)
            {
                return this.peWriter.GetFieldSignatureIndex(xf) == this.peWriter.GetFieldSignatureIndex(yf);
            }

            IMethodReference/*?*/ xm = x as IMethodReference;
            IMethodReference/*?*/ ym = y as IMethodReference;
            if (xm != null && ym != null)
            {
                return this.peWriter.GetMethodSignatureIndex(xm) == this.peWriter.GetMethodSignatureIndex(ym);
            }

            return false;
        }

        public int GetHashCode(ITypeMemberReference memberRef)
        {
            int hash = Hash.Combine(memberRef.Name, (int)this.peWriter.GetMemberRefParentCodedIndex(memberRef) << 4);

            IFieldReference/*?*/ fieldRef = memberRef as IFieldReference;
            if (fieldRef != null)
            {
                hash = Hash.Combine(hash, (int)this.peWriter.GetFieldSignatureIndex(fieldRef));
            }
            else
            {
                IMethodReference/*?*/ methodRef = memberRef as IMethodReference;
                if (methodRef != null)
                {
                    hash = Hash.Combine(hash, (int)this.peWriter.GetMethodSignatureIndex(methodRef));
                }
            }

            return hash;
        }

        private PeWriter peWriter;
    }
}