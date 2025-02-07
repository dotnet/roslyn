// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Analyzer.Utilities.Extensions
{
    internal static partial class SourceTextExtensions
    {
        public static bool OverlapsHiddenPosition(
            this SourceText text,
            TextSpan span,
            Func<int, CancellationToken, bool> isPositionHidden,
            CancellationToken cancellationToken)
        {
            var result = TryOverlapsHiddenPosition(text, span, isPositionHidden, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return result;
        }

        /// <summary>
        /// Same as OverlapsHiddenPosition but doesn't throw on cancellation.  Instead, returns false
        /// in that case.
        /// </summary>
        public static bool TryOverlapsHiddenPosition(
            this SourceText text,
            TextSpan span,
            Func<int, CancellationToken, bool> isPositionHidden,
            CancellationToken cancellationToken)
        {
            var startLineNumber = text.Lines.IndexOf(span.Start);
            var endLineNumber = text.Lines.IndexOf(span.End);

            // NOTE(cyrusn): It's safe to examine the start of a line because you can't have a line
            // with both a pp directive and code on it.  so, for example, if a node crosses a region
            // then it must be the case that the start of some line from the start of the node to
            // the end is hidden.  i.e.:
#if false
'           class C
'           {
'#line hidden
'           }
'#line default
#endif
            // The start of the line with the } on it is hidden, and thus the node overlaps a hidden
            // region.

            for (var lineNumber = startLineNumber; lineNumber <= endLineNumber; lineNumber++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var linePosition = text.Lines[lineNumber].Start;
                var isHidden = isPositionHidden(linePosition, cancellationToken);
                if (isHidden)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
