// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// A document link is a range in a text document that links to an internal or
/// external resource, like another text document or a web site.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#documentLink">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal sealed class DocumentLink
{
    /// <summary>
    /// The range this link applies to.
    /// </summary>
    [JsonPropertyName("range")]
    public Range Range
    {
        get;
        set;
    }

    /// <summary>
    /// The uri this link points to. If missing a resolve request is sent later.
    /// </summary>
    [JsonPropertyName("target")]
    [JsonConverter(typeof(DocumentUriConverter))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DocumentUri? DocumentTarget
    {
        get;
        set;
    }

    [Obsolete("Use DocumentTarget instead. This property will be removed in a future version.")]
    [JsonIgnore]
    public Uri Target
    {
        get => DocumentTarget.GetRequiredParsedUri();
        set => DocumentTarget = new DocumentUri(value);
    }

    /// <summary>
    /// The tooltip text when you hover over this link.
    /// <para>
    /// If a tooltip is provided, it will be displayed in a string that includes
    /// instructions on how to trigger the link, such as <c>{0} (ctrl + click)</c>.
    /// The specific instructions vary depending on OS, user settings, and localization.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.15</remarks>
    [JsonPropertyName("tooltip")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Tooltip { get; init; }

    /// <summary>
    /// A data entry field that is preserved on a document link between a
    /// DocumentLinkRequest and a DocumentLinkResolveRequest.
    /// </summary>
    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Data { get; init; }
}
