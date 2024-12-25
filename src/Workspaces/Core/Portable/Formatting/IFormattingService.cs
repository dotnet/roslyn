// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Formatting;

internal interface IFormattingService : ILanguageService
{
    /// <summary>
    /// Formats the whitespace in areas of a document corresponding to multiple non-overlapping spans.
    /// </summary>
    /// <param name="document">The document to format.</param>
    /// <param name="spans">The spans of the document's text to format. If null, the entire document should be formatted.</param>
    /// <param name="lineFormattingOptions">Line formatting options.</param>
    /// <param name="syntaxFormattingOptions">Formatting options, if available. Null for non-Roslyn languages.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The formatted document.</returns>
    Task<Document> FormatAsync(Document document, IEnumerable<TextSpan>? spans, LineFormattingOptions lineFormattingOptions, SyntaxFormattingOptions? syntaxFormattingOptions, CancellationToken cancellationToken);
}
