// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.Proposed;

using DocumentDiagnosticReport = SumType<FullDocumentDiagnosticReport, UnchangedDocumentDiagnosticReport>;

internal class DocumentDiagnosticParams : IPartialResultParams<DocumentDiagnosticReport[]>
{
    [JsonProperty(PropertyName = "textDocument")]
    public TextDocumentIdentifier TextDocument { get; set; }

    [JsonProperty(PropertyName = "identifier")]
    public string Identifier { get; set; }

    [JsonProperty(PropertyName = "previousResultId", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string? PreviousResultId { get; set; }

    [JsonProperty(PropertyName = Methods.PartialResultTokenName, NullValueHandling = NullValueHandling.Ignore)]
    public IProgress<DocumentDiagnosticReport[]>? PartialResultToken { get; set; }

    [JsonProperty(PropertyName = Methods.WorkDoneTokenName, NullValueHandling = NullValueHandling.Ignore)]
    public IProgress<DocumentDiagnosticReport[]>? WorkDoneToken { get; set; }
}

[DataContract]
[JsonConverter(typeof(StringEnumConverter))]
enum DocumentDiagnosticReportKind
{
    [EnumMember(Value = "full")]
    Full,

    [EnumMember(Value = "unChanged")]
    UnChanged,
}

internal class FullDocumentDiagnosticReport
{
    [JsonProperty(PropertyName = "kind")]
    public DocumentDiagnosticReportKind Kind { get; } = DocumentDiagnosticReportKind.Full;

    [JsonProperty(PropertyName = "resultId", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string? ResultId { get; set; }

    [JsonProperty(PropertyName = "items")]
    public Microsoft.VisualStudio.LanguageServer.Protocol.Diagnostic[] Items { get; set; }
}

internal class UnchangedDocumentDiagnosticReport
{
    [JsonProperty(PropertyName = "kind")]
    public DocumentDiagnosticReportKind Kind { get; } = DocumentDiagnosticReportKind.UnChanged;

    [JsonProperty(PropertyName = "resultId")]
    public string ResultId { get; set; }
}

internal static class ProposedMethods
{
    public const string TextDocumentDiagnostic = "textDocument/diagnostic";
}
