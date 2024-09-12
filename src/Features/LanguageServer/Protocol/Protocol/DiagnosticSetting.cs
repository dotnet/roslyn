// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Runtime.Serialization;
using Newtonsoft.Json;

/// <summary>
/// Client settings for pull diagnostics.
///
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#diagnosticClientCapabilities">Language Server Protocol specification</see> for additional information.
/// </summary>
[DataContract]
internal class DiagnosticSetting : DynamicRegistrationSetting
{
    /// <summary>
    /// Gets or sets a value indicating whether the client supports related documents for document diagnostic pulls.
    /// </summary>
    [DataMember(Name = "relatedDocumentSupport")]
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public bool RelatedDocumentSupport { get; set; }
}