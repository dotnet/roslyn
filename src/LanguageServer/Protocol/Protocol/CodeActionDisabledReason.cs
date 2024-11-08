// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Indicates why a code action is disabled
/// </summary>
class CodeActionDisabledReason
{

    /// <summary>
    /// Human readable description of why the code action is currently
    /// disabled.
    /// <para>
    /// This is displayed in the code actions UI.
    /// </para>
    /// </summary>
    [JsonPropertyName("reason")]
    [JsonRequired]
    public string Reason { get; init; }
}
