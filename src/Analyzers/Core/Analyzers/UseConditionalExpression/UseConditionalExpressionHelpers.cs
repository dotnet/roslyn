// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.UseConditionalExpression
{
    internal static partial class UseConditionalExpressionHelpers
    {
        public static bool CanConvert(
            ISyntaxFacts syntaxFacts, IConditionalOperation ifOperation,
            IOperation whenTrue, IOperation whenFalse)
        {
            // Will likely not work as intended if the if directive spans any preprocessor directives. So
            // do not offer for now.  Note: we pass in both the node for the ifOperation and the
            // whenFalse portion.  The whenFalse portion isn't necessary under the ifOperation.  For
            // example in:
            //
            //  ```c#
            //  #if DEBUG
            //  if (check)
            //      return 3;
            //  #endif  
            //  return 2;
            //  ```
            //
            // In this case, we want to see that this cross the `#endif`
            if (syntaxFacts.SpansPreprocessorDirective(ifOperation.Syntax, whenFalse.Syntax))
            {
                return false;
            }

            // User may have comments on the when-true/when-false statements.  These statements can
            // be very important. Often they indicate why the true/false branches are important in
            // the first place.  We don't have any place to put these, so we don't offer here.
            if (HasRegularComments(syntaxFacts, whenTrue.Syntax) ||
                HasRegularComments(syntaxFacts, whenFalse.Syntax))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Will unwrap a block with a single statement in it to just that block.  Used so we can
        /// support both <c>if (expr) { statement }</c> and <c>if (expr) statement</c>
        /// </summary>
        public static IOperation UnwrapSingleStatementBlock(IOperation statement)
            => statement is IBlockOperation block && block.Operations.Length == 1
                ? block.Operations[0]
                : statement;

        public static IOperation UnwrapImplicitConversion(IOperation value)
            => value is IConversionOperation conversion && conversion.IsImplicit
                ? conversion.Operand
                : value;

        public static bool HasRegularComments(ISyntaxFacts syntaxFacts, SyntaxNode syntax)
            => HasRegularCommentTrivia(syntaxFacts, syntax.GetLeadingTrivia()) ||
               HasRegularCommentTrivia(syntaxFacts, syntax.GetTrailingTrivia());

        public static bool HasRegularCommentTrivia(ISyntaxFacts syntaxFacts, SyntaxTriviaList triviaList)
        {
            foreach (var trivia in triviaList)
            {
                if (syntaxFacts.IsRegularComment(trivia))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool HasInconvertibleThrowStatement(
            ISyntaxFacts syntaxFacts, bool isRef,
            IThrowOperation? trueThrow, IThrowOperation? falseThrow)
        {
            // Can't convert to `x ? throw ... : throw ...` as there's no best common type between the two (even when
            // throwing the same exception type).
            if (trueThrow != null && falseThrow != null)
                return true;

            var anyThrow = trueThrow ?? falseThrow;

            if (anyThrow != null)
            {
                // can only convert to a conditional expression if the lang supports throw-exprs.
                if (!syntaxFacts.SupportsThrowExpression(anyThrow.Syntax.SyntaxTree.Options))
                    return true;

                // `ref` can't be used with `throw`.
                if (isRef)
                    return true;
            }

            return false;
        }
    }
}
