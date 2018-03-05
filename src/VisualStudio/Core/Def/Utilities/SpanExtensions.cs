// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Windows.Media.TextFormatting;
using Microsoft.CodeAnalysis.Text;
using VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.Utilities
{
    internal static class SpanExtensions
    {
        internal static LinePositionSpan ToLinePositionSpan(this VsTextSpan span)
        {
            return new LinePositionSpan(new LinePosition(span.iStartLine, span.iStartIndex), new LinePosition(span.iEndLine, span.iEndIndex));
        }

        internal static VsTextSpan ToVsTextSpan(this LinePositionSpan span)
        {
            return new VsTextSpan
            {
                iStartLine = span.Start.Line,
                iStartIndex = span.Start.Character,
                iEndLine = span.End.Line,
                iEndIndex = span.End.Character
            };
        }
    }
}
