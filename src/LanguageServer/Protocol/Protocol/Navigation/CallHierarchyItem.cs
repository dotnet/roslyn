// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Represents an item in the call hierarchy
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#callHierarchyItem">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.16</remarks>
internal sealed class CallHierarchyItem
{
    /// <summary>
    /// The name of this item.
    /// </summary>
    [JsonPropertyName("name")]
    [JsonRequired]
    public string Name { get; init; }

    /// <summary>
    /// The kind of this item.
    /// </summary>
    [JsonPropertyName("kind")]
    [JsonRequired]
    public SymbolKind Kind { get; init; }

    /// <summary>
    /// Tags for this item.
    /// </summary>
    [JsonPropertyName("tags")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SymbolTag[]? Tags { get; init; }

    /// <summary>
    /// More detail for this item, e.g. the signature of a function.
    /// </summary>
    [JsonPropertyName("detail")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Detail { get; init; }

    /// <summary>
    /// The resource identifier of this item.
    /// </summary>
    [JsonPropertyName("uri")]
    [JsonRequired]
    [JsonConverter(typeof(DocumentUriConverter))]
    public DocumentUri Uri { get; init; }

    /// <summary>
    /// The range enclosing this symbol not including leading/trailing whitespace
    /// but everything else, e.g. comments and code.
    /// </summary>
    [JsonPropertyName("range")]
    [JsonRequired]
    public Range Range { get; init; }

    /// <summary>
    /// The range that should be selected and revealed when this symbol is being
    /// picked, e.g. the name of a function. Must be contained by the
    /// <see cref="Range"/>
    /// </summary>
    [JsonPropertyName("selectionRange")]
    [JsonRequired]
    public Range SelectionRange { get; init; }

    /// <summary>
    /// A data field that is preserved between a call hierarchy prepare and
    /// incoming calls or outgoing calls requests.
    /// </summary>
    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Data { get; init; }
}
