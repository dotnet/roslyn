// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        /// <summary>
        /// If the condition has a constant value, then just use the selected branch.
        /// e.g. "true ? x : y" becomes "x".
        /// </summary>
        public override BoundNode VisitConditionalOperator(BoundConditionalOperator node)
        {
            // just a fact, not a requirement (VisitExpression would have rewritten otherwise)
            Debug.Assert(node.ConstantValue == null);

            var rewrittenCondition = VisitExpression(node.Condition);
            var rewrittenConsequence = VisitExpression(node.Consequence);
            var rewrittenAlternative = VisitExpression(node.Alternative);

            if (rewrittenCondition.ConstantValue == null)
            {
                return node.Update(rewrittenCondition, rewrittenConsequence, rewrittenAlternative, node.ConstantValueOpt, node.Type);
            }

            return RewriteConditionalOperator(
                node.Syntax,
                rewrittenCondition,
                rewrittenConsequence,
                rewrittenAlternative,
                node.ConstantValueOpt,
                node.Type);
        }

        private static BoundExpression RewriteConditionalOperator(
            SyntaxNode syntax,
            BoundExpression rewrittenCondition,
            BoundExpression rewrittenConsequence,
            BoundExpression rewrittenAlternative,
            ConstantValue constantValueOpt,
            TypeSymbol rewrittenType)
        {
            // NOTE: This optimization assumes that a constant has no side effects. In the future we 
            // might wish to represent nodes that are known to the optimizer as having constant
            // values as a sequence of side effects and a constant value; in that case the result
            // of this should be a sequence containing the side effect and the consequence or alternative.

            ConstantValue conditionConstantValue = rewrittenCondition.ConstantValue;
            if (conditionConstantValue == ConstantValue.True)
            {
                return rewrittenConsequence;
            }
            else if (conditionConstantValue == ConstantValue.False)
            {
                return rewrittenAlternative;
            }
            else
            {
                return new BoundConditionalOperator(
                    syntax,
                    rewrittenCondition,
                    rewrittenConsequence,
                    rewrittenAlternative,
                    constantValueOpt,
                    rewrittenType);
            }
        }
    }
}
