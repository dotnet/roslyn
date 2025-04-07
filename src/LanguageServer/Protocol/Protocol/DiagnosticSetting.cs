﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Client capabilities specific to pull diagnostics.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#diagnosticClientCapabilities">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.17</remarks>
internal class DiagnosticSetting : DynamicRegistrationSetting
{
    /// <summary>
    /// Gets or sets a value indicating whether the client supports related documents for document diagnostic pulls.
    /// </summary>
    [JsonPropertyName("relatedDocumentSupport")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool RelatedDocumentSupport { get; set; }
}
