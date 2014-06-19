using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Roslyn.Compilers.CSharp
{
    /// <summary>
    /// IsAndAsRewriter optimizes the Is and As expressions by
    /// replacing those is/as expressions which need an identity
    /// or implicit conversion by the corresponding casts,
    /// thus avoiding the need for a runtime "isinst" check
    /// </summary>
    internal sealed class IsAndAsRewriter : BoundTreeRewriter
    {
        private readonly MethodSymbol containingSymbol;
        private readonly Compilation compilation;

        private IsAndAsRewriter(MethodSymbol containingSymbol, Compilation compilation)
        {
            this.compilation = compilation;
            this.containingSymbol = containingSymbol;
        }

        public static BoundStatement Rewrite(BoundStatement node, MethodSymbol containingSymbol, Compilation compilation)
        {
            Debug.Assert(node != null);
            Debug.Assert(compilation != null);

            var rewriter = new IsAndAsRewriter(containingSymbol, compilation);
            var result = (BoundStatement)rewriter.Visit(node);
            return result;
        }

        public override BoundNode VisitIsOperator(BoundIsOperator node)
        {
            // rewrite is needed only for cases where there are no errors and 
            // no warnings (i.e. non-constant result) generated during binding
            if (!node.HasErrors && node.ConstantValue == null)
            {
                BoundExpression operand = node.Operand;
                var targetType = node.TargetType.Type;
                var operandType = operand.Type;

                Debug.Assert(operandType != null);
                if (operandType.IsNullableType())
                {
                    //TODO: handle nullable types once nullable conversions are implemented
                }
                else if (!operandType.IsValueType)
                {
                    if (operandType.IsSameType(targetType))
                    {
                        // operand with bound identity or implicit conversion
                        // We can replace the "is" instruction with a null check
                        Visit(operand);
                        if(operandType.TypeKind == TypeKind.TypeParameter)
                        {
                            // We need to box the type parameter even if it is a known
                            // reference type to ensure there are no verifier errors
                            operand = new BoundConversion(operand.Syntax, operand.SyntaxTree, operand, 
                                ConversionKind.Boxing, this.containingSymbol, false, false, null, 
                                compilation.GetSpecialType(SpecialType.System_Object));
                        }
                        return new BoundBinaryOperator(node.Syntax, node.SyntaxTree, BinaryOperatorKind.NotEqual,
                            operand, new BoundLiteral(null, null, ConstantValue.Null, null), null, node.Type);
                    }
                }
            }
            return base.VisitIsOperator(node);
        }

        public override BoundNode VisitAsOperator(BoundAsOperator node)
        {
            // rewrite is needed only for cases where there are no errors and 
            // no warnings (i.e. non-constant result) generated during binding
            if (!node.HasErrors && node.ConstantValue == null)
            {
                BoundExpression operand = node.Operand;
                var targetType = node.TargetType.Type;
                var operandType = operand.Type;

                // target type cannot be a non-nullable value type
                Debug.Assert(!(targetType.IsValueType && !targetType.IsNullableType()));
                if (operandType != null && operandType.IsSameType(targetType))
                {
                    // operand with bound identity or implicit conversion
                    // we don't need a runtime check
                    return Visit(operand);
                }
            }
            return base.VisitAsOperator(node);
        }
    }
}
