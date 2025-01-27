// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Shared.Extensions;

/// <summary>
/// Like Span, except it has a start/end line instead of a start/end position.
/// </summary>
/// <param name="Start">Inclusive</param>
/// <param name="End">Exclusive</param>
internal readonly record struct LineSpan(int Start, int End)
{
    public static LineSpan FromBounds(int start, int end)
        => new(start, end);
}
