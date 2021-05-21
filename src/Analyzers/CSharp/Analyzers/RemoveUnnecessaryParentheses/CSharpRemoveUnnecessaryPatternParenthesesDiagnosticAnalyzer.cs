﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Precedence;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Precedence;
using Microsoft.CodeAnalysis.RemoveUnnecessaryParentheses;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryParentheses
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpRemoveUnnecessaryPatternParenthesesDiagnosticAnalyzer
        : AbstractRemoveUnnecessaryParenthesesDiagnosticAnalyzer<SyntaxKind, ParenthesizedPatternSyntax>
    {
        protected override SyntaxKind GetSyntaxKind()
            => SyntaxKind.ParenthesizedPattern;

        protected override ISyntaxFacts GetSyntaxFacts()
            => CSharpSyntaxFacts.Instance;

        protected override bool CanRemoveParentheses(
            ParenthesizedPatternSyntax parenthesizedExpression, SemanticModel semanticModel,
            out PrecedenceKind precedence, out bool clarifiesPrecedence)
        {
            return CanRemoveParenthesesHelper(
                parenthesizedExpression,
                out precedence, out clarifiesPrecedence);
        }

        public static bool CanRemoveParenthesesHelper(
            ParenthesizedPatternSyntax parenthesizedPattern,
            out PrecedenceKind parentPrecedenceKind, out bool clarifiesPrecedence)
        {
            var result = parenthesizedPattern.CanRemoveParentheses();
            if (!result)
            {
                parentPrecedenceKind = default;
                clarifiesPrecedence = false;
                return false;
            }

            var inner = parenthesizedPattern.Pattern;
            var innerPrecedence = inner.GetOperatorPrecedence();
            var innerIsSimple = innerPrecedence == OperatorPrecedence.Primary ||
                                innerPrecedence == OperatorPrecedence.None;

            if (!(parenthesizedPattern.Parent is PatternSyntax))
            {
                // We're parented by something not a pattern.  i.e. `x is (...)` or `case (...)`.
                // These parentheses are never needed for clarity and can always be removed.
                parentPrecedenceKind = PrecedenceKind.Other;
                clarifiesPrecedence = false;
                return true;
            }

            if (!(parenthesizedPattern.Parent is BinaryPatternSyntax parentPattern))
            {
                // We're parented by something other than a BinaryPattern.  These parentheses are never needed for
                // clarity and can always be removed.
                parentPrecedenceKind = PrecedenceKind.Other;
                clarifiesPrecedence = false;
                return true;
            }

            // We're parented by something binary-like. 
            parentPrecedenceKind = CSharpPatternPrecedenceService.Instance.GetPrecedenceKind(parentPattern);

            // Precedence is clarified any time we have expression with different precedence
            // (and the inner expression is not a primary expression).  in other words, this
            // is helps clarify precedence:
            //
            //      a or (b and c)
            //
            // However, this does not:
            //
            //      a or (b)
            clarifiesPrecedence = !innerIsSimple &&
                                  parentPattern.GetOperatorPrecedence() != innerPrecedence;
            return true;
        }
    }
}
