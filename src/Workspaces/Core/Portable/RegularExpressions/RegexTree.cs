// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.RegularExpressions
{
    internal sealed class RegexTree
    {
        public readonly RegexCompilationUnit Root;
        public readonly ImmutableArray<RegexDiagnostic> Diagnostics;

        public RegexTree(RegexCompilationUnit root, ImmutableArray<RegexDiagnostic> diagnostics)
        {
            Root = root;
            Diagnostics = diagnostics;
        }
    }
}
