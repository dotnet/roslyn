// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Inlay hint client capabilities.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#inlayHintClientCapabilities">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.17</remarks>
internal sealed class InlayHintSetting : DynamicRegistrationSetting
{
    /// <summary>
    /// Indicates which properties a client can resolve lazily on an inlay hint.
    /// </summary>
    [JsonPropertyName("resolveSupport")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public InlayHintResolveSupportSetting? ResolveSupport
    {
        get;
        set;
    }
}
