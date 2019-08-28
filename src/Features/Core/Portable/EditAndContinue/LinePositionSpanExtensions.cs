// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal static class LinePositionSpanExtensions
    {
        internal static LinePositionSpan AddLineDelta(this LinePositionSpan span, int lineDelta)
            => new LinePositionSpan(new LinePosition(span.Start.Line + lineDelta, span.Start.Character), new LinePosition(span.End.Line + lineDelta, span.End.Character));

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
    }
}
