// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

/// <summary>
/// Class representing a full diagnostic report with a set of related documents.
///
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#relatedFullDocumentDiagnosticReport">Language Server Protocol specification</see> for additional information.
/// </summary>
[DataContract]
[Kind(DocumentDiagnosticReportKind.Full)]
internal class RelatedFullDocumentDiagnosticReport : FullDocumentDiagnosticReport
{
    /// <summary>
    /// Gets or sets the map of related document diagnostic reports.
    /// </summary>
    [DataMember(Name = "relatedDocuments")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public Dictionary<Uri, SumType<FullDocumentDiagnosticReport, UnchangedDocumentDiagnosticReport>>? RelatedDocuments
    {
        get;
        set;
    }
}