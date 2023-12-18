// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Runtime.Serialization;
using Newtonsoft.Json;

/// <summary>
/// Diagnostic registration options.
///
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#diagnosticRegistrationOptions">Language Server Protocol specification</see> for additional information.
/// </summary>
[DataContract]
internal class DiagnosticRegistrationOptions : DiagnosticOptions, IStaticRegistrationOptions, ITextDocumentRegistrationOptions
{
    /// <summary>
    /// Gets or sets the document filters for this registration option.
    /// </summary>
    [DataMember(Name = "documentSelector")]
    [JsonProperty(NullValueHandling = NullValueHandling.Include)]
    public DocumentFilter[]? DocumentSelector
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether work done progress is supported.
    /// </summary>
    [DataMember(Name = "id")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? Id
    {
        get;
        set;
    }
}
