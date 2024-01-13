// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions
{
    internal sealed class RegexTree(
        VirtualCharSequence text,
        RegexCompilationUnit root,
        ImmutableArray<EmbeddedDiagnostic> diagnostics,
        ImmutableDictionary<string, TextSpan> captureNamesToSpan,
        ImmutableDictionary<int, TextSpan> captureNumbersToSpan) : EmbeddedSyntaxTree<RegexKind, RegexNode, RegexCompilationUnit>(text, root, diagnostics)
    {
        public readonly ImmutableDictionary<string, TextSpan> CaptureNamesToSpan = captureNamesToSpan;
        public readonly ImmutableDictionary<int, TextSpan> CaptureNumbersToSpan = captureNumbersToSpan;
    }
}
