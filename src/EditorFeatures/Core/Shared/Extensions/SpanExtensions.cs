// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions;

/// <summary>
/// Extension methods for the editor Span struct
/// </summary>
internal static class SpanExtensions
{
    /// <summary>
    /// Convert the editor Span instance to the corresponding TextSpan instance
    /// </summary>
    /// <param name="span"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TextSpan ToTextSpan(this Span span)
    {
        // this is a terribly ugly hack.  It depends on the fact that the Span and TextSpan both are just two adjacent
        // ints, and that neither the editor or roslyn will realistically ever change the layout of these types. If
        // either does, we will blow up immediately, so this is somewhat ok for us to take a dependency on.
        return Unsafe.As<Span, TextSpan>(ref span);
    }

    public static bool IntersectsWith(this Span span, int position)
        => position >= span.Start && position <= span.End;
}
