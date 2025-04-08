// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Class which represents save option configurations.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#saveOptions">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal sealed class SaveOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether clients include text content on save.
    /// </summary>
    [JsonPropertyName("includeText")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IncludeText
    {
        get;
        set;
    }
}
