// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Host
{
    internal interface ITemporaryTextStorageWithName : ITemporaryTextStorage, ITemporaryStorageWithName
    {
        SourceHashAlgorithm ChecksumAlgorithm { get; }

        Encoding? Encoding { get; }

        ImmutableArray<byte> GetChecksum();
    }
}
