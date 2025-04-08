// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Class used to extend <see cref="FoldingRangeSetting" /> to add internal capabilities.
/// </summary>
internal sealed class VSFoldingRangeSetting : FoldingRangeSetting
{
    /// <summary>
    /// Gets or sets a value indicating whether if client only supports entire line folding only.
    /// </summary>
    [JsonPropertyName("_vs_refreshSupport")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool RefreshSupport
    {
        get;
        set;
    }
}
