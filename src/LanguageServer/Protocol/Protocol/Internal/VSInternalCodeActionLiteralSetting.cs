// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Class representing support for code action literals.
/// </summary>
internal sealed class VSInternalCodeActionLiteralSetting : CodeActionLiteralSetting
{
    /// <summary>
    /// Gets or sets a value indicating what code action default groups are supported.
    /// </summary>
    [JsonPropertyName("_vs_codeActionGroup")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public VSInternalCodeActionGroupSetting? CodeActionGroup
    {
        get;
        set;
    }
}
