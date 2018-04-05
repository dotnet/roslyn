// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Extensions
{
    internal static class SourceTextExtensions
    {
        public static VsTextSpan GetVsTextSpanForSpan(this SourceText text, TextSpan textSpan)
        {
            text.GetLinesAndOffsets(textSpan, out var startLine, out var startOffset, out var endLine, out var endOffset);

            return new VsTextSpan()
            {
                iStartLine = startLine,
                iStartIndex = startOffset,
                iEndLine = endLine,
                iEndIndex = endOffset
            };
        }

        public static VsTextSpan GetVsTextSpanForLineOffset(this SourceText text, int lineNumber, int offset)
        {
            return new VsTextSpan
            {
                iStartLine = lineNumber,
                iStartIndex = offset,
                iEndLine = lineNumber,
                iEndIndex = offset
            };
        }

        public static VsTextSpan GetVsTextSpanForPosition(this SourceText text, int position, int virtualSpace)
        {
            text.GetLineAndOffset(position, out var lineNumber, out var offset);

            offset += virtualSpace;

            return text.GetVsTextSpanForLineOffset(lineNumber, offset);
        }
    }
}
