// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System;
using System.Text.Json.Serialization;

/// <summary>
/// Class representing the parameters for the textDocument/completion request.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#completionParams">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal sealed class CompletionParams : TextDocumentPositionParams, IPartialResultParams<SumType<CompletionItem[], CompletionList>?>, IWorkDoneProgressParams
{
    /// <summary>
    /// The completion context. This is only available if the client specifies the
    /// client capability <see cref="CompletionSetting.ContextSupport"/>.
    /// </summary>
    [JsonPropertyName("context")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CompletionContext? Context
    {
        get;
        set;
    }

    /// <inheritdoc/>
    [JsonPropertyName(Methods.PartialResultTokenName)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IProgress<SumType<CompletionItem[], CompletionList>?>? PartialResultToken
    {
        get;
        set;
    }

    /// <inheritdoc/>
    [JsonPropertyName(Methods.WorkDoneTokenName)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IProgress<WorkDoneProgress>? WorkDoneToken { get; set; }
}
