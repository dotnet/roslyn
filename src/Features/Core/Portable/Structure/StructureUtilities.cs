// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Structure
{
    internal static class StructureUtilities
    {
        public static TextSpan DetermineHeaderSpan(TextSpan textSpan, TextSpan hintSpan, SourceText text)
        {
            if (hintSpan.Start < textSpan.Start)
            {
                // The HeaderSpan is what is used for drawing the guidelines and also what is shown if
                // you mouse over a guideline. We will use the text from the hint start to the collapsing
                // start; in the case this spans mutiple lines the editor will clip it for us and suffix an
                // ellipsis at the end.
                return TextSpan.FromBounds(hintSpan.Start, textSpan.Start);
            }
            else
            {
                var hintLine = text.Lines.GetLineFromPosition(hintSpan.Start);
                return TrimLeadingWhitespace(hintLine.Span);
            }

            TextSpan TrimLeadingWhitespace(TextSpan span)
            {
                var start = span.Start;

                while (start < span.End && char.IsWhiteSpace(text[start]))
                    start++;

                return TextSpan.FromBounds(start, span.End);
            }
        }
    }
}
