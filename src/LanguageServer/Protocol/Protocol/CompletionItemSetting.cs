// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System;
using System.Text.Json.Serialization;

/// <summary>
/// Client capabilities specific to <see cref="CompletionItem"/>.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#completionClientCapabilities">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal sealed class CompletionItemSetting
{
    /// <summary>
    /// The client supports treating <see cref="CompletionItem.InsertText"/> as a snippet
    /// when <see cref="CompletionItem.InsertTextFormat"/> is set to <see cref="InsertTextFormat.Snippet"/>.
    /// <para>
    /// A snippet can define tab stops and placeholders with <c>$1</c>, <c>$2</c>
    /// and <c>${3:foo}</c>. <c>$0</c> defines the final tab stop and defaults to
    /// the end of the snippet. Placeholders with equal identifiers are
    /// linked, such that typing in one will update others too.
    /// </para>
    /// </summary>
    [JsonPropertyName("snippetSupport")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool SnippetSupport
    {
        get;
        set;
    }

    /// <summary>
    /// The client supports the <see cref="CompletionItem.CommitCharacters"/> property.
    /// </summary>
    [JsonPropertyName("commitCharactersSupport")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool CommitCharactersSupport
    {
        get;
        set;
    }

    /// <summary>
    /// The client supports the following content formats for the <see cref="CompletionItem.Documentation"/>
    /// property. The order describes the preferred format of the client.
    /// </summary>
    [JsonPropertyName("documentationFormat")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MarkupKind[]? DocumentationFormat
    {
        get;
        set;
    }

    /// <summary>
    /// The client supports the <see cref="CompletionItem.Deprecated"/> property on a completion item.
    /// </summary>
    [Obsolete("Use Tags instead if supported")]
    [JsonPropertyName("deprecatedSupport")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool DeprecatedSupport
    {
        get;
        set;
    }

    /// <summary>
    /// The client supports the <see cref="CompletionItem.Preselect"/> property.
    /// </summary>
    [JsonPropertyName("preselectSupport")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool PreselectSupport
    {
        get;
        set;
    }

    /// <summary>
    /// The tags that the client supports on the <see cref="CompletionItem.Tags"/> property.
    /// <para>
    /// Clients supporting tags have to handle unknown tags gracefully. Clients
    /// especially need to preserve unknown tags when sending a completion
    /// item back to the server in a resolve call.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.15</remarks>
    [JsonPropertyName("tagSupport")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CompletionItemTagSupportSetting? TagSupport
    {
        get;
        set;
    }

    /// <summary>
    /// Whether the client supports <see cref="InsertReplaceEdit"/> values on the
    /// <see cref="CompletionItem.TextEdit"/> property.
    /// </summary>
    /// <remarks>Since 3.16</remarks>
    [JsonPropertyName("insertReplaceSupport")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool InsertReplaceSupport
    {
        get;
        set;
    }

    /// <summary>
    /// Indicates which properties a client can resolve lazily on a completion item.
    /// <para>
    /// Before version 3.16 only the predefined properties <see cref="CompletionItem.Documentation"/>
    /// and <see cref="CompletionItem.Detail"/> could be resolved lazily.
    /// </para>
    /// </summary>
    /// <remarks>Since 3.16</remarks>
    [JsonPropertyName("resolveSupport")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ResolveSupportSetting? ResolveSupport
    {
        get;
        set;
    }

    /// <summary>
    /// Indicates whether the client supports the <see cref="CompletionItem.InsertTextMode"/>
    /// property and which <see cref="InsertTextMode"/> values it supports.
    /// </summary>
    /// <remarks>Since 3.16</remarks>
    [JsonPropertyName("insertTextModeSupport")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public InsertTextModeSupportSetting? InsertTextModeSupport
    {
        get;
        set;
    }

    /// <summary>
    /// Indicates whether the client supports the <see cref="CompletionItem.LabelDetails"/> property.
    /// </summary>
    /// <remarks>Since 3.17</remarks>
    [JsonPropertyName("labelDetailsSupport")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool LabelDetailsSupport
    {
        get;
        set;
    }
}
