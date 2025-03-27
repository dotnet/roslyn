// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Context for inline completion request.
/// See https://github.com/microsoft/vscode/blob/075ba020e8493f40dba89891b1a08453f2c067e9/src/vscode-dts/vscode.proposed.inlineCompletions.d.ts#L27.
/// </summary>
internal sealed class VSInternalInlineCompletionContext
{
    /// <summary>
    /// Gets or sets how completion was triggered.
    /// </summary>
    [JsonPropertyName("_vs_triggerKind")]
    [JsonRequired]
    public VSInternalInlineCompletionTriggerKind TriggerKind { get; set; } = VSInternalInlineCompletionTriggerKind.Explicit;

    /// <summary>
    /// Gets or sets information about the currently selected item in the autocomplete widget, if visible.
    ///
    /// See https://github.com/microsoft/vscode/blob/075ba020e8493f40dba89891b1a08453f2c067e9/src/vscode-dts/vscode.proposed.inlineCompletions.d.ts#L45.
    /// </summary>
    [JsonPropertyName("_vs_selectedCompletionInfo")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public VSInternalSelectedCompletionInfo? SelectedCompletionInfo { get; set; }
}
