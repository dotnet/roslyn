// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Used to report the end of an operation to an <c>IProgress&lt;WorkDoneProgress></c>.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#workDoneProgressEnd">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.15</remarks>
internal sealed class WorkDoneProgressEnd : WorkDoneProgress
{
    // NOTE: the kind property from the spec is used as a JsonPolymorphic discriminator on the base type

    /// <summary>
    /// Optional final message indicating for example the outcome of the operation.
    /// </summary>
    [JsonPropertyName("message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; init; }
}
