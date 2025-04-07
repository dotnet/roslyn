// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Describes the tags supported by the client on <see cref="SymbolInformation"/> and <see cref="WorkspaceSymbol"/>.
/// </summary>
/// <remarks>Since LSP 3.16</remarks>
internal class SymbolTagSupport
{
    /// <summary>
    /// The tags supported by the client.
    /// </summary>
    [JsonPropertyName("valueSet")]
    public SymbolTag[] ValueSet { get; init; }
}
