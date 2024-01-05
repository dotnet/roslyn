﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Runtime.Serialization;
using Newtonsoft.Json;

/// <summary>
/// Class representing a diagnostic report with a full set of problems.
///
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#fullDocumentDiagnosticReport">Language Server Protocol specification</see> for additional information.
/// </summary>
[DataContract]
[Kind(DocumentDiagnosticReportKind.Full)]
internal class FullDocumentDiagnosticReport
{
    /// <summary>
    /// Gets the kind of this report.
    /// </summary>
    [DataMember(Name = "kind")]
#pragma warning disable CA1822 // Mark members as static
    public string Kind => DocumentDiagnosticReportKind.Full;
#pragma warning restore CA1822 // Mark members as static

    /// <summary>
    /// Gets or sets the optional result id.
    /// </summary>
    [DataMember(Name = "resultId")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? ResultId
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the diagnostics in this report.
    /// </summary>
    [DataMember(Name = "items")]
    public Diagnostic[] Items
    {
        get;
        set;
    }
}
