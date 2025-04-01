// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// <see cref="VSLocation"/> extends <see cref="Location"/> providing additional properties used by Visual Studio.
/// </summary>
internal class VSLocation : Location
{
    /// <summary>
    /// Gets or sets the project name to be displayed to user.
    /// </summary>
    [JsonPropertyName("_vs_projectName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ProjectName { get; set; }

    /// <summary>
    /// Gets or sets the text value for the display path.
    /// In case the actual path on disk would be confusing for users, this should be a friendly display name.
    /// This doesn't have to correspond to a real file path, but must be parsable by the <see cref="System.IO.Path.GetFileName(string)" /> method.
    /// </summary>
    [JsonPropertyName("_vs_displayPath")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DisplayPath { get; set; }
}