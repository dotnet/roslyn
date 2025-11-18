// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Class representing the user configuration (as defined in <see cref="VSInternalRenameOptionSupport"/>) for a rename request.
/// </summary>
internal sealed class VSInternalRenameOptionSelection
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
    /// Gets or sets a value indicating whether the user selected the option.
    /// </summary>
    [JsonPropertyName("_vs_value")]
    [JsonRequired]
    public bool Value
    {
        get;
        set;
    }
}
