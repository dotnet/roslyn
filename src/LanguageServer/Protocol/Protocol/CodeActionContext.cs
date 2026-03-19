// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Class representing diagnostic information about the context of a code action
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#codeActionContext">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal class CodeActionContext
{
    /// <summary>
    /// An array of diagnostics known on the client side overlapping the range
    /// provided to the <c>textDocument/codeAction</c> request.
    /// <para>
    /// They are provided so that the server knows which errors are currently
    /// presented to the user for the given range. There is no guarantee that
    /// these accurately reflect the error state of the resource. The primary
    /// parameter to compute code actions is the provided range.
    /// </para>
    /// </summary>
    [JsonPropertyName("diagnostics")]
    [JsonRequired]
    public Diagnostic[] Diagnostics
    {
        get;
        set;
    }

    /// <summary>
    /// Requested kinds of actions to return.
    /// <para>
    /// Actions not of this kind are filtered out by the client before being
    /// shown, so servers can omit computing them.
    /// </para>
    /// </summary>
    [JsonPropertyName("only")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CodeActionKind[]? Only
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the <see cref="CodeActionTriggerKind"/> indicating how the code action was triggered..
    /// </summary>
    /// <remarks>Since LSP 3.17</remarks>
    [JsonPropertyName("triggerKind")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CodeActionTriggerKind? TriggerKind
    {
        get;
        set;
    }
}
