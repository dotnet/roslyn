// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Matching options for the file operation pattern.
/// 
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#fileOperationPatternOptions">Language Server Protocol specification</see> for additional information.
/// </summary>
/// <remarks>Since LSP 3.16</remarks>
internal sealed class FileOperationPatternOptions
{
    /// <summary>
    /// The pattern should be matched ignoring casing.
    /// </summary>
    [JsonPropertyName("ignoreCase")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IgnoreCase { get; init; }
}
