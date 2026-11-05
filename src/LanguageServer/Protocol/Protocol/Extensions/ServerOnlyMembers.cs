// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

internal partial class InitializeParams
{
    [JsonPropertyName("rootPath")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RootPath { get; set; }

    [JsonPropertyName("rootUri")]
    public DocumentUri? RootUri { get; set; }
}

internal partial class ClientCapabilities
{
    [JsonPropertyName("notebook")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public NotebookDocumentClientCapabilities? Notebook { get; set; }
}

internal partial class HoverClientCapabilities
{
    /// <summary>
    /// Whether the client recognizes a leading U+200B (zero-width space) at the start of a C# declaration code block in
    /// hover markdown, and uses it to apply specialized syntax highlighting.
    /// </summary>
    [JsonPropertyName("_roslyn_supportsCSharpDeclarationMarkers")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool SupportsCSharpDeclarationMarkers { get; set; }
}

internal partial class ServerCapabilities
{
    public ServerCapabilities()
    {
        TextDocumentSync = new TextDocumentSyncOptions
        {
            OpenClose = true,
            Change = TextDocumentSyncKind.None,
            Save = new SaveOptions { IncludeText = false },
        };
    }

    [JsonPropertyName("notebookDocumentSync")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public NotebookSyncOptions? NotebookDocumentSync { get; set; }
}

internal partial class CompletionItem
{
    [JsonPropertyName("deprecated")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool? Deprecated { get; set; }
}

internal partial class DocumentSymbol
{
    [JsonPropertyName("deprecated")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Deprecated { get; set; }

    [JsonPropertyName("glyph")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Glyph { get; set; }
}

internal partial class SymbolInformation
{
    [JsonPropertyName("deprecated")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Deprecated { get; set; }
}

internal partial class InlineCompletionItem
{
    [JsonPropertyName("insertTextFormat")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public InsertTextFormat InsertTextFormat { get; set; } = InsertTextFormat.PlainText;
}

internal partial class WorkspaceSymbol
{
    [JsonPropertyName("tags")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public new SymbolTag? Tags { get; set; }
}

internal partial class RelatedFullDocumentDiagnosticReport
{
    [JsonPropertyName("relatedDocuments")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<Uri, FullOrUnchangedDiagnosticReport>? RelatedDocuments { get; set; }
}

internal partial class RelatedUnchangedDocumentDiagnosticReport
{
    [JsonPropertyName("relatedDocuments")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<Uri, FullOrUnchangedDiagnosticReport>? RelatedDocuments { get; set; }
}

internal partial class ExecuteCommandRegistrationOptions
{
    [JsonPropertyName("documentSelector")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DocumentFilter[]? DocumentSelector { get; set; }
}

internal partial class TextDocumentRegistrationOptions
{
    [JsonPropertyName("_vs_triggerCharacters")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[]? TriggerCharacters { get; set; }
}

internal partial class VSInternalDocumentOnAutoInsertResponseItem
{
    [JsonPropertyName("command")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Command? Command { get; set; }
}
