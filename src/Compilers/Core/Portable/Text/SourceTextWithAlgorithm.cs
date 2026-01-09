// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace Microsoft.CodeAnalysis.Text;

internal sealed class SourceTextWithAlgorithm : SourceText
{
    private readonly SourceText _underlying;

    public SourceTextWithAlgorithm(SourceText underlying, SourceHashAlgorithm checksumAlgorithm) : base(checksumAlgorithm: checksumAlgorithm)
    {
        Debug.Assert(checksumAlgorithm != SourceHashAlgorithm.None);
        Debug.Assert(checksumAlgorithm != underlying.ChecksumAlgorithm);
        _underlying = underlying;
    }

    public override char this[int position] => _underlying[position];

    public override Encoding? Encoding => _underlying.Encoding;

    public override int Length => _underlying.Length;

    public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
    {
        _underlying.CopyTo(sourceIndex, destination, destinationIndex, count);
    }
}
