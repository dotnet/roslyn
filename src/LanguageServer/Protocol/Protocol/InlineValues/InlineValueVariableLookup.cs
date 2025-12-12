// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Provide inline value through a variable lookup.
/// <para>
/// If only a range is specified, the variable name will be extracted from
/// the underlying document.
/// </para>
/// <para>
/// An optional variable name can be used to override the extracted name.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.17</remarks>
internal sealed class InlineValueVariableLookup
{
    /// <summary>
    /// The document range for which the inline value applies.
    /// <para>
    /// The range is used to extract the variable name from the underlying document.
    /// </para>
    /// </summary>
    [JsonPropertyName("range")]
    [JsonRequired]
    public Range Range { get; init; }

    /// <summary>
    /// If specified the name of the variable to look up.
    /// </summary>
    [JsonPropertyName("variableName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? VariableName { get; init; }

    /// <summary>
    /// How to perform the lookup.
    /// </summary>
    [JsonPropertyName("caseSensitiveLookup")]
    [JsonRequired]
    public bool CaseSensitiveLookup { get; init; }
}
