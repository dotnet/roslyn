// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.Cci
{
    internal sealed class MemberRefComparer : IEqualityComparer<ITypeMemberReference>
    {
        private readonly MetadataWriter _metadataWriter;

        internal MemberRefComparer(MetadataWriter metadataWriter)
        {
            _metadataWriter = metadataWriter;
        }

        public bool Equals(ITypeMemberReference x, ITypeMemberReference y)
        {
            if (x == y)
            {
                return true;
            }

            if (x.GetContainingType(_metadataWriter.Context) != y.GetContainingType(_metadataWriter.Context))
            {
                if (_metadataWriter.GetMemberRefParentCodedIndex(x) != _metadataWriter.GetMemberRefParentCodedIndex(y))
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
                return _metadataWriter.GetFieldSignatureIndex(xf) == _metadataWriter.GetFieldSignatureIndex(yf);
            }

            IMethodReference/*?*/ xm = x as IMethodReference;
            IMethodReference/*?*/ ym = y as IMethodReference;
            if (xm != null && ym != null)
            {
                return _metadataWriter.GetMethodSignatureIndex(xm) == _metadataWriter.GetMethodSignatureIndex(ym);
            }

            return false;
        }

        public int GetHashCode(ITypeMemberReference memberRef)
        {
            int hash = Hash.Combine(memberRef.Name, (int)_metadataWriter.GetMemberRefParentCodedIndex(memberRef) << 4);

            IFieldReference/*?*/ fieldRef = memberRef as IFieldReference;
            if (fieldRef != null)
            {
                hash = Hash.Combine(hash, (int)_metadataWriter.GetFieldSignatureIndex(fieldRef));
            }
            else
            {
                IMethodReference/*?*/ methodRef = memberRef as IMethodReference;
                if (methodRef != null)
                {
                    hash = Hash.Combine(hash, (int)_metadataWriter.GetMethodSignatureIndex(methodRef));
                }
            }

            return hash;
        }
    }
}
