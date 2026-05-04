// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Class representing settings for well-known Visual Studio's code action command.
/// </summary>
internal sealed class VSInternalExecuteCommandClientCapabilities : DynamicRegistrationSetting
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VSInternalExecuteCommandClientCapabilities"/> class.
    /// </summary>
    public VSInternalExecuteCommandClientCapabilities()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VSInternalExecuteCommandClientCapabilities"/> class.
    /// </summary>
    /// <param name="value">Value indicating whether the setting can be dynamically registered.</param>
    public VSInternalExecuteCommandClientCapabilities(bool value)
        : base(value)
    {
    }

    /// <summary>
    /// Gets or sets a set of well-known commands name the given VS-LSP client supports.
    /// </summary>
    [JsonPropertyName("_vs_supportedCommands")]
    public string[] SupportedCommands
    {
        get;
        set;
    }
}
