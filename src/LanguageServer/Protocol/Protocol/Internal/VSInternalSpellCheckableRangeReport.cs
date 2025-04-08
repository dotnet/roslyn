// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Report of spell checkable ranges.
/// </summary>
internal class VSInternalSpellCheckableRangeReport
{
    /// <summary>
    /// Gets or sets the server-generated version number for the spell checkable ranges.
    /// This is treated as a black box by the client: it is stored on the client
    /// for each textDocument and sent back to the server when requesting
    /// spell checkable ranges. The server can use this result ID to avoid resending
    /// spell checkable ranges that had previously been sent.
    /// </summary>
    [JsonPropertyName("_vs_resultId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ResultId { get; set; }

    /// <summary>
    /// Gets or sets an array containing encoded ranges to spell check.
    /// </summary>
    /// <remarks>
    /// The data structure is as the following:
    /// 1. <see cref="Ranges"/> property can contain multiple spans to spell check.
    /// 2. Each span is defined by a set of 3 ordered integers in the <see cref="Ranges"/> property.
    /// 3. The span's ordered information are the following:
    ///     1. A kind, corresponding to the numeric value of <see cref="VSInternalSpellCheckableRangeKind"/>.
    ///     2. A start position, which is the character index where the span starts in the document buffer.
    ///        The start position should be the relative offset from the end of the previous span, regardless of whether that span is the same range
    ///        or a prior range.
    ///     3. The length of the span.
    /// 4. Spans should be ordered by their absolute start position in the document buffer.
    /// </remarks>
    /// <example>
    /// [
    ///     /* ---- First span in the document, admitting this is the first report returned. ---- */
    ///     1,   // The kind of the span. Equivalent to <see cref="VSInternalSpellCheckableRangeKind.Comment"/>.
    ///     123, // This is the very first span's start position. The position is relative to the beginning of the document buffer.
    ///     5,   // Span length
    ///     /* ---- Second span in the document ---- */
    ///     0,   // Equivalent to <see cref="VSInternalSpellCheckableRangeKind.String"/>.
    ///     6,   // Start position relative to the first span in the document. Absolute span position is therefore (123 + 5) + 6 = 134.
    ///     4,   // Span length
    ///     /* ---- Third span in the document ---- */
    ///     0,   // Equivalent to <see cref="VSInternalSpellCheckableRangeKind.String"/>.
    ///     12,  // Start position relative to the second span. Absolute span position is therefore (134 + 4) + 12 = 150
    ///     5    // Span length
    /// ]
    /// </example>
    [JsonPropertyName("_vs_ranges")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int[]? Ranges
    {
        get;
        set;
    }
}
