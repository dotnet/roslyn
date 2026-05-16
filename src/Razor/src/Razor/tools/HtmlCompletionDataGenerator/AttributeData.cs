// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace HtmlSchemaGenerator;

/// <summary>
/// Represents a single HTML attribute parsed from the XSD schema, carrying its name,
/// description reference, type flags, allowed values, and icon metadata.
/// </summary>
internal sealed class AttributeData
{
    /// <summary>
    /// The attribute name as it appears in markup (e.g., "class", "aria-label", "onclick").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Index into the shared description table, or -1 if no description is available.
    /// </summary>
    public int DescriptionId { get; init; } = -1;

    /// <summary>
    /// True for boolean attributes that don't require a value (e.g., "disabled", "readonly").
    /// </summary>
    public bool IsBoolean { get; init; }

    /// <summary>
    /// True for event-handler attributes (e.g., "onclick", "onchange").
    /// </summary>
    public bool IsEvent { get; init; }

    /// <summary>
    /// True when this attribute's value completion is owned by an external provider
    /// (e.g., file-path attributes with <c>vs:preferredextensions</c>, or <c>class</c>/<c>style</c>
    /// which need CSS intellisense from the project context).
    /// </summary>
    public bool HasExternalCompletion { get; init; }

    /// <summary>
    /// The schema-level <c>vs:icon</c> filename for this attribute's supplemental schema, if any
    /// (e.g., "aria16.png", "angular16.png"). Used to select the completion list icon.
    /// </summary>
    public string? Icon { get; init; }

    /// <summary>
    /// The XSD simpleType name that produced <see cref="Values"/>, if any (e.g., "inputTypeType").
    /// Used for naming shared value array fields in codegen.
    /// </summary>
    public string? ValueTypeName { get; set; }

    /// <summary>
    /// The set of allowed values for this attribute, parsed from the XSD enumeration facets.
    /// Empty when the attribute accepts free-form text.
    /// </summary>
    public List<string> Values { get; } = new();
}
