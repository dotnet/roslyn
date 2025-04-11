// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Describes the client's support for partial workspace symbols
/// </summary>
/// <remarks>Since LSP 3.17</remarks>
internal sealed class WorkspaceSymbolResolveSupport
{
    /// <summary>
    /// The properties that a client can resolve lazily. Usually `location.range`
    /// </summary>
    [JsonPropertyName("properties")]
    [JsonRequired]
    public string[] Properties { get; init; }
}
