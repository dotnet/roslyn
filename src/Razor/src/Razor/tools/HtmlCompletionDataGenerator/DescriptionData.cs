// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace HtmlSchemaGenerator;

/// <summary>
/// Represents a description entry parsed from the XSD schema annotations,
/// pairing human-readable text with an optional MDN documentation URL.
/// </summary>
internal sealed class DescriptionData
{
    /// <summary>
    /// The description text for an HTML element or attribute (from the XSD annotation).
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// An MDN documentation URL for this element or attribute, or empty if none is available.
    /// </summary>
    public string Url { get; init; } = "";
}
