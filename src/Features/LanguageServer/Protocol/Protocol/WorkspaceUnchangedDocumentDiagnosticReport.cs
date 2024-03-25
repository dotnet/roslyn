// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

/// <summary>
/// Class representing a unchanged document diagnostic report for workspace diagnostic result.
///
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#workspaceUnchangedDocumentDiagnosticReport">Language Server Protocol specification</see> for additional information.
/// </summary>
[DataContract]
[Kind(DocumentDiagnosticReportKind.Unchanged)]
internal class WorkspaceUnchangedDocumentDiagnosticReport : UnchangedDocumentDiagnosticReport
{
    /// <summary>
    /// Gets or sets the URI associated with this diagnostic report.
    /// </summary>
    [DataMember(Name = "uri")]
    [JsonConverter(typeof(DocumentUriConverter))]
    public Uri Uri
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the version number for which the diagnostics are reported.
    /// If the document is not marked as open 'null' can be provided.
    /// </summary>
    [DataMember(Name = "version")]
    public int? Version
    {
        get;
        set;
    }
}
