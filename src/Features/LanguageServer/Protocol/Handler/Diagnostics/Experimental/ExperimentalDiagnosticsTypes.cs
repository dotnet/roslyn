// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.Experimental;

using DocumentDiagnosticReport = SumType<FullDocumentDiagnosticReport, UnchangedDocumentDiagnosticReport>;
using WorkspaceDocumentDiagnosticReport = SumType<WorkspaceFullDocumentDiagnosticReport, WorkspaceUnchangedDocumentDiagnosticReport>;

// A document diagnostic partial report is defined as having the first literal send = DocumentDiagnosticReport (aka the sumtype of changed / unchanged) followed
// by n DocumentDiagnosticPartialResult literals.
// See https://github.com/microsoft/vscode-languageserver-node/blob/main/protocol/src/common/proposed.diagnostics.md#textDocument_diagnostic
using DocumentDiagnosticPartialReport = SumType<SumType<FullDocumentDiagnosticReport, UnchangedDocumentDiagnosticReport>, DocumentDiagnosticPartialResult>;

internal class DocumentDiagnosticParams : IPartialResultParams<DocumentDiagnosticPartialReport[]>
{
    [JsonConstructor]
    public DocumentDiagnosticParams(
        TextDocumentIdentifier textDocument,
        string? identifier,
        string? previousResultId,
        IProgress<DocumentDiagnosticPartialReport[]>? partialResultToken,
        IProgress<DocumentDiagnosticPartialReport[]>? workDoneToken)
    {
        TextDocument = textDocument ?? throw new ArgumentNullException(nameof(textDocument));
        Identifier = identifier;
        PreviousResultId = previousResultId;
        PartialResultToken = partialResultToken;
        WorkDoneToken = workDoneToken;
    }

    [JsonProperty(PropertyName = "textDocument", Required = Required.Always)]
    public TextDocumentIdentifier TextDocument { get; }

    [JsonProperty(PropertyName = "identifier", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string? Identifier { get; set; }

    [JsonProperty(PropertyName = "previousResultId", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string? PreviousResultId { get; set; }

    [JsonProperty(PropertyName = Methods.PartialResultTokenName, NullValueHandling = NullValueHandling.Ignore)]
    public IProgress<DocumentDiagnosticPartialReport[]>? PartialResultToken { get; set; }

    [JsonProperty(PropertyName = Methods.WorkDoneTokenName, NullValueHandling = NullValueHandling.Ignore)]
    public IProgress<DocumentDiagnosticPartialReport[]>? WorkDoneToken { get; set; }
}

[DataContract]
[JsonConverter(typeof(StringEnumConverter))]
internal enum DocumentDiagnosticReportKind
{
    [EnumMember(Value = "full")]
    Full,

    [EnumMember(Value = "unChanged")]
    UnChanged,
}

internal class FullDocumentDiagnosticReport
{
    [JsonConstructor]
    public FullDocumentDiagnosticReport(string? resultId, VisualStudio.LanguageServer.Protocol.Diagnostic[] items)
    {
        ResultId = resultId;
        Items = items ?? throw new ArgumentNullException(nameof(items));
    }

    [JsonProperty(PropertyName = "kind", Required = Required.Always)]
    public DocumentDiagnosticReportKind Kind { get; } = DocumentDiagnosticReportKind.Full;

    [JsonProperty(PropertyName = "resultId", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string? ResultId { get; set; }

    [JsonProperty(PropertyName = "items", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public Microsoft.VisualStudio.LanguageServer.Protocol.Diagnostic[] Items { get; }
}

internal class UnchangedDocumentDiagnosticReport
{
    [JsonConstructor]
    public UnchangedDocumentDiagnosticReport(string? resultId)
    {
        ResultId = resultId;
    }

    [JsonProperty(PropertyName = "kind", Required = Required.Always)]
    public DocumentDiagnosticReportKind Kind { get; } = DocumentDiagnosticReportKind.UnChanged;

    [JsonProperty(PropertyName = "resultId", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string? ResultId { get; }
}

internal class DocumentDiagnosticPartialResult
{
    [JsonConstructor]
    public DocumentDiagnosticPartialResult(Dictionary<Uri, DocumentDiagnosticReport> relatedDocuments)
    {
        RelatedDocuments = relatedDocuments ?? throw new ArgumentNullException(nameof(relatedDocuments));
    }

    [JsonProperty(PropertyName = "relatedDocuments", Required = Required.Always)]
    public Dictionary<Uri, DocumentDiagnosticReport> RelatedDocuments { get; }
}

internal class WorkspaceDiagnosticParams : IPartialResultParams<WorkspaceDiagnosticReport[]>
{
    [JsonConstructor]
    public WorkspaceDiagnosticParams(
        string? identifier,
        PreviousResultId[] previousResultIds,
        IProgress<WorkspaceDiagnosticReport[]>? workDoneToken,
        IProgress<WorkspaceDiagnosticReport[]>? partialResultToken)
    {
        Identifier = identifier;
        PreviousResultIds = previousResultIds;
        WorkDoneToken = workDoneToken;
        PartialResultToken = partialResultToken;
    }

    [JsonProperty(PropertyName = "identifier", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string? Identifier { get; }

    [JsonProperty(PropertyName = "previousResultIds", Required = Required.Always)]
    public PreviousResultId[] PreviousResultIds { get; }

    [JsonProperty(PropertyName = Methods.WorkDoneTokenName, NullValueHandling = NullValueHandling.Ignore)]
    public IProgress<WorkspaceDiagnosticReport[]>? WorkDoneToken { get; set; }

    [JsonProperty(PropertyName = Methods.PartialResultTokenName, NullValueHandling = NullValueHandling.Ignore)]
    public IProgress<WorkspaceDiagnosticReport[]>? PartialResultToken { get; set; }
}

internal class PreviousResultId
{
    [JsonConstructor]
    public PreviousResultId(Uri uri, string value)
    {
        Uri = uri ?? throw new ArgumentNullException(nameof(uri));
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    [JsonProperty(PropertyName = "uri", Required = Required.Always)]
    [JsonConverter(typeof(DocumentUriConverter))]
    public Uri Uri { get; }

    [JsonProperty(PropertyName = "value", Required = Required.Always)]
    public string Value { get; }
}

internal class WorkspaceDiagnosticReport
{
    [JsonConstructor]
    public WorkspaceDiagnosticReport(WorkspaceDocumentDiagnosticReport[] items)
    {
        Items = items ?? throw new ArgumentNullException(nameof(items));
    }

    [JsonProperty(PropertyName = "items", Required = Required.Always)]
    public WorkspaceDocumentDiagnosticReport[] Items { get; }
}

internal class WorkspaceFullDocumentDiagnosticReport : FullDocumentDiagnosticReport
{
    [JsonConstructor]
    public WorkspaceFullDocumentDiagnosticReport(
        Uri uri,
        VisualStudio.LanguageServer.Protocol.Diagnostic[] items,
        int? version,
        string? resultId) : base(resultId, items)
    {
        Uri = uri ?? throw new ArgumentNullException(nameof(uri));
        Version = version;
    }

    [JsonProperty(PropertyName = "uri", Required = Required.Always)]
    [JsonConverter(typeof(DocumentUriConverter))]
    public Uri Uri { get; }

    [JsonProperty(PropertyName = "version", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public int? Version { get; set; }
}

internal class WorkspaceUnchangedDocumentDiagnosticReport : UnchangedDocumentDiagnosticReport
{
    [JsonConstructor]
    public WorkspaceUnchangedDocumentDiagnosticReport(Uri uri, string? resultId, int? version) : base(resultId)
    {
        Uri = uri ?? throw new ArgumentNullException(nameof(uri));
        Version = version;
    }

    [JsonProperty(PropertyName = "uri", Required = Required.Always)]
    [JsonConverter(typeof(DocumentUriConverter))]
    public Uri Uri { get; }

    [JsonProperty(PropertyName = "version", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public int? Version { get; set; }
}

internal static class ExperimentalMethods
{
    public const string TextDocumentDiagnostic = "textDocument/diagnostic";
    public const string WorkspaceDiagnostic = "workspace/diagnostic";
}
