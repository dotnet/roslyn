// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// The item returned from a 'callHierarchy/outgoingCalls' request
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#callHierarchyOutgoingCall">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.16</remarks>
internal class CallHierarchyOutgoingCall
{
    /// <summary>
    /// The <see cref="CallHierarchyItem"/> for which to return outgoing calls
    /// </summary>
    [JsonPropertyName("to")]
    [JsonRequired]
    public CallHierarchyItem To { get; init; }

    /// <summary>
    /// The range at which this item is called. This is the range relative to
    /// the caller, e.g the item passed to the <c>callHierarchy/outgoingCalls</c> request.
    /// </summary>
    [JsonPropertyName("fromRanges")]
    [JsonRequired]
    public Range[] FromRanges { get; init; }
}

