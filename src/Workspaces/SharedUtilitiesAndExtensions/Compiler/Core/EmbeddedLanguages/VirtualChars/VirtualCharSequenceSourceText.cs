﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;

/// <summary>
/// Trivial implementation of a <see cref="SourceText"/> that directly maps over a <see
/// cref="VirtualCharSequence"/>.
/// </summary>
internal sealed class VirtualCharSequenceSourceText : SourceText
{
    private readonly ImmutableSegmentedList<VirtualChar> _virtualChars;

    public override Encoding? Encoding { get; }

    public VirtualCharSequenceSourceText(ImmutableSegmentedList<VirtualChar> virtualChars, Encoding? encoding)
    {
        _virtualChars = virtualChars;
        Encoding = encoding;
    }

    public override int Length => _virtualChars.Count;

    public override char this[int position] => _virtualChars[position];

    public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
    {
        for (int i = sourceIndex, n = sourceIndex + count; i < n; i++)
            destination[destinationIndex + i] = this[i];
    }
}
