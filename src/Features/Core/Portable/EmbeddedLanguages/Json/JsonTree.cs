// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json
{
    internal sealed class JsonTree(
        VirtualCharSequence text,
        JsonCompilationUnit root,
        ImmutableArray<EmbeddedDiagnostic> diagnostics) : EmbeddedSyntaxTree<JsonKind, JsonNode, JsonCompilationUnit>(text, root, diagnostics)
    {
    }
}
