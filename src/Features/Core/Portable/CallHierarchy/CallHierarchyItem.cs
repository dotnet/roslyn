// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CallHierarchy;

/// <summary>
/// Represents a single item in a call hierarchy.
/// This is a shared data type used by both LSP and VS implementations.
/// </summary>
internal readonly struct CallHierarchyItem
{
    /// <summary>
    /// The name of the symbol.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The symbol kind.
    /// </summary>
    public SymbolKind Kind { get; }

    /// <summary>
    /// Additional detail for this item (e.g., the signature of a function).
    /// </summary>
    public string? Detail { get; }

    /// <summary>
    /// The glyph for this item, used to determine the icon.
    /// </summary>
    public Glyph Glyph { get; }

    /// <summary>
    /// The document containing the definition.
    /// </summary>
    public DocumentId DocumentId { get; }

    /// <summary>
    /// The span of the entire definition.
    /// </summary>
    public Text.TextSpan Span { get; }

    /// <summary>
    /// The span of the name/identifier to select.
    /// </summary>
    public Text.TextSpan SelectionSpan { get; }

    /// <summary>
    /// The symbol key that can be used to resolve this symbol later.
    /// </summary>
    public SymbolKey SymbolKey { get; }

    /// <summary>
    /// The project containing this symbol.
    /// </summary>
    public ProjectId ProjectId { get; }

    /// <summary>
    /// The display name of the containing type.
    /// </summary>
    public string? ContainingTypeName { get; }

    /// <summary>
    /// The display name of the containing namespace.
    /// </summary>
    public string? ContainingNamespaceName { get; }

    public CallHierarchyItem(
        string name,
        SymbolKind kind,
        string? detail,
        Glyph glyph,
        DocumentId documentId,
        Text.TextSpan span,
        Text.TextSpan selectionSpan,
        SymbolKey symbolKey,
        ProjectId projectId,
        string? containingTypeName,
        string? containingNamespaceName)
    {
        Name = name;
        Kind = kind;
        Detail = detail;
        Glyph = glyph;
        DocumentId = documentId;
        Span = span;
        SelectionSpan = selectionSpan;
        SymbolKey = symbolKey;
        ProjectId = projectId;
        ContainingTypeName = containingTypeName;
        ContainingNamespaceName = containingNamespaceName;
    }
}
