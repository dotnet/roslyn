// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Class representing the response sent for a <c>workspace/applyEdit</c> request.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#applyWorkspaceEditResult">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal sealed class ApplyWorkspaceEditResponse
{
    /// <summary>
    /// Indicates whether the edit was applied or not.
    /// </summary>
    [JsonPropertyName("applied")]
    [JsonRequired]
    public bool Applied { get; set; }

    /// <summary>
    /// An optional textual description for why the edit was not applied.
    /// <para>
    /// This may be used by the server for diagnostic logging or to provide
    /// a suitable error for a request that triggered the edit.
    /// </para>
    /// </summary>
    [JsonPropertyName("failureReason")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FailureReason { get; set; }

    /// <summary>
    /// Depending on the client's failure handling strategy this
    /// might contain the index of the change that failed.
    /// <para>
    /// This property is only available if the client signals a <see cref="WorkspaceEditSetting.FailureHandling"/> strategy in its client capabilities.
    /// </para>
    /// </summary>
    [JsonPropertyName("failedChange")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? FailedChange { get; init; }
}
