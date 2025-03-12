// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CustomMessageHandler;

public struct DocumentLinePosition
{
    public DocumentLinePosition(Document document, LinePosition linePosition)
    {
        _ = document ?? throw new ArgumentNullException(nameof(document));

        FilePath = document.FilePath ?? throw new InvalidOperationException("Missing document file path");
        Line = linePosition.Line;
        Character = linePosition.Character;
    }

    [JsonConstructor]
    public DocumentLinePosition(string filePath, int line, int character)
    {
        FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        Line = line;
        Character = character;
    }

    [JsonPropertyName("filePath")]
    public string FilePath { get; }

    [JsonPropertyName("line")]
    [DataMember(Order = 1)]
    public int Line { get; }

    [JsonPropertyName("character")]
    [DataMember(Order = 2)]
    public int Character { get; }

    public LinePosition ToLinePosition()
        => new LinePosition(Line, Character);

    public static implicit operator LinePosition(DocumentLinePosition documentLinePosition)
        => documentLinePosition.ToLinePosition();
}
