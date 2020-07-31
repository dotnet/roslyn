// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Diagnostics.Analyzers.BlankLines;

namespace Roslyn.Diagnostics.CSharp.Analyzers.BlankLines
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CSharpBlankLinesDiagnosticAnalyzer : AbstractBlankLinesDiagnosticAnalyzer
    {
        protected override bool IsEndOfLine(SyntaxTrivia trivia)
            => trivia.IsKind(SyntaxKind.EndOfLineTrivia);
    }
}
