// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Class which represents Moniker capabilities.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#monikerOptions">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal sealed class MonikerOptions : IWorkDoneProgressOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether work done progress is supported.
    /// </summary>
    [JsonPropertyName("workDoneProgress")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool WorkDoneProgress { get; init; }
}
