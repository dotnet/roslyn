// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VirtualChars;

namespace Microsoft.CodeAnalysis.Json
{
    internal sealed class JsonTree
    {
        public readonly ImmutableArray<VirtualChar> Text;
        public readonly JsonCompilationUnit Root;
        public readonly ImmutableArray<JsonDiagnostic> Diagnostics;

        public JsonTree(
            ImmutableArray<VirtualChar> text,
            JsonCompilationUnit root,
            ImmutableArray<JsonDiagnostic> diagnostics)
        {
            Text = text;
            Root = root;
            Diagnostics = diagnostics;
        }
    }
}
