// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

[Union, JsonConverter(typeof(UnionConverter))]
internal sealed class DocumentDiagnosticPartialReport : IUnion
{
    public DocumentDiagnosticPartialReport(RelatedFullDocumentDiagnosticReport value) => Value = value;
    public DocumentDiagnosticPartialReport(RelatedUnchangedDocumentDiagnosticReport value) => Value = value;
    public DocumentDiagnosticPartialReport(DocumentDiagnosticReportPartialResult value) => Value = value;
    public object Value { get; }
}

[Union, JsonConverter(typeof(UnionConverter))]
internal sealed class FullOrUnchangedDiagnosticReport : IUnion
{
    public FullOrUnchangedDiagnosticReport(FullDocumentDiagnosticReport value) => Value = value;
    public FullOrUnchangedDiagnosticReport(UnchangedDocumentDiagnosticReport value) => Value = value;
    public object Value { get; }
}

[Union, JsonConverter(typeof(UnionConverter))]
internal sealed class ReferenceItem : IUnion
{
    public ReferenceItem(VSInternalReferenceItem value) => Value = value;
    public ReferenceItem(Location value) => Value = value;
    public object Value { get; }
}

[Union, JsonConverter(typeof(UnionConverter))]
internal sealed class NotebookFilter : IUnion
{
    public NotebookFilter(string value) => Value = value;
    public NotebookFilter(NotebookDocumentFilter value) => Value = value;
    public object Value { get; }
}

[Union, JsonConverter(typeof(UnionConverter))]
internal sealed class NotebookSyncOptions : IUnion
{
    public NotebookSyncOptions(NotebookDocumentSyncOptions value) => Value = value;
    public NotebookSyncOptions(NotebookDocumentSyncRegistrationOptions value) => Value = value;
    public object Value { get; }
}
