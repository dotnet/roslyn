// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

/// <summary>
/// Class representing a workspace diagnostic report.
///
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#workspaceDiagnosticReport">Language Server Protocol specification</see> for additional information.
/// </summary>
[DataContract]
internal class WorkspaceDiagnosticReport
{
    /// <summary>
    /// Gets or sets the items in this diagnostic report.
    /// </summary>
    [DataMember(Name = "items")]
    public SumType<WorkspaceFullDocumentDiagnosticReport, WorkspaceUnchangedDocumentDiagnosticReport>[] Items
    {
        get;
        set;
    }
}