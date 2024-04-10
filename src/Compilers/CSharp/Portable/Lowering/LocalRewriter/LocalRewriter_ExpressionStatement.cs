// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitExpressionStatement(BoundExpressionStatement node)
        {
            // NOTE: not using a BoundNoOpStatement, since we don't want a nop to be emitted.
            // CONSIDER: could use a BoundNoOpStatement (DevDiv #12943).
            return RewriteExpressionStatement(node) ?? BoundStatementList.Synthesized(node.Syntax);
        }

        private BoundStatement? RewriteExpressionStatement(BoundExpressionStatement node, bool suppressInstrumentation = false)
        {
            var loweredExpression = VisitUnusedExpression(node.Expression);

            if (loweredExpression == null)
            {
                return null;
            }
            else
            {
                BoundStatement result = node.Update(loweredExpression);
                if (!suppressInstrumentation && this.Instrument && !node.WasCompilerGenerated)
                {
                    result = Instrumenter.InstrumentExpressionStatement(node, result);
                }

                return result;
            }
        }

        private BoundExpression? VisitUnusedExpression(BoundExpression expression)
        {
            if (expression.HasErrors)
            {
                return expression;
            }

            switch (expression.Kind)
            {
                case BoundKind.AwaitExpression:
                    return VisitAwaitExpression((BoundAwaitExpression)expression, used: false);

                case BoundKind.AssignmentOperator:
                    // Avoid extra temporary by indicating the expression value is not used.
                    return VisitAssignmentOperator((BoundAssignmentOperator)expression, used: false);

                case BoundKind.CompoundAssignmentOperator:
                    return VisitCompoundAssignmentOperator((BoundCompoundAssignmentOperator)expression, used: false);

                case BoundKind.Call:
                    if (_allowOmissionOfConditionalCalls)
                    {
                        var call = (BoundCall)expression;
                        if (call.Method.CallsAreOmitted(call.SyntaxTree))
                        {
                            return null;
                        }
                    }
                    break;

                case BoundKind.DynamicInvocation:
                    // TODO (tomat): circumvents logic in VisitExpression...
                    return VisitDynamicInvocation((BoundDynamicInvocation)expression, resultDiscarded: true);

                case BoundKind.ConditionalAccess:
                    return RewriteConditionalAccess((BoundConditionalAccess)expression, used: false);
            }
            return VisitExpression(expression);
        }
    }
}
