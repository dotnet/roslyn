// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Shared.Collections;

internal readonly struct TextSpanIntervalIntrospector : IIntervalIntrospector<TextSpan>
{
    public int GetStart(TextSpan value)
        => value.Start;

    public int GetLength(TextSpan value)
        => value.Length;
}

internal sealed class TextSpanIntervalTree(IEnumerable<TextSpan>? values)
    : SimpleIntervalTree<TextSpan, TextSpanIntervalIntrospector>(new TextSpanIntervalIntrospector(), values)
{
    public TextSpanIntervalTree() : this(null)
    {
    }

    public TextSpanIntervalTree(params TextSpan[]? values) : this((IEnumerable<TextSpan>?)values)
    {
    }

    public bool HasIntervalThatIntersectsWith(TextSpan span)
        => this.HasIntervalThatIntersectsWith(span.Start, span.Length);
}
