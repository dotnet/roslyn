// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Provides information about the context in which an inline completion was requested.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#inlineCompletionContext">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.18</remarks>
internal sealed class InlineCompletionContext
{
    /// <summary>
    /// Describes how the inline completion was triggered.
    /// </summary>
    [JsonPropertyName("triggerKind")]
    [JsonRequired]
    public InlineCompletionTriggerKind TriggerKind { get; set; }

    /// <summary>
    /// Provides information about the currently selected item in the autocomplete widget
    /// if it is visible.
    /// <para>
    /// If set, provided inline completions must extend the text of the selected item
    /// and use the same range, otherwise they are not shown as preview.
    /// As an example, if the document text is <c>console.</c> and the selected item is
    /// <c>.log</c> replacing the <c>.</c> in the document, the inline completion must
    /// also replace <c>.</c> and start with <c>.log</c>, for example <c>.telemet</c>.
    /// </para>
    /// <para>
    /// Inline completion providers are requested again whenever the selected item changes.
    /// </para>
    /// </summary>
    [JsonPropertyName("selectedCompletionInfo")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SelectedCompletionInfo? SelectedCompletionInfo { get; set; }
}
