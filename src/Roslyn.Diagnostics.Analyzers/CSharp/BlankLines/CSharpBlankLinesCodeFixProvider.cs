// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Roslyn.Diagnostics.Analyzers.BlankLines;

namespace Roslyn.Diagnostics.CSharp.Analyzers.BlankLines
{
    [ExportCodeFixProvider(LanguageNames.CSharp)]
    public sealed class CSharpBlankLinesCodeFixProvider : AbstractBlankLinesCodeFixProvider
    {
        protected override bool IsEndOfLine(SyntaxTrivia trivia)
            => trivia.IsKind(SyntaxKind.EndOfLineTrivia);
    }
}
