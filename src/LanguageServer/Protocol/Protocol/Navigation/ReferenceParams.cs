// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// The params sent in a <c>textDocument/references</c> request.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#referenceParams">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal class ReferenceParams : TextDocumentPositionParams, IWorkDoneProgressParams, IPartialResultParams<Location[]>
{
    /// <summary>
    /// Gets or sets the reference context.
    /// </summary>
    [JsonPropertyName("context")]
    [JsonRequired]
    public ReferenceContext Context { get; set; }

    /// <inheritdoc/>
    [JsonPropertyName(Methods.WorkDoneTokenName)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IProgress<WorkDoneProgress>? WorkDoneToken { get; set; }

    /// <inheritdoc/>
    [JsonPropertyName(Methods.PartialResultTokenName)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IProgress<Location[]>? PartialResultToken { get; set; }
}
