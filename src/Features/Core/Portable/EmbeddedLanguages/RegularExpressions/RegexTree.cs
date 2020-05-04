// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions
{
    internal sealed class RegexTree : EmbeddedSyntaxTree<RegexKind, RegexNode, RegexCompilationUnit>
    {
        public readonly ImmutableDictionary<string, TextSpan> CaptureNamesToSpan;
        public readonly ImmutableDictionary<int, TextSpan> CaptureNumbersToSpan;

        public RegexTree(
            VirtualCharSequence text,
            RegexCompilationUnit root,
            ImmutableArray<EmbeddedDiagnostic> diagnostics,
            ImmutableDictionary<string, TextSpan> captureNamesToSpan,
            ImmutableDictionary<int, TextSpan> captureNumbersToSpan)
            : base(text, root, diagnostics)
        {
            CaptureNamesToSpan = captureNamesToSpan;
            CaptureNumbersToSpan = captureNumbersToSpan;
        }
    }
}
