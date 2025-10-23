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
internal interface IRawStringLiteralOnAutoInsertService : ILanguageService
{
    /// <summary>
    /// Gets the text change to apply when typing a quote character at the given position,
    /// or null if no special handling is needed.
    /// </summary>
    /// <param name="document">The document being edited.</param>
    /// <param name="caretPosition">The position where the quote is being typed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A text change to apply after the quote character is inserted, or null if no special handling is needed.
    /// The text change span represents where to insert text AFTER the quote has been inserted.
    /// </returns>
    TextChange? GetTextChangeForQuote(Document document, int caretPosition, CancellationToken cancellationToken);
}
