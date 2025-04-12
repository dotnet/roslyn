// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.ExternalAccess.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Extensions;

/// <summary>
/// Immutable representation of a line number and position within a document.
/// </summary>
/// <remarks>This type meant to be used exclusively by VisualStudio.Extensibility extensions.</remarks>
public readonly struct DocumentLinePosition
{
    /// <summary>
    /// Initializes a new instance of a <see cref="DocumentLinePosition"/> with the given document and line position.
    /// </summary>
    /// <param name="document">The document.</param>
    /// <param name="linePosition">The position within the document</param>
    /// <exception cref="ArgumentNullException">When <paramref name="document"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">When the document path is not available.</exception>
    public DocumentLinePosition(Document document, LinePosition linePosition)
    {
        _ = document ?? throw new ArgumentNullException(nameof(document));

        FilePath = document.FilePath ?? throw new InvalidOperationException(ExternalAccessExtensionsResources.Missing_document_file_path);
        Line = linePosition.Line;
        Character = linePosition.Character;
    }

    /// <summary>
    /// Initializes a new instance of a <see cref="DocumentLinePosition"/> with the given file path, line and character.
    /// </summary>
    /// <param name="filePath">The file path of the document.</param>
    /// <param name="line">The line number.</param>
    /// <param name="character">The character position within the line.</param>
    /// <exception cref="ArgumentNullException">When <paramref name="filePath"/> is <see langword="null"/>.</exception>
    [JsonConstructor]
    public DocumentLinePosition(string filePath, int line, int character)
    {
        FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        Line = line;
        Character = character;
    }

    /// <summary>
    /// Gets the file path of the document.
    /// </summary>
    [JsonPropertyName("filePath")]
    public string FilePath { get; }

    /// <summary>
    /// Gets the line number. The first line in a file is defined as line 0 (zero based line numbering).
    /// </summary>
    [JsonPropertyName("line")]
    public int Line { get; }

    /// <summary>
    /// Gets the character position within the line.
    /// </summary>
    [JsonPropertyName("character")]
    public int Character { get; }

    /// <summary>
    /// Converts this <see cref="DocumentLinePosition"/> to a <see cref="LinePosition"/>.
    /// </summary>
    /// <returns>The <see cref="LinePosition"/> value.</returns>
    public LinePosition ToLinePosition()
        => new LinePosition(Line, Character);

    /// <summary>
    /// Implicitly converts a <see cref="DocumentLinePosition"/> to a <see cref="LinePosition"/>.
    /// </summary>
    /// <param name="documentLinePosition">The <see cref="LinePosition"/> value.</param>
    public static implicit operator LinePosition(DocumentLinePosition documentLinePosition)
        => documentLinePosition.ToLinePosition();
}
