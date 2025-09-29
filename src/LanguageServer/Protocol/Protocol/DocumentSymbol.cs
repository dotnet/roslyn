// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System;
using System.Text.Json.Serialization;

/// <summary>
/// Represents programming constructs like variables, classes, interfaces etc. that appear in a document. Document symbols can be
/// hierarchical and they have two ranges: one that encloses its definition and one that points to its most interesting range,
/// e.g. the range of an identifier.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#documentSymbol">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal class DocumentSymbol
{
    /// <summary>
    /// The name of this symbol. Will be displayed in the user interface and
    /// therefore must not be an empty string or a string consisting only of whitespace.
    /// </summary>
    [JsonPropertyName("name")]
    [JsonRequired]
    public string Name
    {
        get;
        set;
    }

    /// <summary>
    /// More detail for this symbol, e.g the signature of a function.
    /// </summary>
    [JsonPropertyName("detail")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Detail
    {
        get;
        set;
    }

    /// <summary>
    /// The <see cref="SymbolKind" /> of this symbol.
    /// </summary>
    [JsonPropertyName("kind")]
    public SymbolKind Kind
    {
        get;
        set;
    }

    /// <summary>
    /// Tags for this document symbol.
    /// </summary>
    /// <remarks>Since 3.16</remarks>
    [JsonPropertyName("tags")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SymbolTag[]? Tags { get; init; }

    /// <summary>
    /// Indicates whether this symbol is deprecated.
    /// </summary>
    [JsonPropertyName("deprecated")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [Obsolete("Use Tags instead")]
    public bool Deprecated
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the range enclosing this symbol not including leading/trailing whitespace
    /// but everything else like comments. This information is typically used to determine
    /// if the client's cursor is inside the symbol to reveal in the symbol in the UI.
    /// </summary>
    [JsonPropertyName("range")]
    [JsonRequired]
    public Range Range
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the range that should be selected and revealed when this symbol is being picked, e.g the name of a function.
    /// Must be contained by the <see cref="Range"/>.
    /// </summary>
    [JsonPropertyName("selectionRange")]
    [JsonRequired]
    public Range SelectionRange
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the children of this symbol, e.g. properties of a class.
    /// </summary>
    [JsonPropertyName("children")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DocumentSymbol[]? Children
    {
        get;
        set;
    }
}
