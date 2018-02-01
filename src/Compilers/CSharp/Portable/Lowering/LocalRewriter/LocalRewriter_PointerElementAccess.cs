// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitPointerElementAccess(BoundPointerElementAccess node)
        {
            BoundExpression rewrittenExpression = VisitExpression(node.Expression);
            BoundExpression rewrittenIndex = VisitExpression(node.Index);

            return RewritePointerElementAccess(node, rewrittenExpression, rewrittenIndex);
        }

        private BoundExpression RewritePointerElementAccess(BoundPointerElementAccess node, BoundExpression rewrittenExpression, BoundExpression rewrittenIndex)
        {
            // Optimization: p[0] == *p
            if (rewrittenIndex.IsDefaultValue())
            {
                return new BoundPointerIndirectionOperator(
                    node.Syntax,
                    rewrittenExpression,
                    node.Type);
            }

            BinaryOperatorKind additionKind = BinaryOperatorKind.Addition;

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
                    isPointerElementAccess: true), //see RewriterPointerNumericOperator
                node.Type);
        }
    }
}
