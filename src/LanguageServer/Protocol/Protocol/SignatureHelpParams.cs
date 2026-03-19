// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System;
using System.Text.Json.Serialization;

/// <summary>
/// Class representing the parameters for the textDocument/signatureHelp request.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#signatureHelpParams">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal sealed class SignatureHelpParams : TextDocumentPositionParams, IWorkDoneProgressParams
{
    /// <summary>
    /// The signature help context.
    /// <para>
    /// This is only available if the client specifies the client
    /// capability <see cref="SignatureHelpSetting.ContextSupport"/>
    /// </para>
    /// </summary>
    [JsonPropertyName("context")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SignatureHelpContext? Context { get; set; }

    /// <inheritdoc/>
    [JsonPropertyName(Methods.WorkDoneTokenName)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IProgress<WorkDoneProgress>? WorkDoneToken { get; set; }
}
