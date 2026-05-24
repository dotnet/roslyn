// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.Razor.NestedFiles;

namespace Microsoft.CodeAnalysis.Razor.Protocol.NestedFiles;

/// <summary>
/// Parameters for the razor/addNestedFile endpoint.
/// </summary>
internal sealed class AddNestedFileParams : ITextDocumentParams
{
    /// <summary>
    /// The text document identifier for the Razor file (.razor or .cshtml) to create a nested file for.
    /// </summary>
    [JsonPropertyName("textDocument")]
    public required TextDocumentIdentifier TextDocument { get; set; }

    /// <summary>
    /// The kind of nested file to create.
    /// </summary>
    [JsonPropertyName("fileKind")]
    public required NestedFileKind FileKind { get; set; }

    public static AddNestedFileParams Create(Uri razorFileUri, NestedFileKind fileKind)
    {
        return new AddNestedFileParams
        {
            TextDocument = new TextDocumentIdentifier { DocumentUri = new(razorFileUri) },
            FileKind = fileKind
        };
    }
}
