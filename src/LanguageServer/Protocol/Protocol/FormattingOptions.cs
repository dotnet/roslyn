// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Collections.Generic;
using System.Text.Json.Serialization;

/// <summary>
/// Class which represents formatting options.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#formattingOptions">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
// NOTE: The FormattingOptionsConverter enables the FormattingOptions.OtherOptions JsonExtensionData to be strongly typed.
// The Json* attributes on the members are for reference only - the converter is fully custom and ignores them.
[JsonConverter(typeof(FormattingOptionsConverter))]
internal sealed class FormattingOptions
{
    /// <summary>
    /// Size of a tab in spaces.
    /// </summary>
    [JsonPropertyName("tabSize")]
    [JsonRequired]
    public int TabSize { get; set; }

    /// <summary>
    /// Whether tabs should be spaces.
    /// </summary>
    [JsonPropertyName("insertSpaces")]
    [JsonRequired]
    public bool InsertSpaces { get; set; }

    /// <summary>
    /// Trim trailing whitespace on a line.
    /// </summary>
    /// <remarks>Since LSP 3.15</remarks>
    [JsonPropertyName("trimTrailingWhitespace")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool TrimTrailingWhitespace { get; init; }

    /// <summary>
    /// Insert a newline character at the end of the file if one does not exist.
    /// </summary>
    /// <remarks>Since LSP 3.15</remarks>
    [JsonPropertyName("insertFinalNewline")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool InsertFinalNewline { get; init; }

    /// <summary>
    /// Trim all newlines after the final newline at the end of the file.
    /// </summary>
    /// <remarks>Since LSP 3.15</remarks>
    [JsonPropertyName("trimFinalNewlines")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool TrimFinalNewlines { get; init; }

    /// <summary>
    /// Other potential formatting options.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, SumType<bool, int, string>>? OtherOptions { get; set; }
}
