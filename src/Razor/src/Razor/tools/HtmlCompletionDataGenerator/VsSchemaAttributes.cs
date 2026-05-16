// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace HtmlSchemaGenerator;

/// <summary>
/// Constants for the <c>vs:</c>-prefixed extension attributes used in the HTML Editor's XSD schemas.
/// These attributes augment the standard XSD definitions with Visual Studio-specific metadata
/// that controls how the HTML completion provider behaves.
/// </summary>
internal static class VsSchemaAttributes
{
    /// <summary>
    /// Numeric ID referencing a localized description string for the element or attribute.
    /// Used to populate tooltip/description text in completion items.
    /// </summary>
    public const string Description = "description";

    /// <summary>
    /// When "true", the element or attribute is hidden from completion lists.
    /// Used for deprecated or internal-only schema entries that shouldn't be suggested.
    /// </summary>
    public const string NonBrowseable = "nonbrowseable";

    /// <summary>
    /// When "true", the attribute is a boolean (standalone) attribute that requires no value.
    /// Examples: <c>disabled</c>, <c>checked</c>, <c>async</c>.
    /// </summary>
    public const string Standalone = "standalone";

    /// <summary>
    /// Specifies the object model type for the attribute. When set to "event", the attribute
    /// is classified as an event handler (e.g., <c>onclick</c>) and gets a distinct icon.
    /// </summary>
    public const string OmType = "omtype";

    /// <summary>
    /// File extension filter (e.g., ".js; .iced" or ".css") indicating the attribute accepts
    /// a file path value. Signals that value completion is owned by an external provider
    /// (file picker). Sets <c>HasExternalCompletion</c> on the attribute.
    /// </summary>
    public const string PreferredExtensions = "preferredextensions";

    /// <summary>
    /// When "true", the attribute or simpleType accepts multiple space-separated values
    /// (e.g., <c>rel</c>, <c>aria-relevant</c>). Signals that value completion is owned by
    /// an external provider (which handles retrigger-on-space and value exclusion).
    /// Sets <c>HasExternalCompletion</c> on the attribute.
    /// </summary>
    public const string MultiValue = "multivalue";

    /// <summary>
    /// Space-separated list of ancestor element names that disallow this element as a descendant.
    /// Used to filter out invalid nesting (e.g., <c>&lt;form&gt;</c> inside <c>&lt;form&gt;</c>).
    /// </summary>
    public const string DisallowedAncestor = "disallowedancestor";

    /// <summary>
    /// When "true", the element can be implicitly closed when a sibling of the same type begins.
    /// Examples: <c>&lt;li&gt;</c>, <c>&lt;p&gt;</c>, <c>&lt;td&gt;</c>.
    /// Affects completion by offering the parent element as a valid child (auto-closing the current one).
    /// </summary>
    public const string ImplicitClosure = "implicitclosure";

    /// <summary>
    /// Icon filename (e.g., "angular16.png") specifying a custom completion icon for elements
    /// in a schema group. Used for Angular directive elements.
    /// </summary>
    public const string Icon = "icon";

    /// <summary>
    /// Specifies the content model for the element (e.g., "phrasing", "flow").
    /// Used to determine which child elements are allowed within this element.
    /// </summary>
    public const string ContentModel = "contentmodel";
}
