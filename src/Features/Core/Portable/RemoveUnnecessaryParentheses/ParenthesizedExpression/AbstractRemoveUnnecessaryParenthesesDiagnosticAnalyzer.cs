// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.RemoveUnnecessaryParentheses.ParenthesizedExpression
{
    internal abstract class AbstractRemoveUnnecessaryParenthesesDiagnosticAnalyzer<
        TLanguageKindEnum,
        TParenthesizedExpressionSyntax>
        : RemoveUnnecessaryParentheses.AbstractRemoveUnnecessaryParenthesesDiagnosticAnalyzer<TLanguageKindEnum, TParenthesizedExpressionSyntax>
        where TLanguageKindEnum : struct
        where TParenthesizedExpressionSyntax : SyntaxNode
    {
        protected AbstractRemoveUnnecessaryParenthesesDiagnosticAnalyzer()
            : base(Constants.ParenthesizedExpression)
        {
        }

        protected sealed override bool ShouldNotRemoveParentheses(
            TParenthesizedExpressionSyntax parenthesizedExpression, PrecedenceKind precedence)
        {
            // Do not remove parentheses from these expressions when there are different kinds
            // between the parent and child of the parenthesized expr..  This is because removing
            // these parens can significantly decrease readability and can confuse many people
            // (including several people quizzed on Roslyn).  For example, most people see
            // "1 + 2 << 3" as "1 + (2 << 3)", when it's actually "(1 + 2) << 3".  To avoid 
            // making code bases more confusing, we just do not touch parens for these constructs 
            // unless both the child and parent have the same kinds.
            switch (precedence)
            {
                case PrecedenceKind.Shift:
                case PrecedenceKind.Bitwise:
                case PrecedenceKind.Coalesce:
                    var syntaxFacts = this.GetSyntaxFactsService();
                    var child = syntaxFacts.GetExpressionOfParenthesizedExpression(parenthesizedExpression);

                    var parentKind = parenthesizedExpression.Parent.RawKind;
                    var childKind = child.RawKind;
                    if (parentKind != childKind)
                    {
                        return true;
                    }

                    // Ok to remove if it was the exact same kind.  i.e. ```(a | b) | c```
                    // not ok to remove if kinds changed.  i.e. ```(a + b) << c```
                    break;
            }

            return false;
        }
    }
}
