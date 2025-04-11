// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Analyzer.Utilities
{
    internal static class CodeRefactoringHelpers
    {
        /// <summary>
        /// Trims leading and trailing whitespace from <paramref name="span"/>.
        /// </summary>
        /// <remarks>
        /// Returns unchanged <paramref name="span"/> in case <see cref="TextSpan.IsEmpty"/>.
        /// Returns empty Span with original <see cref="TextSpan.Start"/> in case it contains only whitespace.
        /// </remarks>
        public static async Task<TextSpan> GetTrimmedTextSpanAsync(Document document, TextSpan span, CancellationToken cancellationToken)
        {
            if (span.IsEmpty)
            {
                return span;
            }

            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var start = span.Start;
            var end = span.End;

            while (start < end && char.IsWhiteSpace(sourceText[end - 1]))
            {
                end--;
            }

            while (start < end && char.IsWhiteSpace(sourceText[start]))
            {
                start++;
            }

            return TextSpan.FromBounds(start, end);
        }
    }
}
