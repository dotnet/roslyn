// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.UseConditionalExpression
{
    internal static class UseConditionalExpressionHelpers
    {
        public static readonly SyntaxAnnotation SpecializedFormattingAnnotation = new SyntaxAnnotation();

        public static bool CanConvert(
            ISyntaxFactsService syntaxFacts, IConditionalOperation ifOperation,
            IOperation whenTrue, IOperation whenFalse)
        {
            // Will likely screw things up if the if directive spans any preprocessor directives. So
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

        public static SyntaxRemoveOptions GetRemoveOptions(
            ISyntaxFactsService syntaxFacts, SyntaxNode syntax)
        {
            var removeOptions = SyntaxGenerator.DefaultRemoveOptions;
            if (HasRegularCommentTrivia(syntaxFacts, syntax.GetLeadingTrivia()))
            {
                removeOptions |= SyntaxRemoveOptions.KeepLeadingTrivia;
            }

            if (HasRegularCommentTrivia(syntaxFacts, syntax.GetTrailingTrivia()))
            {
                removeOptions |= SyntaxRemoveOptions.KeepTrailingTrivia;
            }

            return removeOptions;
        }

        private static bool HasRegularComments(ISyntaxFactsService syntaxFacts, SyntaxNode syntax)
            => HasRegularCommentTrivia(syntaxFacts, syntax.GetLeadingTrivia()) ||
               HasRegularCommentTrivia(syntaxFacts, syntax.GetTrailingTrivia());

        private static bool HasRegularCommentTrivia(ISyntaxFactsService syntaxFacts, SyntaxTriviaList triviaList)
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
    }
}
