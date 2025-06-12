// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// A set of <see cref="FoldingRangeKind"/> values
/// </summary>
/// <remarks>Since LSP 3.17</remarks>
internal sealed class FoldingRangeKindSet
{
    /// <summary>
    /// The folding range kind values the client supports.
    /// <para>
    /// When this property exists the client also guarantees that it will
    /// handle values outside its set gracefully and falls back
    /// to a default value when unknown.
    /// </para>
    /// </summary>
    [JsonPropertyName("valueSet")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public FoldingRangeKind[]? ValueSet { get; init; }
}
