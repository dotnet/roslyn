// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.RemoveUnnecessaryParentheses;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryParentheses
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class CSharpRemoveUnnecessaryParenthesesCodeFixProvider :
        AbstractRemoveUnnecessaryParenthesesCodeFixProvider<ParenthesizedExpressionSyntax>
    {
        [ImportingConstructor]
        public CSharpRemoveUnnecessaryParenthesesCodeFixProvider()
        {
        }

        protected override bool CanRemoveParentheses(ParenthesizedExpressionSyntax current, SemanticModel semanticModel)
        {
            return CSharpRemoveUnnecessaryParenthesesDiagnosticAnalyzer.CanRemoveParenthesesHelper(
                current, semanticModel, out _, out _);
        }
    }
}
