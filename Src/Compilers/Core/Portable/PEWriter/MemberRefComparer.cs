// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.Cci
{
    internal sealed class MemberRefComparer : IEqualityComparer<ITypeMemberReference>
    {
        private readonly MetadataWriter metadataWriter;

        internal MemberRefComparer(MetadataWriter metadataWriter)
        {
            this.metadataWriter = metadataWriter;
        }

        public bool Equals(ITypeMemberReference x, ITypeMemberReference y)
        {
            if (x == y)
            {
                return true;
            }

            if (x.GetContainingType(metadataWriter.Context) != y.GetContainingType(metadataWriter.Context))
            {
                if (this.metadataWriter.GetMemberRefParentCodedIndex(x) != this.metadataWriter.GetMemberRefParentCodedIndex(y))
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
                return this.metadataWriter.GetFieldSignatureIndex(xf) == this.metadataWriter.GetFieldSignatureIndex(yf);
            }

            IMethodReference/*?*/ xm = x as IMethodReference;
            IMethodReference/*?*/ ym = y as IMethodReference;
            if (xm != null && ym != null)
            {
                return this.metadataWriter.GetMethodSignatureIndex(xm) == this.metadataWriter.GetMethodSignatureIndex(ym);
            }

            return false;
        }

        public int GetHashCode(ITypeMemberReference memberRef)
        {
            int hash = Hash.Combine(memberRef.Name, (int)this.metadataWriter.GetMemberRefParentCodedIndex(memberRef) << 4);

            IFieldReference/*?*/ fieldRef = memberRef as IFieldReference;
            if (fieldRef != null)
            {
                hash = Hash.Combine(hash, (int)this.metadataWriter.GetFieldSignatureIndex(fieldRef));
            }
            else
            {
                IMethodReference/*?*/ methodRef = memberRef as IMethodReference;
                if (methodRef != null)
                {
                    hash = Hash.Combine(hash, (int)this.metadataWriter.GetMethodSignatureIndex(methodRef));
                }
            }

            return hash;
        }
    }
}