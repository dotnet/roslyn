// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Microsoft.VisualStudio.Razor.LanguageClient.WrapWithTag;

/// <summary>
/// Class representing the parameters sent for a textDocument/_vsweb_wrapWithTag request.
/// Matches corresponding class in Web Tools' Html language server
/// </summary>
[DataContract]
internal class VSInternalWrapWithTagParams : ITextDocumentParams
{
    public VSInternalWrapWithTagParams(LspRange range,
                                       string tagName,
                                       FormattingOptions options,
                                       VersionedTextDocumentIdentifier textDocument)
    {
        Range = range;
        Options = options;
        TagName = tagName;
        TextDocument = textDocument;
    }

    TextDocumentIdentifier ITextDocumentParams.TextDocument => TextDocument;

    /// <summary>
    /// Gets or sets the identifier for the text document to be operate on.
    /// </summary>
    [DataMember(Name = "_vs_textDocument")]
    [JsonPropertyName("_vs_textDocument")]
    public VersionedTextDocumentIdentifier TextDocument
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the selection range to be wrapped.
    /// </summary>
    [DataMember(Name = "_vs_range")]
    [JsonPropertyName("_vs_range")]
    public LspRange Range
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the wrapping tag name.
    /// </summary>
    [DataMember(Name = "_vs_tagName")]
    [JsonPropertyName("_vs_tagName")]
    public string TagName
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the formatting options.
    /// </summary>
    [DataMember(Name = "_vs_options")]
    [JsonPropertyName("_vs_options")]
    public FormattingOptions Options
    {
        get;
        set;
    }
}
