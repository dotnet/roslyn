// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Represents a folding range in a document.
/// <para>
/// To be valid, start and end line must be bigger than zero and smaller than
/// the number of lines in the document. Clients are free to ignore invalid ranges.
/// </para>
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#foldingRange">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.17</remarks>
internal sealed class FoldingRange
{
    /// <summary>
    /// The zero-based start line of the range to fold.
    /// <para>
    /// The folded area starts after the line's last character. To be
    /// valid, the  end must be zero or larger and smaller than the
    /// number of lines in the document.
    /// </para>
    /// </summary>
    [JsonPropertyName("startLine")]
    [JsonRequired]
    public int StartLine
    {
        get;
        set;
    }

    /// <summary>
    /// The zero-based character offset from where the folded range starts.
    /// <para>
    /// If not defined, defaults to the length of the start line.
    /// </para>
    /// </summary>
    [JsonPropertyName("startCharacter")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? StartCharacter
    {
        get;
        set;
    }

    /// <summary>
    /// The zero-based end line of the range to fold.
    /// <para>
    /// The folded area ends with the line's last character. To be valid,
    /// the end must be zero or larger and smaller than the number of 
    /// lines in the document.
    /// </para>
    /// </summary>
    [JsonPropertyName("endLine")]
    [JsonRequired]
    public int EndLine
    {
        get;
        set;
    }

    /// <summary>
    /// The zero-based character offset before the folded range ends.
    /// <para>
    /// If not defined, defaults to the length of the end line.
    /// </para>
    /// </summary>
    [JsonPropertyName("endCharacter")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? EndCharacter
    {
        get;
        set;
    }

    /// <summary>
    /// Describes the kind of the folding range such as <see cref="FoldingRangeKind.Comment"/>
    /// or <see cref="FoldingRangeKind.Region"/>.
    /// <para>
    /// The kind is used to categorize folding ranges and used by commands like 
    /// 'Fold all comments'. See <see cref="FoldingRangeKind"/> for an
    /// enumeration of standardized kinds.
    /// </para>
    /// </summary>
    [JsonPropertyName("kind")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FoldingRangeKind? Kind
    {
        get;
        set;
    }

    /// <summary>
    /// The text that the client should show when the specified range is
    /// collapsed.
    /// <para>
    /// If not defined or not supported by the client, a default will be chosen by the client
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.17</remarks>
    [JsonPropertyName("collapsedText")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CollapsedText
    {
        get;
        set;
    }
}
