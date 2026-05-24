// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace HtmlSchemaGenerator;

/// <summary>
/// Intermediate representation of the parsed HTML schema data. Produced by
/// <see cref="SchemaParser"/> and consumed by <see cref="CodeEmitter"/>
/// to generate the compiled completion data source files.
/// </summary>
internal sealed class HtmlSchema
{
    /// <summary>
    /// All HTML elements defined in the schema, each with its attributes, content model, and metadata.
    /// </summary>
    public ImmutableArray<ElementData> Elements { get; init; }

    /// <summary>
    /// Attributes that apply to all elements (e.g., "id", "class", "style", "title").
    /// </summary>
    public ImmutableArray<AttributeData> GlobalAttributes { get; init; }

    /// <summary>
    /// Description entries keyed by ID. Elements and attributes reference these by index
    /// to avoid duplicating description text across the schema.
    /// </summary>
    public Dictionary<int, DescriptionData> Descriptions { get; init; } = new();

    /// <summary>
    /// Named content groups (e.g., "flowContent" → list of allowed child element names).
    /// Used by the code emitter to create shared arrays.
    /// </summary>
    public Dictionary<string, List<string>> ContentGroups { get; init; } = new();
}

