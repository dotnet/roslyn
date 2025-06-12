// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Class representing the rename options for server capabilities.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#renameOptions">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal class RenameOptions : IWorkDoneProgressOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether renames should be checked and tested before being executed.
    /// </summary>
    [JsonPropertyName("prepareProvider")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool PrepareProvider
    {
        get;
        set;
    }

    /// <inheritdoc/>
    [JsonPropertyName("workDoneProgress")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool WorkDoneProgress { get; init; }
}
