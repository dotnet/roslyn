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
                return node.Update(node.IsRef, rewrittenCondition, rewrittenConsequence, rewrittenAlternative, node.ConstantValueOpt, node.Type);
            }

            return RewriteConditionalOperator(
                node.Syntax,
                rewrittenCondition,
                rewrittenConsequence,
                rewrittenAlternative,
                node.ConstantValueOpt,
                node.Type,
                node.IsRef);
        }

        private static BoundExpression RewriteConditionalOperator(
            SyntaxNode syntax,
            BoundExpression rewrittenCondition,
            BoundExpression rewrittenConsequence,
            BoundExpression rewrittenAlternative,
            ConstantValue constantValueOpt,
            TypeSymbol rewrittenType,
            bool isRef)
        {
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
                    isRef,
                    rewrittenCondition,
                    rewrittenConsequence,
                    rewrittenAlternative,
                    constantValueOpt,
                    rewrittenType);
            }
        }
    }
}
