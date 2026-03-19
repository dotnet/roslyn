// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Class which represents general client capabilities.
/// </summary>
/// <remarks>Since LSP 3.16</remarks>
internal sealed class GeneralClientCapabilities
{
    /// <summary>
    /// Client capability that signals how the client
    /// handles stale requests (e.g. a request
    /// for which the client will not process the response
    /// anymore since the information is outdated).
    /// </summary>
    /// <remarks>Since LSP 3.17</remarks>
    [JsonPropertyName("staleRequestSupport")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public StaleRequestSupport? StaleRequestSupport { get; init; }

    /// <summary>
    /// Client capabilities specific to regular expressions.
    /// </summary>
    [JsonPropertyName("regularExpressions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RegularExpressionsClientCapabilities? RegularExpressions { get; init; }

    /// <summary>
    /// Client capabilities specific to the client's markdown parser.
    /// </summary>
    [JsonPropertyName("markdown")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MarkdownClientCapabilities? Markdown { get; init; }

    /// <summary>
    /// The position encodings supported by the client. Client and server
    /// have to agree on the same position encoding to ensure that offsets
    /// (e.g. character position in a line) are interpreted the same on both
    /// side.
    /// <para>
    /// To keep the protocol backwards compatible the following applies: if
    /// the value 'utf-16' is missing from the array of position encodings
    /// servers can assume that the client supports UTF-16. UTF-16 is
    /// therefore a mandatory encoding.
    /// </para>
    /// <para>
    /// If omitted it defaults to ['utf-16'].
    /// </para>
    /// <para>
    /// Implementation considerations: since the conversion from one encoding
    /// into another requires the content of the file / line the conversion
    /// is best done where the file is read which is usually on the server
    /// side.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.17</remarks>
    [JsonPropertyName("positionEncodings")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PositionEncodingKind[]? PositionEncodings { get; init; }

}
