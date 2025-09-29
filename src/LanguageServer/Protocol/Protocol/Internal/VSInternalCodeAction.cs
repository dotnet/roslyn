// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System;
using System.Text.Json.Serialization;

/// <summary>
/// Class used to extend <see cref="CodeAction" /> to add the data field for codeAction/_ms_resolve support.
/// </summary>
/// <remarks>Do not seal this type! This is extended by Razor</remarks>
internal class VSInternalCodeAction : CodeAction
{
    /// <summary>
    /// Gets or sets the group this CodeAction belongs to.
    /// </summary>
    [JsonPropertyName("_vs_group")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Group
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the priority level of the code action.
    /// </summary>
    [JsonPropertyName("_vs_priority")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public VSInternalPriorityLevel? Priority
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the range of the span this action is applicable to.
    /// </summary>
    [JsonPropertyName("_vs_applicableRange")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Range? ApplicableRange
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the children of this action.
    /// </summary>
    [JsonPropertyName("_vs_children")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public VSInternalCodeAction[]? Children
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the telemetry id of this action.
    /// </summary>
    [JsonPropertyName("_vs_telemetryId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? TelemetryId
    {
        get;
        set;
    }
}
