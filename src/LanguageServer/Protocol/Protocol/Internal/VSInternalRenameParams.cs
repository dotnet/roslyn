// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Class representing the parameters (together with extra VS-specific options) sent for the
/// 'textDocument/rename' request.
/// </summary>
internal sealed class VSInternalRenameParams : RenameParams
{
    /// <summary>
    /// Gets or sets the rename option values as selected by the user.
    /// </summary>
    [JsonPropertyName("_vs_optionSelections")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public VSInternalRenameOptionSelection[]? OptionSelections
    {
        get;
        set;
    }
}
