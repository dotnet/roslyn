// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Provide an inline value through an expression evaluation.
/// <para>
/// If only a range is specified, the expression will be extracted from the
/// underlying document.
/// </para>
/// <para>
/// An optional expression can be used to override the extracted expression.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.17</remarks>
internal class InlineValueEvaluatableExpression
{
    /// <summary>
    /// The document range for which the inline value applies.
    /// <para>
    /// The range is used to extract the evaluatable expression from the
    /// underlying document.
    /// </para>
    /// </summary>
    [JsonPropertyName("range")]
    [JsonRequired]
    public Range Range { get; init; }

    /// <summary>
    /// If specified the expression overrides the extracted expression.
    /// </summary>
    [JsonPropertyName("expression")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Expression { get; init; }
}
