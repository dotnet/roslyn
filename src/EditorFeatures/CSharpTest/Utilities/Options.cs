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
        internal static CSharpParseOptions Script { get { return new CSharpParseOptions(kind: SourceCodeKind.Script); } }
        internal static CSharpParseOptions Interactive { get { return new CSharpParseOptions(kind: SourceCodeKind.Interactive); } }
        internal static CSharpParseOptions Regular { get { return new CSharpParseOptions(kind: SourceCodeKind.Regular); } }
    }
}
