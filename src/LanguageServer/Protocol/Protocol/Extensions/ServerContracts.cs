// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

internal interface ITextDocumentParams
{
    [JsonPropertyName("textDocument")]
    [JsonRequired]
    TextDocumentIdentifier TextDocument { get; }
}

internal partial class DidChangeTextDocumentParams : ITextDocumentParams
{
    TextDocumentIdentifier ITextDocumentParams.TextDocument => TextDocument;
}

internal partial class WorkDoneProgressBegin : WorkDoneProgress;
internal partial class WorkDoneProgressReport : WorkDoneProgress;
internal partial class WorkDoneProgressEnd : WorkDoneProgress;

internal partial class DocumentDiagnosticParams :
    IPartialResultParams<DocumentDiagnosticPartialReport>;

internal interface ISpellCheckableRangeReport
{
    string? ResultId { get; set; }
    int[]? Ranges { get; set; }
}

internal partial class VSInternalSpellCheckableRangeReport : ISpellCheckableRangeReport;
internal partial class VSInternalWorkspaceSpellCheckableReport : ISpellCheckableRangeReport;
