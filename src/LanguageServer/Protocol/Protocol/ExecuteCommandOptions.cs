// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Class representing the options for execute command support.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#executeCommandOptions">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal class ExecuteCommandOptions : IWorkDoneProgressOptions
{
    /// <summary>
    /// Gets or sets the commands that are to be executed on the server.
    /// </summary>
    [JsonPropertyName("commands")]
    public string[] Commands
    {
        get;
        set;
    }

    /// <inheritdoc/>
    [JsonPropertyName("workDoneProgress")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool WorkDoneProgress { get; init; }
}
