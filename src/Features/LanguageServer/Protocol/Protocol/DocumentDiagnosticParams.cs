// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

/// <summary>
/// Class representing the document diagnostic request parameters
///
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#documentDiagnosticParams">Language Server Protocol specification</see> for additional information.
/// </summary>
[DataContract]
internal class DocumentDiagnosticParams : ITextDocumentParams, IPartialResultParams<SumType<RelatedFullDocumentDiagnosticReport, RelatedUnchangedDocumentDiagnosticReport, DocumentDiagnosticReportPartialResult>>
{
    /// <summary>
    /// Gets or sets the value of the Progress instance.
    /// </summary>
    /// <remarks>
    /// Note that the first literal send needs to be either the <see cref="RelatedUnchangedDocumentDiagnosticReport"/> or <see cref="RelatedUnchangedDocumentDiagnosticReport"/>
    /// followed by n <see cref="DocumentDiagnosticReportPartialResult"/> literals.
    /// </remarks>
    [DataMember(Name = Methods.PartialResultTokenName, IsRequired = false)]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public IProgress<SumType<RelatedFullDocumentDiagnosticReport, RelatedUnchangedDocumentDiagnosticReport, DocumentDiagnosticReportPartialResult>>? PartialResultToken
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the <see cref="TextDocumentIdentifier"/> to provide diagnostics for.
    /// </summary>
    [DataMember(Name = "textDocument")]
    public TextDocumentIdentifier TextDocument
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the identifier for which the client is requesting diagnostics for.
    /// </summary>
    [DataMember(Name = "identifier")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? Identifier
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the result id of a previous diagnostics response if provided.
    /// </summary>
    [DataMember(Name = "previousResultId")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? PreviousResultId
    {
        get;
        set;
    }
}
