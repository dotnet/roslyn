// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json
{
    internal sealed class JsonTree : EmbeddedSyntaxTree<JsonKind, JsonNode, JsonCompilationUnit>
    {
        public JsonTree(
            VirtualCharSequence text,
            JsonCompilationUnit root,
            ImmutableArray<EmbeddedDiagnostic> diagnostics) : base(text, root, diagnostics)
        {
        }
    }
}
