﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.RemoveUnnecessaryParentheses;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryParentheses
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class CSharpRemoveUnnecessaryParenthesesCodeFixProvider :
        AbstractRemoveUnnecessaryParenthesesCodeFixProvider<SyntaxNode>
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpRemoveUnnecessaryParenthesesCodeFixProvider()
        {
        }

        protected override bool CanRemoveParentheses(SyntaxNode current, SemanticModel semanticModel)
            => current switch
            {
                ParenthesizedExpressionSyntax p => CSharpRemoveUnnecessaryExpressionParenthesesDiagnosticAnalyzer.CanRemoveParenthesesHelper(p, semanticModel, out _, out _),
                ParenthesizedPatternSyntax p => CSharpRemoveUnnecessaryPatternParenthesesDiagnosticAnalyzer.CanRemoveParenthesesHelper(p, out _, out _),
                _ => false,
            };
    }
}
