// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System;
using System.Text.Json.Serialization;

/// <summary>
/// Parameters of the 'workspace/diagnostic' request
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#workspaceDiagnosticParams">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>
/// Since LSP 3.17
/// </remarks>
internal class WorkspaceDiagnosticParams : IWorkDoneProgressParams, IPartialResultParams<SumType<WorkspaceDiagnosticReport, WorkspaceDiagnosticReportPartialResult>>
{
    /// <summary>
    /// An <see cref="IProgress{T}"/> instance that can be used to report partial results
    /// via the <c>$/progress</c> notification.
    /// <para>
    /// Note that the first literal sent needs to be a <see cref="WorkspaceDiagnosticReport"/>
    /// followed by n <see cref="WorkspaceDiagnosticReportPartialResult"/> literals.
    /// </para>
    /// </summary>
    [JsonPropertyName(Methods.PartialResultTokenName)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IProgress<SumType<WorkspaceDiagnosticReport, WorkspaceDiagnosticReportPartialResult>>? PartialResultToken { get; set; }

    /// <inheritdoc/>
    [JsonPropertyName(Methods.WorkDoneTokenName)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IProgress<WorkDoneProgress>? WorkDoneToken { get; set; }

    /// <summary>
    /// The additional identifier provided during registration.
    /// </summary>
    [JsonPropertyName("identifier")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Identifier
    {
        get;
        set;
    }

    /// <summary>
    /// The currently known diagnostic reports with their previous result ids.
    /// </summary>
    [JsonPropertyName("previousResultIds")]
    public PreviousResultId[] PreviousResultId
    {
        get;
        set;
    }
}
