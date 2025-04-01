// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Client capabilities specific to the `inlayHint/resolve` request.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#inlayHintClientCapabilities">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.17</remarks>
internal sealed class InlayHintResolveSupportSetting
{
    /// <summary>
    /// The names of the <see cref="InlayHint"/> properties that the client can resolve lazily.
    /// </summary>
    [JsonPropertyName("properties")]
    public string[] Properties
    {
        get;
        set;
    }
}
