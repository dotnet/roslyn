// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.RawStringLiteral;

/// <summary>
/// Service that provides text edits for raw string literal auto-insertion scenarios.
/// This service handles typing a quote character (") when it should grow raw string delimiters.
/// </summary>
internal interface IRawStringLiteralAutoInsertService : ILanguageService
{
    /// <summary>
    /// Gets the text change to apply when if a quote character is typed at the given position,
    /// or null if no special handling is needed.
    /// </summary>
    /// <param name="document">The document where the quote is being typed.</param>
    /// <param name="text">The source text of the document without the typed quote inserted.</param>
    /// <param name="caretPosition">The position where the quote character would be typed.</param>
    /// <returns>A text change to apply *after* the quote has been typed</returns>
    TextChange? GetTextChangeForQuote(Document document, SourceText text, int caretPosition, CancellationToken cancellationToken);
}
