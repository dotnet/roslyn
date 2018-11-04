// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.RemoveUnnecessaryParentheses;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryParentheses.IfStatement
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class CSharpRemoveUnnecessaryParenthesesCodeFixProvider :
        AbstractRemoveUnnecessaryParenthesesCodeFixProvider<IfStatementSyntax>
    {
        public CSharpRemoveUnnecessaryParenthesesCodeFixProvider()
            : base(Constants.IfStatement)
        {
        }

        protected override ISyntaxFactsService GetSyntaxFactsService()
            => CSharpSyntaxFactsService.Instance;

        protected override bool CanRemoveParentheses(IfStatementSyntax current, SemanticModel semanticModel)
        {
            return CSharpRemoveUnnecessaryParenthesesDiagnosticAnalyzer.CanRemoveParenthesesHelper(
                current, semanticModel, out _, out _);
        }

        protected override SyntaxNode Unparenthesize(IfStatementSyntax current)
        {
            var openParenTrivia = current.OpenParenToken.GetAllTrivia();
            var closeParenTrivia = current.CloseParenToken.GetAllTrivia();
            var condition = current.Condition
                .WithPrependedLeadingTrivia(openParenTrivia)
                .WithAppendedTrailingTrivia(closeParenTrivia);

            return current.WithOpenParenToken(default)
                          .WithCloseParenToken(default)
                          .WithCondition(condition);
        }
    }
}
