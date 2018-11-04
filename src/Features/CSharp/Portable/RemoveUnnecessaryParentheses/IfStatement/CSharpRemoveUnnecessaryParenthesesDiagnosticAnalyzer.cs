// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.RemoveUnnecessaryParentheses;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryParentheses.IfStatement
{
    using PrecedenceKind = Microsoft.CodeAnalysis.RemoveUnnecessaryParentheses.PrecedenceKind;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpRemoveUnnecessaryParenthesesDiagnosticAnalyzer
        : AbstractRemoveUnnecessaryParenthesesDiagnosticAnalyzer<SyntaxKind, IfStatementSyntax>
    {
        public CSharpRemoveUnnecessaryParenthesesDiagnosticAnalyzer()
            : base(Constants.IfStatement)
        {
        }

        protected override ISyntaxFactsService GetSyntaxFactsService()
            => CSharpSyntaxFactsService.Instance;

        protected override SyntaxKind GetSyntaxNodeKind()
            => SyntaxKind.IfStatement;

        protected override bool ShouldNotRemoveParentheses(IfStatementSyntax construct, PrecedenceKind precedence)
            => false;

        protected override bool CanRemoveParentheses(
            IfStatementSyntax ifStatement, SemanticModel semanticModel,
            out PrecedenceKind precedence, out bool clarifiesPrecedence)
        {
            return CanRemoveParenthesesHelper(
                ifStatement, semanticModel,
                out precedence, out clarifiesPrecedence);
        }

        public static bool CanRemoveParenthesesHelper(
            IfStatementSyntax ifStatement, SemanticModel semanticModel,
            out PrecedenceKind precedence, out bool clarifiesPrecedence)
        {
            clarifiesPrecedence = false;
            precedence = PrecedenceKind.Other;

            return ifStatement.OpenParenToken != default &&
                   ifStatement.Condition.IsValidIfGuardCondition() &&
                   ((CSharpParseOptions)ifStatement.SyntaxTree.Options).LanguageVersion >= LanguageVersion.CSharp8;
        }
    }
}
