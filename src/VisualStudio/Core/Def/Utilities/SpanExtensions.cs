// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows.Media.TextFormatting;
using Microsoft.CodeAnalysis.Text;
using VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.Utilities
{
    internal static class SpanExtensions
    {
        internal static LinePositionSpan ToLinePositionSpan(this VsTextSpan span)
            => new LinePositionSpan(new LinePosition(span.iStartLine, span.iStartIndex), new LinePosition(span.iEndLine, span.iEndIndex));

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
