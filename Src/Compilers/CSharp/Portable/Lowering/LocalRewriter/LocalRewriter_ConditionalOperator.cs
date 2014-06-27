// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            CSharpSyntaxNode syntax,
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

        // null when currently enclosing conditional access node
        // is not supposed to be lowered.
        private BoundExpression currentConditionalAccessTarget = null;

        // in simple cases could be left unlowered.
        // IL gen can generate more compact code for unlowered conditional accesses 
        // by utilizing stack dup/pop instructions 
        public override BoundNode VisitConditionalAccess(BoundConditionalAccess node)
        {
            BoundExpression loweredReceiver = (BoundExpression)this.Visit(node.Receiver);

            var receiverType = loweredReceiver.Type;

            //TODO: if AccessExpression does not contain awaits, the node could be left unlowered (saves a temp),
            //      but there seem to be no way of knowing that without walking AccessExpression.
            var needToLower = receiverType.IsNullableType() || 
                this.inExpressionLambda || 
                this.factory.CurrentMethod.IsAsync || 
                node.Type.IsDynamic();

            var previousConditionalAccesTarget = currentConditionalAccessTarget;
            LocalSymbol temp = null;

            if (needToLower)
            {
                if (NeedsTemp(loweredReceiver, localsMayBeAssigned: false))
                {
                    temp = factory.SynthesizedLocal(receiverType);
                    currentConditionalAccessTarget = factory.Local(temp);
                    loweredReceiver = factory.AssignmentExpression(factory.Local(temp), loweredReceiver);
                }
                else
                {
                    currentConditionalAccessTarget = loweredReceiver;
                }
            }
            else
            {
                currentConditionalAccessTarget = null;
            }

            BoundExpression loweredAccessExpression = (BoundExpression)this.Visit(node.AccessExpression);

            currentConditionalAccessTarget = previousConditionalAccesTarget;

            TypeSymbol type = this.VisitType(node.Type);

            TypeSymbol nodeType = node.Type;
            TypeSymbol accessExpressionType = loweredAccessExpression.Type;

            if (accessExpressionType != nodeType)
            {
                Debug.Assert(nodeType.IsNullableType() && accessExpressionType == nodeType.GetNullableUnderlyingType());
                loweredAccessExpression = factory.New((NamedTypeSymbol)nodeType, loweredAccessExpression);
            }

            BoundExpression result;
            if (!needToLower)
            {
                Debug.Assert(receiverType.IsReferenceType);
                result = node.Update(loweredReceiver, loweredAccessExpression, type);
            }
            else
            {
                var condition = receiverType.IsReferenceType ?
                    factory.ObjectNotEqual(loweredReceiver, factory.Null(receiverType)) :
                    MakeOptimizedHasValue(loweredReceiver.Syntax, loweredReceiver);

                var consequence = loweredAccessExpression;
                var alternative = factory.Default(nodeType);

                result = RewriteConditionalOperator(node.Syntax,
                    condition,
                    consequence,
                    alternative,
                    null,
                    alternative.Type);

                if (temp!=null)
                {
                    result = factory.Sequence(temp, result);
                }
            }

            return result;
        }

        public override BoundNode VisitConditionalReceiver(BoundConditionalReceiver node)
        {
            if (currentConditionalAccessTarget == null)
            {
                return node;
            }

            var newtarget = currentConditionalAccessTarget;
            if (newtarget.Type.IsNullableType())
            {
                newtarget = MakeOptimizedGetValueOrDefault(node.Syntax, newtarget);
            }

            return newtarget;
        }
    }
}
