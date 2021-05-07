// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal static class LinePositionSpanExtensions
    {
        internal static LinePositionSpan AddLineDelta(this LinePositionSpan span, int lineDelta)
            => new(new LinePosition(span.Start.Line + lineDelta, span.Start.Character), new LinePosition(span.End.Line + lineDelta, span.End.Character));

        internal static SourceFileSpan AddLineDelta(this SourceFileSpan span, int lineDelta)
            => new(span.Path, span.Span.AddLineDelta(lineDelta));

        internal static int GetLineDelta(this LinePositionSpan oldSpan, LinePositionSpan newSpan)
            => newSpan.Start.Line - oldSpan.Start.Line;

        internal static bool Contains(this LinePositionSpan container, LinePositionSpan span)
            => span.Start >= container.Start && span.End <= container.End;

        public static LinePositionSpan ToLinePositionSpan(this SourceSpan span)
            => new(new(span.StartLine, span.StartColumn), new(span.EndLine, span.EndColumn));

        public static SourceSpan ToSourceSpan(this LinePositionSpan span)
            => new(span.Start.Line, span.Start.Character, span.End.Line, span.End.Character);

        public static LinePosition Min(LinePosition x, LinePosition y)
            => (x < y) ? x : y;

        public static LinePosition Max(LinePosition x, LinePosition y)
            => (x > y) ? x : y;

        public static bool OverlapsWith(this LinePositionSpan x, LinePositionSpan span)
            => Max(x.Start, span.Start) < Min(x.End, span.End);
    }
}
