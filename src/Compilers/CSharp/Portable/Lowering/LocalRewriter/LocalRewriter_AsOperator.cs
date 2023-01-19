// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitAsOperator(BoundAsOperator node)
        {
            BoundExpression rewrittenOperand = VisitExpression(node.Operand);
            var rewrittenTargetType = (BoundTypeExpression)VisitTypeExpression(node.TargetType);
            TypeSymbol rewrittenType = VisitType(node.Type);

            return MakeAsOperator(node, node.Syntax, rewrittenOperand, rewrittenTargetType, node.OperandPlaceholder, node.OperandConversion, rewrittenType);
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
            BoundValuePlaceholder? operandPlaceholder,
            BoundExpression? operandConversion,
            TypeSymbol rewrittenType)
        {
            // TODO: Handle dynamic operand type and target type
            Debug.Assert(rewrittenTargetType.Type.Equals(rewrittenType));

            // target type cannot be a non-nullable value type
            Debug.Assert(!rewrittenType.IsValueType || rewrittenType.IsNullableType());

            if (!_inExpressionLambda)
            {
                var conversion = BoundNode.GetConversion(operandConversion, operandPlaceholder);

                ConstantValue constantValue = Binder.GetAsOperatorConstantResult(rewrittenOperand.Type, rewrittenType, conversion.Kind, rewrittenOperand.ConstantValueOpt);

                if (constantValue != null)
                {
                    if (constantValue.IsBad)
                    {
                        throw ExceptionUtilities.UnexpectedValue(constantValue);
                    }

                    Debug.Assert(constantValue.IsNull);
                    BoundExpression result = rewrittenType.IsNullableType() ? new BoundDefaultExpression(syntax, rewrittenType) : MakeLiteral(syntax, constantValue, rewrittenType);

                    if (rewrittenOperand.ConstantValueOpt != null)
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
                    Debug.Assert(operandPlaceholder is not null);
                    Debug.Assert(operandConversion is not null);

                    AddPlaceholderReplacement(operandPlaceholder, rewrittenOperand);
                    BoundExpression result = VisitExpression(operandConversion);
                    Debug.Assert(result.Type!.Equals(rewrittenType, TypeCompareKind.ConsiderEverything));
                    RemovePlaceholderReplacement(operandPlaceholder);

                    return result;
                }
            }

            return oldNode.Update(rewrittenOperand, rewrittenTargetType, operandPlaceholder: null, operandConversion: null, rewrittenType);
        }
    }
}
