// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Used to report progress of an operation to an <c>IProgress&lt;WorkDoneProgress></c>.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#workDoneProgressReport">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.15</remarks>
internal sealed class WorkDoneProgressReport : WorkDoneProgress
{
    // NOTE: the kind property from the spec is used as a JsonPolymorphic discriminator on the base type

    /// <summary>
    /// Controls enablement state of a cancel button. This property is only valid
    /// if a cancel button got requested in the <see cref="WorkDoneProgressBegin"/> payload.
    /// Clients that don't support cancellation or don't support control the
    /// button's enablement state are allowed to ignore the setting.
    /// </summary>
    [JsonPropertyName("cancellable")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Cancellable { get; init; }

    /// <summary>
    /// Optional, more detailed associated progress message. Contains
    /// complementary information to the `title`.
    ///
    /// Examples: "3/25 files", "project/src/module2", "node_modules/some_dep".
    /// 
    /// If unset, the previous progress message (if any) is still valid.
    /// </summary>
    [JsonPropertyName("message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; init; }

    /// <summary>
    /// Optional progress percentage to display (value 100 is considered 100%).
    /// If not provided infinite progress is assumed and clients are allowed
    /// to ignore the `percentage` value in subsequent in report notifications.
    /// 
    /// The value should be steadily rising. Clients are free to ignore values
    /// that are not following this rule. The value range is [0, 100]
    /// </summary>
    [JsonPropertyName("percentage")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Percentage { get; init; }
}
