// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

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
                if (_metadataWriter.GetMemberReferenceParent(x) != _metadataWriter.GetMemberReferenceParent(y))
                {
                    return false;
                }
            }

            if (x.Name != y.Name)
            {
                return false;
            }

            var xf = x as IFieldReference;
            var yf = y as IFieldReference;
            if (xf != null && yf != null)
            {
                return _metadataWriter.GetFieldSignatureIndex(xf) == _metadataWriter.GetFieldSignatureIndex(yf);
            }

            var xm = x as IMethodReference;
            var ym = y as IMethodReference;
            if (xm != null && ym != null)
            {
                return _metadataWriter.GetMethodSignatureHandle(xm) == _metadataWriter.GetMethodSignatureHandle(ym);
            }

            return false;
        }

        public int GetHashCode(ITypeMemberReference memberRef)
        {
            int hash = Hash.Combine(memberRef.Name, _metadataWriter.GetMemberReferenceParent(memberRef).GetHashCode());

            var fieldRef = memberRef as IFieldReference;
            if (fieldRef != null)
            {
                hash = Hash.Combine(hash, _metadataWriter.GetFieldSignatureIndex(fieldRef).GetHashCode());
            }
            else
            {
                var methodRef = memberRef as IMethodReference;
                if (methodRef != null)
                {
                    hash = Hash.Combine(hash, _metadataWriter.GetMethodSignatureHandle(methodRef).GetHashCode());
                }
            }

            return hash;
        }
    }
}
