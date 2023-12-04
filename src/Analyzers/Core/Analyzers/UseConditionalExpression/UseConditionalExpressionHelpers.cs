// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.UseConditionalExpression
{
    internal static partial class UseConditionalExpressionHelpers
    {
        public const string CanSimplifyName = nameof(CanSimplifyName);

        public static readonly ImmutableDictionary<string, string?> CanSimplifyProperties =
            ImmutableDictionary<string, string?>.Empty.Add(CanSimplifyName, CanSimplifyName);

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
        [return: NotNullIfNotNull(nameof(statement))]
        public static IOperation? UnwrapSingleStatementBlock(IOperation? statement)
            => statement is IBlockOperation { Operations: [var operationInBlock] }
                ? operationInBlock
                : statement;

        public static bool HasRegularComments(ISyntaxFacts syntaxFacts, SyntaxNode syntax)
            => HasRegularCommentTrivia(syntaxFacts, syntax.GetLeadingTrivia()) ||
               HasRegularCommentTrivia(syntaxFacts, syntax.GetTrailingTrivia());

        public static bool HasRegularCommentTrivia(ISyntaxFacts syntaxFacts, SyntaxTriviaList triviaList)
        {
            foreach (var trivia in triviaList)
            {
                if (syntaxFacts.IsRegularComment(trivia))
                    return true;
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

        public static bool IsBooleanLiteral(IOperation trueValue, bool val)
            => trueValue is ILiteralOperation { ConstantValue: { HasValue: true, Value: bool value } } && value == val;

        public static bool CanSimplify(IOperation trueValue, IOperation falseValue, bool isRef, out bool negate)
        {
            // If we are going to generate "expr ? true : false" then just generate "expr"
            // instead.
            //
            // If we are going to generate "expr ? false : true" then just generate "!expr"
            // instead.
            if (!isRef)
            {
                if (IsBooleanLiteral(trueValue, true) && IsBooleanLiteral(falseValue, false))
                {
                    negate = false;
                    return true;
                }

                if (IsBooleanLiteral(trueValue, false) && IsBooleanLiteral(falseValue, true))
                {
                    negate = true;
                    return true;
                }
            }

            negate = false;
            return false;
        }
    }
}
