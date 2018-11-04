// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.RemoveUnnecessaryParentheses.ParenthesizedExpression;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryParentheses
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class CSharpRemoveUnnecessaryParenthesesCodeFixProvider :
        AbstractRemoveUnnecessaryParenthesesCodeFixProvider<ParenthesizedExpressionSyntax>
    {
        protected override ISyntaxFactsService GetSyntaxFactsService()
            => CSharpSyntaxFactsService.Instance;

        protected override bool CanRemoveParentheses(ParenthesizedExpressionSyntax current, SemanticModel semanticModel)
        {
            return CSharpRemoveUnnecessaryParenthesesDiagnosticAnalyzer.CanRemoveParenthesesHelper(
                current, semanticModel, out _, out _);
        }
    }
}
