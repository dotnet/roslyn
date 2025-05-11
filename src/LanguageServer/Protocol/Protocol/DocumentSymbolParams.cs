// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Class which represents the parameter sent with textDocument/documentSymbol requests.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#documentSymbolParams">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal class DocumentSymbolParams
    : ITextDocumentParams, IWorkDoneProgressParams,
#pragma warning disable CS0618 // SymbolInformation is obsolete but this class is not
      IPartialResultParams<SumType<SymbolInformation[], DocumentSymbol[]>>
#pragma warning restore CS0618
{
    /// <summary>
    /// Gets or sets the text document.
    /// </summary>
    [JsonPropertyName("textDocument")]
    public TextDocumentIdentifier TextDocument
    {
        get;
        set;
    }

    /// <inheritdoc/>
    [JsonPropertyName(Methods.WorkDoneTokenName)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IProgress<WorkDoneProgress>? WorkDoneToken { get; set; }

    /// <inheritdoc/>
    [JsonPropertyName(Methods.PartialResultTokenName)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
#pragma warning disable CS0618 // SymbolInformation is obsolete but this property is not
    public IProgress<SumType<SymbolInformation[], DocumentSymbol[]>>? PartialResultToken { get; set; }
#pragma warning restore CS0618
}
