// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Client capabilities specific to <see cref="ParameterInformation"/>
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#signatureHelpClientCapabilities">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.14</remarks>
internal sealed class ParameterInformationSetting
{
    /// <summary>
    /// The client supports processing label offsets instead of a simple label string.
    /// </summary>
    /// <remarks>Since LSP 3.14</remarks>
    [JsonPropertyName("labelOffsetSupport")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool LabelOffsetSupport
    {
        get;
        set;
    }
}
