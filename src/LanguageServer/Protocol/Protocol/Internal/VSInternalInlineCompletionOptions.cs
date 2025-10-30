// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

/// <summary>
/// The options for inline completion.
/// </summary>
internal class VSInternalInlineCompletionOptions
{
    /// <summary>
    /// Gets or sets a regex used by the client to determine when to ask the server for snippets.
    /// </summary>
    [JsonPropertyName("_vs_pattern")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonConverter(typeof(RegexConverter))]
    public Regex Pattern { get; set; }
}
