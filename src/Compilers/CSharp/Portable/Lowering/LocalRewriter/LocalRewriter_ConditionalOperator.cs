// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        /// <summary>
        /// If the conditional operator matches to one of the following patterns replaces it with more optimal expression.
        /// "true ? x : y" becomes "x"
        /// "false" ? x : y" becomes "y"
        /// "condition ? true : false" becomes "condition"
        /// "condition ? false: true" becomes "!condition"
        /// </summary>
        public override BoundNode VisitConditionalOperator(BoundConditionalOperator node)
        {
            // just a fact, not a requirement (VisitExpression would have rewritten otherwise)
            Debug.Assert(node.ConstantValue == null);

            var rewrittenCondition = VisitExpression(node.Condition);
            var rewrittenConsequence = VisitExpression(node.Consequence);
            var rewrittenAlternative = VisitExpression(node.Alternative);

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
            BoundExpression result;

            if (TryRewriteConstantConditionalOperator(rewrittenCondition, rewrittenConsequence, rewrittenAlternative, out result))
            {
                return result;
            }

            if (TryRewriteBoolConditionalOperatorBranches(syntax, rewrittenCondition, rewrittenConsequence, rewrittenAlternative, rewrittenType, out result))
            {
                return result;
            }

            return new BoundConditionalOperator(
                syntax,
                isRef,
                rewrittenCondition,
                rewrittenConsequence,
                rewrittenAlternative,
                constantValueOpt,
                rewrittenType);
        }

        /// <summary>
        /// If the condition has a constant value, then just use the selected branch.
        /// e.g. "true ? x : y" becomes "x".
        /// </summary>
        private static bool TryRewriteConstantConditionalOperator(
            BoundExpression condition,
            BoundExpression consequence,
            BoundExpression alternative,
            out BoundExpression result)
        {
            ConstantValue conditionConstantValue = condition.ConstantValue;
            if (conditionConstantValue == ConstantValue.True)
            {
                result = consequence;
                return true;
            }

            if (conditionConstantValue == ConstantValue.False)
            {
                result = alternative;
                return true;
            }

            result = default;
            return false;
        }

        /// <summary>
        /// If the conditional operator matches the pattern "condition ? true : false" or "condition ? false : true"
        /// replaces it with "condition" or "!condition" respectively.
        /// </summary>
        private static bool TryRewriteBoolConditionalOperatorBranches(
            SyntaxNode syntax,
            BoundExpression condition,
            BoundExpression consequence,
            BoundExpression alternative,
            TypeSymbol type,
            out BoundExpression result)
        {
            var consequenceConstantValue = consequence.ConstantValue;
            var alternativeConstantValue = alternative.ConstantValue;

            if (consequenceConstantValue == ConstantValue.True && alternativeConstantValue == ConstantValue.False)
            {
                result = condition;
                return true;
            }

            if (consequenceConstantValue == ConstantValue.False && alternativeConstantValue == ConstantValue.True)
            {
                result = new BoundUnaryOperator(
                    syntax,
                    UnaryOperatorKind.BoolLogicalNegation,
                    condition,
                    ConstantValue.NotAvailable,
                    methodOpt: null,
                    LookupResultKind.Empty,
                    type);
                return true;
            }

            result = default;
            return false;
        }
    }
}