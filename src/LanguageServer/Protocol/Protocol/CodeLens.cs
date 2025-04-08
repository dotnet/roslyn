// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// A code lens represents a command that should be shown along with
/// source text, like the number of references, a way to run tests, etc.
/// <para>
/// A code lens is _unresolved_ when no command is associated to it. For
/// performance reasons the creation of a code lens and resolving should be done
/// in two stages.
/// </para>
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#codeLens">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal sealed class CodeLens
{
    /// <summary>
    /// The range in which this code lens is valid. Should only span a single line.
    /// </summary>
    [JsonPropertyName("range")]
    [JsonRequired]
    public Range Range
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the command associated with this code lens.
    /// </summary>
    [JsonPropertyName("command")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Command? Command
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the data that should be preserved between a textDocument/codeLens request and a codeLens/resolve request.
    /// </summary>
    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Data
    {
        get;
        set;
    }
}
