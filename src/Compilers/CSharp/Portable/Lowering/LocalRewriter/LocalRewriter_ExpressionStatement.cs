// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            var syntax = node.Syntax;
            var loweredExpression = VisitUnusedExpression(node.Expression);

            if (loweredExpression == null)
            {
                // NOTE: not using a BoundNoOpStatement, since we don't want a nop to be emitted.
                // CONSIDER: could use a BoundNoOpStatement (DevDiv #12943).
                return BoundStatementList.Synthesized(syntax);
            }
            else
            {
                BoundStatement result = node.Update(loweredExpression);
                if (this.Instrument && !node.WasCompilerGenerated)
                {
                    result = _instrumenter.InstrumentExpressionStatement(node, result);
                }

                return result;
            }
        }

        private BoundExpression VisitUnusedExpression(BoundExpression expression)
        {
            if (expression.HasErrors)
            {
                return expression;
            }

            switch (expression.Kind)
            {
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
