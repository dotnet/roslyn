// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitPointerElementAccess(BoundPointerElementAccess node)
        {
            BoundExpression rewrittenExpression = LowerReceiverOfPointerElementAccess(node.Expression);
            BoundExpression rewrittenIndex = VisitExpression(node.Index);

            return RewritePointerElementAccess(node, rewrittenExpression, rewrittenIndex);
        }

        private BoundExpression LowerReceiverOfPointerElementAccess(BoundExpression receiver)
        {
            if (receiver is BoundFieldAccess fieldAccess && fieldAccess.FieldSymbol.IsFixedSizeBuffer)
            {
                var loweredFieldReceiver = VisitExpression(fieldAccess.ReceiverOpt);
                fieldAccess = fieldAccess.Update(loweredFieldReceiver, fieldAccess.FieldSymbol, fieldAccess.ConstantValueOpt, fieldAccess.ResultKind, fieldAccess.Type);
                return new BoundAddressOfOperator(receiver.Syntax, fieldAccess, isManaged: true, fieldAccess.Type);
            }

            return VisitExpression(receiver);
        }

        private BoundExpression RewritePointerElementAccess(BoundPointerElementAccess node, BoundExpression rewrittenExpression, BoundExpression rewrittenIndex)
        {
            // Optimization: p[0] == *p
            if (rewrittenIndex.IsDefaultValue())
            {
                return new BoundPointerIndirectionOperator(
                    node.Syntax,
                    rewrittenExpression,
                    node.RefersToLocation,
                    node.Type);
            }

            BinaryOperatorKind additionKind = BinaryOperatorKind.Addition;

            Debug.Assert(rewrittenExpression.Type is { });
            Debug.Assert(rewrittenIndex.Type is { });
            switch (rewrittenIndex.Type.SpecialType)
            {
                case SpecialType.System_Int32:
                    additionKind |= BinaryOperatorKind.PointerAndIntAddition;
                    break;
                case SpecialType.System_UInt32:
                    additionKind |= BinaryOperatorKind.PointerAndUIntAddition;
                    break;
                case SpecialType.System_Int64:
                    additionKind |= BinaryOperatorKind.PointerAndLongAddition;
                    break;
                case SpecialType.System_UInt64:
                    additionKind |= BinaryOperatorKind.PointerAndULongAddition;
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(rewrittenIndex.Type.SpecialType);
            }

            if (node.Checked)
            {
                additionKind |= BinaryOperatorKind.Checked;
            }

            return new BoundPointerIndirectionOperator(
                node.Syntax,
                MakeBinaryOperator(
                    node.Syntax,
                    additionKind,
                    rewrittenExpression,
                    rewrittenIndex,
                    rewrittenExpression.Type,
                    method: null,
                    constrainedToTypeOpt: null,
                    isPointerElementAccess: true), //see RewriterPointerNumericOperator
                node.RefersToLocation,
                node.Type);
        }
    }
}
