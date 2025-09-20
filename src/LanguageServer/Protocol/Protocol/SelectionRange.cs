// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// A selection range is a range around the cursor position which the user might be interested in selecting.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#selectionRangeRegistrationOptions">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.15</remarks>
internal sealed class SelectionRange
{
    /// <summary>
    /// The range of the selection range
    /// </summary>
    [JsonPropertyName("range")]
    [JsonRequired]
    public Range Range { get; init; }

    /// <summary>
    /// The parent selection range containing this range.
    /// <para>
    /// <c>Parent.Range</c> must contain <c>this.Range</c>.
    /// </para>
    /// </summary>
    [JsonPropertyName("parent")]
    [JsonRequired]
    public SelectionRange Parent { get; init; }
}
