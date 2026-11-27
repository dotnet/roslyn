// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using System.Collections.Immutable;

namespace Microsoft.NET.Sdk.Razor.SourceGenerators
{
    internal sealed class RazorGeneratorResult(TagHelperCollection tagHelpers, ImmutableDictionary<string, (string hintName, RazorCodeDocument document)> filePathToDocument, ImmutableDictionary<string, string> hintNameToFilePath)
    {
        public TagHelperCollection TagHelpers => tagHelpers;

        public RazorCodeDocument? GetCodeDocument(string physicalPath) => filePathToDocument.TryGetValue(physicalPath, out var pair) ? pair.document : null;

        public string? GetHintName(string physicalPath) => filePathToDocument.TryGetValue(physicalPath, out var pair) ? pair.hintName : null;

        public string? GetFilePath(string hintName) => hintNameToFilePath.TryGetValue(hintName, out var filePath) ? filePath : null;
    }
}
