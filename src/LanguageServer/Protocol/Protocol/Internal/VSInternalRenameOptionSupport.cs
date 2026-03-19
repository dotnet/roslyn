// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Class representing a renaming option for customizing the edit in the 'textDocument/rename' request.
/// </summary>
internal sealed class VSInternalRenameOptionSupport
{
    /// <summary>
    /// Gets or sets the name that identifies the option.
    /// </summary>
    [JsonPropertyName("_vs_name")]
    [JsonRequired]
    public string Name
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the user-facing option label.
    /// </summary>
    [JsonPropertyName("_vs_label")]
    [JsonRequired]
    public string Label
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the option has a default value of <c>true</c>.
    /// </summary>
    [JsonPropertyName("_vs_default")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Default
    {
        get;
        set;
    }
}
