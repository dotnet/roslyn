// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VirtualChars;

namespace Microsoft.CodeAnalysis.RegularExpressions
{
    internal sealed class RegexTree
    {
        public readonly ImmutableArray<VirtualChar> Text;
        public readonly RegexCompilationUnit Root;
        public readonly ImmutableArray<RegexDiagnostic> Diagnostics;

        public readonly ImmutableDictionary<string, TextSpan> CaptureNamesToSpan;
        public readonly ImmutableDictionary<int, TextSpan> CaptureNumbersToSpan;

        public RegexTree(
            ImmutableArray<VirtualChar> text,
            RegexCompilationUnit root,
            ImmutableArray<RegexDiagnostic> diagnostics,
            ImmutableDictionary<string, TextSpan> captureNamesToSpan,
            ImmutableDictionary<int, TextSpan> captureNumbersToSpan)
        {
            Text = text;
            Root = root;
            Diagnostics = diagnostics;
            CaptureNamesToSpan = captureNamesToSpan;
            CaptureNumbersToSpan = captureNumbersToSpan;
        }
    }
}
