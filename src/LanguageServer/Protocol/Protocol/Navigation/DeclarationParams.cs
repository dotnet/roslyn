// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// The params sent in a 'textDocument/declaration' request.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#declarationParams">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal class DeclarationParams : TextDocumentPositionParams, IWorkDoneProgressParams, IPartialResultParams<SumType<Location[], LocationLink[]>>
{
    /// <inheritdoc/>
    [JsonPropertyName(Methods.WorkDoneTokenName)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IProgress<WorkDoneProgress>? WorkDoneToken { get; set; }

    /// <inheritdoc/>
    /// <remarks>
    /// <see cref="LocationLink"/> may only be used if the client opts in via <see cref="DeclarationClientCapabilities.LinkSupport"/>
    /// </remarks>
    [JsonPropertyName(Methods.PartialResultTokenName)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IProgress<SumType<Location[], LocationLink[]>>? PartialResultToken { get; set; }
}
