// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// A relative pattern is a helper to construct glob patterns that are matched
/// relatively to a base URI. The common value for a <see cref="BaseUri"/> is a workspace
/// folder root, but it can be another absolute URI as well.
/// </summary>
/// <remarks>Since LSP 3.17</remarks>
internal class RelativePattern
{
    /// <summary>
    /// A workspace folder or a base URI to which this pattern will be matched
    /// against relatively.
    /// </summary>
    [JsonPropertyName("baseUri")]
    [JsonRequired]
    public SumType<Uri, WorkspaceFolder> BaseUri { get; init; }

    /// <summary>
    /// The actual glob pattern. See <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#pattern">Glob Pattern</see> for more detail.
    /// </summary>
    [JsonPropertyName("pattern")]
    [JsonRequired]
    public string Pattern { get; init; }
}
