// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// A setting that can be dynamically registered via the `client/registerCapability` method.
/// </summary>
internal interface IDynamicRegistrationSetting
{
    /// <summary>
    /// Whether the implementation supports dynamic registration.
    /// </summary>
    [JsonPropertyName("dynamicRegistration")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool DynamicRegistration { get; set; }
}
