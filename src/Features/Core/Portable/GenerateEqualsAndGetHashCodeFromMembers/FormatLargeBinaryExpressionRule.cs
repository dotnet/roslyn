// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.GenerateEqualsAndGetHashCodeFromMembers
{
    internal partial class AbstractGenerateEqualsAndGetHashCodeService
    {
        /// <summary>
        /// Specialized formatter for the "return a == obj.a &amp;&amp; b == obj.b &amp;&amp; c == obj.c &amp;&amp; ...
        /// code that we spit out.
        /// </summary>
        private class FormatLargeBinaryExpressionRule : AbstractFormattingRule
        {
            private readonly ISyntaxFactsService _syntaxFacts;

            public FormatLargeBinaryExpressionRule(ISyntaxFactsService syntaxFacts)
            {
                _syntaxFacts = syntaxFacts;
            }

            /// <summary>
            /// Wrap the large &amp;&amp; expression after every &amp;&amp; token.
            /// </summary>
            public override AdjustNewLinesOperation GetAdjustNewLinesOperation(
                SyntaxToken previousToken, SyntaxToken currentToken, OptionSet optionSet, in NextGetAdjustNewLinesOperation nextOperation)
            {
                if (_syntaxFacts.IsLogicalAndExpression(previousToken.Parent))
                {
                    return FormattingOperations.CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                }

                return nextOperation.Invoke();
            }

            /// <summary>
            /// Align all the wrapped parts of the expression with the token after 'return'.
            /// That way we get:
            /// 
            /// return a == obj.a &amp;&amp;
            ///        b == obj.b &amp;&amp;
            ///        ...
            /// </summary>
            public override void AddIndentBlockOperations(
                List<IndentBlockOperation> list, SyntaxNode node, OptionSet optionSet, in NextIndentBlockOperationAction nextOperation)
            {
                if (_syntaxFacts.IsReturnStatement(node))
                {
                    var expr = _syntaxFacts.GetExpressionOfReturnStatement(node);
                    if (expr?.ChildNodesAndTokens().Count > 1)
                    {
                        list.Add(FormattingOperations.CreateRelativeIndentBlockOperation(
                            expr.GetFirstToken(),
                            expr.GetFirstToken().GetNextToken(),
                            node.GetLastToken(),
                            indentationDelta: 0,
                            option: IndentBlockOption.RelativePosition));

                        return;
                    }
                }

                nextOperation.Invoke();
            }
        }
    }
}
