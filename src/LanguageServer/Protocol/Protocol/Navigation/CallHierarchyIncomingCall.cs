// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// The item returned from a 'callHierarchy/incomingCalls' request
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#callHierarchyIncomingCall">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.16</remarks>
internal sealed class CallHierarchyIncomingCall
{
    /// <summary>
    /// The <see cref="CallHierarchyItem"/> for which to return incoming calls
    /// </summary>
    [JsonPropertyName("from")]
    [JsonRequired]
    public CallHierarchyItem From { get; init; }

    /// <summary>
    /// The ranges at which the calls appear. This is relative to the caller
    /// denoted by [`this.from`](#CallHierarchyIncomingCall.from).
    /// </summary>
    [JsonPropertyName("fromRanges")]
    [JsonRequired]

    public Range[] FromRanges { get; init; }
}
