// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitIsOperator(BoundIsOperator node)
        {
            BoundExpression rewrittenOperand = VisitExpression(node.Operand);
            var rewrittenTargetType = (BoundTypeExpression)VisitTypeExpression(node.TargetType);
            TypeSymbol rewrittenType = VisitType(node.Type);

            return MakeIsOperator(node, node.Syntax, rewrittenOperand, rewrittenTargetType, node.Conversion, rewrittenType);
        }

        private BoundExpression MakeIsOperator(
            BoundIsOperator oldNode,
            SyntaxNode syntax,
            BoundExpression rewrittenOperand,
            BoundTypeExpression rewrittenTargetType,
            Conversion conversion,
            TypeSymbol rewrittenType)
        {
            if (rewrittenOperand.Kind == BoundKind.MethodGroup)
            {
                var methodGroup = (BoundMethodGroup)rewrittenOperand;
                BoundExpression receiver = methodGroup.ReceiverOpt;
                if (receiver != null && receiver.Kind != BoundKind.ThisReference)
                {
                    // possible side-effect
                    return RewriteConstantIsOperator(receiver.Syntax, receiver, ConstantValue.False, rewrittenType);
                }
                else
                {
                    return MakeLiteral(syntax, ConstantValue.False, rewrittenType);
                }
            }

            var operandType = rewrittenOperand.Type;
            var targetType = rewrittenTargetType.Type;

            Debug.Assert((object)operandType != null || rewrittenOperand.ConstantValue.IsNull);
            Debug.Assert((object)targetType != null);

            // TODO: Handle dynamic operand type and target type

            if (!_inExpressionLambda)
            {
                ConstantValue constantValue = Binder.GetIsOperatorConstantResult(operandType, targetType, conversion.Kind, rewrittenOperand.ConstantValue);

                if (constantValue != null)
                {
                    return RewriteConstantIsOperator(syntax, rewrittenOperand, constantValue, rewrittenType);
                }
                else if (conversion.IsImplicit)
                {
                    // operand is a reference type with bound identity or implicit conversion
                    // We can replace the "is" instruction with a null check
                    return MakeNullCheck(syntax, rewrittenOperand, BinaryOperatorKind.NotEqual);
                }
            }

            return oldNode.Update(rewrittenOperand, rewrittenTargetType, conversion, rewrittenType);
        }

        private BoundExpression RewriteConstantIsOperator(
            SyntaxNode syntax,
            BoundExpression loweredOperand,
            ConstantValue constantValue,
            TypeSymbol type)
        {
            Debug.Assert(constantValue == ConstantValue.True || constantValue == ConstantValue.False);
            Debug.Assert((object)type != null);

            return new BoundSequence(
                syntax: syntax,
                locals: ImmutableArray<LocalSymbol>.Empty,
                sideEffects: ImmutableArray.Create<BoundExpression>(loweredOperand),
                value: MakeLiteral(syntax, constantValue, type),
                type: type);
        }
    }
}
