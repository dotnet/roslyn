// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System;
using System.Text.Json.Serialization;

/// <summary>
/// Parameter for the 'textDocument/formatting' request.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#documentFormattingParams">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Do not seal this type! This is extended by Razor</remarks>
internal class DocumentFormattingParams : ITextDocumentParams, IWorkDoneProgressParams
{
    /// <summary>
    /// Gets or sets the identifier for the text document to be formatted.
    /// </summary>
    [JsonPropertyName("textDocument")]
    [JsonRequired]
    public TextDocumentIdentifier TextDocument
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the formatting options.
    /// </summary>
    [JsonPropertyName("options")]
    [JsonRequired]
    public FormattingOptions Options
    {
        get;
        set;
    }

    /// <inheritdoc/>
    [JsonPropertyName(Methods.WorkDoneTokenName)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IProgress<WorkDoneProgress>? WorkDoneToken { get; set; }
}
