// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal static class LinePositionSpanExtensions
    {
        internal static LinePositionSpan AddLineDelta(this LinePositionSpan span, int lineDelta)
            => new(new LinePosition(span.Start.Line + lineDelta, span.Start.Character), new LinePosition(span.End.Line + lineDelta, span.End.Character));

        internal static int GetLineDelta(this LinePositionSpan oldSpan, LinePositionSpan newSpan)
            => newSpan.Start.Line - oldSpan.Start.Line;

        internal static bool Contains(this LinePositionSpan container, LinePositionSpan span)
            => span.Start >= container.Start && span.End <= container.End;

        // TODO: Remove. Workaround for https://devdiv.visualstudio.com/DevDiv/_workitems/edit/554205 
        internal static TextSpan GetTextSpanSafe(this TextLineCollection lines, LinePositionSpan span)
        {
            if (lines.Count == 0)
            {
                return default;
            }

            try
            {
                return lines.GetTextSpan(span);
            }
            catch
            {
                return new TextSpan(lines[0].Text.Length - 1, 0);
            }
        }

        public static LinePositionSpan ToLinePositionSpan(this SourceSpan span)
            => new(new(span.StartLine, span.StartColumn), new(span.EndLine, span.EndColumn));

        public static SourceSpan ToSourceSpan(this LinePositionSpan span)
            => new(span.Start.Line, span.Start.Character, span.End.Line, span.End.Character);
    }
}
