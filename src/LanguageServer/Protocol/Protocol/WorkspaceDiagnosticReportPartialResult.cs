// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;
using System.Text.Json.Serialization;

/// <summary>
/// Class representing a partial result for a workspace diagnostic report.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#workspaceDiagnosticReportPartialResult">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.17</remarks>
internal class WorkspaceDiagnosticReportPartialResult
{
    /// <summary>
    /// Gets or sets the items in this diagnostic report.
    /// </summary>
    [JsonPropertyName("items")]
    [JsonRequired]
    public SumType<WorkspaceFullDocumentDiagnosticReport, WorkspaceUnchangedDocumentDiagnosticReport>[] Items
    {
        get;
        set;
    }
}
