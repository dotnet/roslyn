// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// <see cref="VSServerCapabilities"/> extends <see cref="ServerCapabilities"/> allowing to provide
/// additional capabilities supported by Visual Studio.
/// </summary>
internal class VSServerCapabilities : ServerCapabilities
{
    /// <summary>
    /// Gets or sets a value indicating whether the server supports the
    /// 'textDocument/_vs_getProjectContexts' request.
    /// </summary>
    [JsonPropertyName("_vs_projectContextProvider")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool ProjectContextProvider
    {
        get;
        set;
    }
}
