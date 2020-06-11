// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitAsOperator(BoundAsOperator node)
        {
            BoundExpression rewrittenOperand = VisitExpression(node.Operand);
            var rewrittenTargetType = (BoundTypeExpression)VisitTypeExpression(node.TargetType);
            TypeSymbol rewrittenType = VisitType(node.Type);

            return MakeAsOperator(node, node.Syntax, rewrittenOperand, rewrittenTargetType, node.Conversion, rewrittenType);
        }

        public override BoundNode VisitTypeExpression(BoundTypeExpression node)
        {
            var result = base.VisitTypeExpression(node);
            Debug.Assert(result is { });
            return result;
        }

        private BoundExpression MakeAsOperator(
            BoundAsOperator oldNode,
            SyntaxNode syntax,
            BoundExpression rewrittenOperand,
            BoundTypeExpression rewrittenTargetType,
            Conversion conversion,
            TypeSymbol rewrittenType)
        {
            // TODO: Handle dynamic operand type and target type
            Debug.Assert(rewrittenTargetType.Type.Equals(rewrittenType));

            // target type cannot be a non-nullable value type
            Debug.Assert(!rewrittenType.IsValueType || rewrittenType.IsNullableType());

            if (!_inExpressionLambda)
            {
                ConstantValue constantValue = Binder.GetAsOperatorConstantResult(rewrittenOperand.Type, rewrittenType, conversion.Kind, rewrittenOperand.ConstantValue);

                if (constantValue != null)
                {
                    Debug.Assert(constantValue.IsNull);
                    BoundExpression result = rewrittenType.IsNullableType() ? new BoundDefaultExpression(syntax, rewrittenType) : MakeLiteral(syntax, constantValue, rewrittenType);

                    if (rewrittenOperand.ConstantValue != null)
                    {
                        // No need to preserve any side-effects from the operand. 
                        // We also can keep the "constant" notion of the result, which
                        // enables some optimizations down the road.
                        return result;
                    }

                    return new BoundSequence(
                        syntax: syntax,
                        locals: ImmutableArray<LocalSymbol>.Empty,
                        sideEffects: ImmutableArray.Create<BoundExpression>(rewrittenOperand),
                        value: result,
                        type: rewrittenType);
                }

                if (conversion.IsImplicit)
                {
                    // Operand with bound implicit conversion to target type.
                    // We don't need a runtime check, generate a conversion for the operand instead.
                    return MakeConversionNode(syntax, rewrittenOperand, conversion, rewrittenType, @checked: false);
                }
            }

            return oldNode.Update(rewrittenOperand, rewrittenTargetType, conversion, rewrittenType);
        }
    }
}
