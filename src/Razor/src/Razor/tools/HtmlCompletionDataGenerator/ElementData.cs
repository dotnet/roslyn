// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace HtmlSchemaGenerator;

/// <summary>
/// Represents a single HTML element parsed from the XSD schema, carrying its name,
/// description reference, attribute list, content model, and icon metadata.
/// </summary>
internal sealed class ElementData
{
    /// <summary>
    /// The element tag name as it appears in markup (e.g., "div", "input", "script").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Index into the shared description table, or -1 if no description is available.
    /// </summary>
    public int DescriptionId { get; init; } = -1;

    /// <summary>
    /// The attributes defined for this element in the schema.
    /// </summary>
    public List<AttributeData> Attributes { get; } = new();

    /// <summary>
    /// True if this element has a custom icon (e.g., Angular ng- elements with vs:icon="angular16.png").
    /// </summary>
    public bool HasCustomIcon { get; init; }

    /// <summary>
    /// Names of elements allowed as direct children. Empty means void/no children.
    /// A single entry of "*" means transparent content model (inherits parent's children).
    /// </summary>
    public List<string> AllowedChildren { get; } = new();

    /// <summary>
    /// Space-separated ancestor element names that disallow this element as a descendant.
    /// E.g., "form" cannot be nested inside "form"; "a" cannot be inside "a" or "button".
    /// </summary>
    public string? DisallowedAncestors { get; init; }

    /// <summary>
    /// True if this element can be implicitly closed (e.g., li, p, td, tr).
    /// When a new sibling of the same type starts, the previous one is implicitly ended.
    /// </summary>
    public bool IsImplicitlyClosed { get; set; }

    /// <summary>
    /// When true, this element's content completion is owned by an external provider.
    /// Examples: script (JavaScript), style (CSS).
    /// </summary>
    public bool HasExternalCompletion { get; init; }
}
