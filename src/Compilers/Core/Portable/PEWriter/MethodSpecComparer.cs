// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

        public bool Equals(IGenericMethodInstanceReference? x, IGenericMethodInstanceReference? y)
        {
            if (x == y)
            {
                return true;
            }
            RoslynDebug.Assert(x is object && y is object);

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
