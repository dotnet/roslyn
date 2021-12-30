// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Cci
{
    internal sealed class TypeSpecComparer : IEqualityComparer<ITypeReference>
    {
        private readonly MetadataWriter _metadataWriter;

        internal TypeSpecComparer(MetadataWriter metadataWriter)
        {
            _metadataWriter = metadataWriter;
        }

        public bool Equals(ITypeReference? x, ITypeReference? y)
        {
            return x == y || _metadataWriter.GetTypeSpecSignatureIndex(x).Equals(_metadataWriter.GetTypeSpecSignatureIndex(y));
        }

        public int GetHashCode(ITypeReference typeReference)
        {
            return _metadataWriter.GetTypeSpecSignatureIndex(typeReference).GetHashCode();
        }
    }
}
