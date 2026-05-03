// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// A special workspace symbol that supports locations without a range
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#workspaceSymbol">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.17</remarks>
internal sealed class WorkspaceSymbol
{
    /// <summary>
    /// The name of this symbol.
    /// </summary>
    [JsonPropertyName("name")]
    [JsonRequired]
    public string Name { get; init; }

    /// <summary>
    /// The kind of the symbol
    /// </summary>
    [JsonPropertyName("kind")]
    [JsonRequired]
    public SymbolKind Kind { get; init; }

    /// <summary>
    /// Tags for this symbol.
    /// </summary>
    [JsonPropertyName("tags")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SymbolTag? Tags { get; init; }

    /// <summary>
    /// The name of the symbol containing this symbol. This information is for
    /// user interface purposes (e.g. to render a qualifier in the user interface
    /// if necessary). It can't be used to re-infer a hierarchy for the document
    /// symbols.
    /// </summary>
    [JsonPropertyName("containerName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ContainerName { get; init; }

    /// <summary>
    /// The location of this symbol. Whether a server is allowed to
    /// return a location without a range depends on the client
    /// workspace capability <see cref="SymbolSetting.ResolveSupport"/>
    /// </summary>
    /// <seealso cref="SymbolInformation.Location"/>
    [JsonPropertyName("location")]
    [JsonRequired]
    public SumType<Location, WorkspaceSymbolLocation> Location { get; init; }

    /// <summary>
    /// Data field that is preserved on a workspace symbol between a
    /// workspace symbol request and a workspace symbol resolve request.
    /// </summary>
    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Data { get; init; }
}
