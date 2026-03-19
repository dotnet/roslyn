// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Additional execution summary information
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#executionSummary">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.17</remarks>
internal sealed class ExecutionSummary
{
    /// <summary>
    /// A strict monotonically increasing value indicating the
    /// execution order of a cell inside a notebook.
    /// </summary>
    [JsonPropertyName("executionOrder")]
    [JsonRequired]
    public int ExecutionOrder { get; init; }

    /// <summary>
    /// Whether the execution was successful or not, if known by the client.
    /// </summary>
    [JsonPropertyName("success")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Success { get; init; }
}
