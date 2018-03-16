// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;

namespace Microsoft.CodeAnalysis.Json
{
    internal sealed class JsonTree : EmbeddedSyntaxTree<JsonKind, JsonNode, JsonCompilationUnit>
    {
        public JsonTree(
            ImmutableArray<VirtualChar> text,
            JsonCompilationUnit root,
            ImmutableArray<EmbeddedDiagnostic> diagnostics) : base(text, root, diagnostics)
        {
        }
    }
}
