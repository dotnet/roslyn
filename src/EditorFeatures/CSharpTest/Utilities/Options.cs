// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests
{
    internal static class Options
    {
        internal static readonly CSharpParseOptions Script = new CSharpParseOptions(kind: SourceCodeKind.Script);
        internal static readonly CSharpParseOptions Interactive = new CSharpParseOptions(kind: SourceCodeKind.Interactive);
        internal static readonly CSharpParseOptions Regular = new CSharpParseOptions(kind: SourceCodeKind.Regular);
    }
}
