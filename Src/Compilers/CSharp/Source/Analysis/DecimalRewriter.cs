using System;
using System.Diagnostics;

namespace Roslyn.Compilers.CSharp
{
    // The decimal type is "built in" to C# but is just another struct type in the CLR; the code
    // generator does not know how to deal with it.  The decimal rewriter lowers operations on
    // decimals.

    internal sealed class DecimalRewriter : BoundTreeRewriter
    {
        private readonly Compilation compilation;

        private DecimalRewriter(Compilation compilation)
        {
            this.compilation = compilation;
        }

        public static BoundStatement Rewrite(BoundStatement node, Compilation compilation)
        {
            Debug.Assert(node != null);
            DecimalRewriter instance = new DecimalRewriter(compilation);
            return (BoundStatement)instance.Visit(node);
        }

        public override BoundNode Visit(BoundNode node)
        {
            var expression = node as BoundExpression;
            if (expression != null)
            {
                var constantValue = expression.ConstantValue;
                if (constantValue != null && constantValue.IsDecimal)
                {
                    return RewriteConstant(expression);
                }
            }
            return base.Visit(node);
        }

        public override BoundNode VisitConversion(BoundConversion node)
        {
            Debug.Assert(node != null);

            BoundExpression operand = (BoundExpression)this.Visit(node.Operand);

            switch (node.ConversionKind)
            {
                case ConversionKind.ImplicitNumeric:
                case ConversionKind.ExplicitNumeric:
                    if (node.Type.SpecialType == SpecialType.System_Decimal)
                    {
                        return RewriteNumericToDecimalConversion(node.Syntax, operand, operand.Type);
                    }
                    else if (operand.Type.SpecialType == SpecialType.System_Decimal)
                    {
                        return RewriteDecimalToNumericConversion(node.Syntax, operand, node.Type);
                    }
                    break;
                default:
                    break;
            }

            return node.Update(operand, node.ConversionKind, node.SymbolOpt, node.Checked, node.ExplicitCastInCode, node.IsExtensionMethod, node.ConstantValueOpt, node.ResultKind, node.Type);
        }

        public override BoundNode VisitBinaryOperator(BoundBinaryOperator node)
        {
            Debug.Assert(node != null);

            BoundExpression left = (BoundExpression)this.Visit(node.Left);
            BoundExpression right = (BoundExpression)this.Visit(node.Right);

            if (node.Type.SpecialType == SpecialType.System_Decimal)
            {
                return RewriteDecimalArithmeticBinaryOperator(node.OperatorKind, node.Syntax, left, right);
            }
            else if (node.Left.Type.SpecialType == SpecialType.System_Decimal && node.Left.Type.SpecialType == SpecialType.System_Decimal)
            {
                return RewriteDecimalComparisonOperator(node.OperatorKind, node.Syntax, left, right);
            }

            return node.Update(node.OperatorKind, left, right, node.ConstantValueOpt, node.MethodOpt, node.ResultKind, node.Type);
        }

        private static BoundNode RewriteConstant(BoundExpression node)
        {
            var syntax = node.Syntax;

            Debug.Assert(node != null);
            var constantValue = node.ConstantValue;
            Debug.Assert(constantValue != null);
            Debug.Assert(node.ConstantValue.IsDecimal);

            var decimalType = node.Type as NamedTypeSymbol;

            Debug.Assert(decimalType != null);
            Debug.Assert(decimalType.SpecialType == SpecialType.System_Decimal);

            var value = node.ConstantValue.DecimalValue;
            var parts = new DecimalParts(value);
            var scale = parts.Scale;

            var arguments = new ArrayBuilder<BoundExpression>();
            MethodSymbol ctor = null;
            var ctors = decimalType.InstanceConstructors;

            // check if we can call a simple constructor
            if (scale == 0 && !parts.IsNegative && value == 0m)
            {
                // new decimal();
                foreach (MethodSymbol c in ctors)
                {
                    if (c.Parameters.Count == 0)
                    {
                        ctor = c;
                        break;
                    }
                }
            }
            else if (scale == 0 && int.MinValue <= value && value <= int.MaxValue)
            {
                //new decimal(int);
                foreach (MethodSymbol c in ctors)
                {
                    if (c.Parameters.Count == 1 && c.Parameters[0].Type.SpecialType == SpecialType.System_Int32)
                    {
                        ctor = c;
                        break;
                    }
                }
                arguments.Add(new BoundLiteral(syntax, ConstantValue.Create((int)value), ctor.Parameters[0].Type));
            }
            else if (scale == 0 && uint.MinValue <= value && value <= uint.MaxValue)
            {
                //new decimal(uint);
                foreach (MethodSymbol c in ctors)
                {
                    if (c.Parameters.Count == 1 && c.Parameters[0].Type.SpecialType == SpecialType.System_UInt32)
                    {
                        ctor = c;
                        break;
                    }
                }
                arguments.Add(new BoundLiteral(syntax, ConstantValue.Create((uint)value), ctor.Parameters[0].Type));
            }
            else if (scale == 0 && long.MinValue <= value && value <= long.MaxValue)
            {
                //new decimal(long);
                foreach (MethodSymbol c in ctors)
                {
                    if (c.Parameters.Count == 1 && c.Parameters[0].Type.SpecialType == SpecialType.System_Int64)
                    {
                        ctor = c;
                        break;
                    }
                }
                arguments.Add(new BoundLiteral(syntax, ConstantValue.Create((long)value), ctor.Parameters[0].Type));
            }
            else if (scale == 0 && ulong.MinValue <= value && value <= ulong.MaxValue)
            {
                //new decimal(ulong);
                foreach (MethodSymbol c in ctors)
                {
                    if (c.Parameters.Count == 1 && c.Parameters[0].Type.SpecialType == SpecialType.System_UInt64)
                    {
                        ctor = c;
                        break;
                    }
                }
                arguments.Add(new BoundLiteral(syntax, ConstantValue.Create((ulong)value), ctor.Parameters[0].Type));
            }
            else
            {
                //new decimal(int low, int mid, int high, bool isNegative, byte scale);
                foreach (MethodSymbol c in ctors)
                {
                    if (c.Parameters.Count == 5)
                    {
                        ctor = c;
                        break;
                    }
                }
                arguments.Add(new BoundLiteral(syntax, ConstantValue.Create(parts.Low), ctor.Parameters[0].Type));
                arguments.Add(new BoundLiteral(syntax, ConstantValue.Create(parts.Mid), ctor.Parameters[1].Type));
                arguments.Add(new BoundLiteral(syntax, ConstantValue.Create(parts.High), ctor.Parameters[2].Type));
                arguments.Add(new BoundLiteral(syntax, ConstantValue.Create(parts.IsNegative), ctor.Parameters[3].Type));
                arguments.Add(new BoundLiteral(syntax, ConstantValue.Create((byte)parts.Scale), ctor.Parameters[4].Type));
            }
            return new BoundObjectCreationExpression(
                node.Syntax,
                ctor,
                arguments.ToReadOnlyAndFree());
        }

        private struct DecimalParts
        {
            public DecimalParts(decimal value)
                : this()
            {
                int[] bits = Decimal.GetBits(value);
                Low = bits[0];
                Mid = bits[1];
                High = bits[2];
                Scale = (bits[3] & 0x7FFFFFF) >> 16;
                IsNegative = ((bits[3] & 0x80000000) != 0);
            }

            public int Low { get; private set; }
            public int Mid { get; private set; }
            public int High { get; private set; }
            public int Scale { get; private set; }
            public bool IsNegative { get; private set; }
        }

        private BoundNode RewriteDecimalToNumericConversion(SyntaxNode syntaxNode, BoundExpression operand, TypeSymbol typeTo)
        {
            SpecialMember member;

            switch (typeTo.SpecialType)
            {
                case SpecialType.System_Char: member = SpecialMember.System_Decimal__op_Explicit_ToChar; break;
                case SpecialType.System_SByte: member = SpecialMember.System_Decimal__op_Explicit_ToSByte; break;
                case SpecialType.System_Byte: member = SpecialMember.System_Decimal__op_Explicit_ToByte; break;
                case SpecialType.System_Int16: member = SpecialMember.System_Decimal__op_Explicit_ToInt16; break;
                case SpecialType.System_UInt16: member = SpecialMember.System_Decimal__op_Explicit_ToUInt16; break;
                case SpecialType.System_Int32: member = SpecialMember.System_Decimal__op_Explicit_ToInt32; break;
                case SpecialType.System_UInt32: member = SpecialMember.System_Decimal__op_Explicit_ToUInt32; break;
                case SpecialType.System_Int64: member = SpecialMember.System_Decimal__op_Explicit_ToInt64; break;
                case SpecialType.System_UInt64: member = SpecialMember.System_Decimal__op_Explicit_ToUInt64; break;
                case SpecialType.System_Single: member = SpecialMember.System_Decimal__op_Explicit_ToSingle; break;
                case SpecialType.System_Double: member = SpecialMember.System_Decimal__op_Explicit_ToDouble; break;
                default:
                    Debug.Assert(false); // Cannot reach here
                    return null;
            }

            // call the method
            var method = (MethodSymbol)this.compilation.Assembly.GetSpecialTypeMember(member);
            Debug.Assert(method != null); // Should have been checked during Warnings pass

            return BoundCall.Synthesized(
                syntaxNode,
                null,
                method,
                operand);
        }

        private BoundNode RewriteNumericToDecimalConversion(SyntaxNode syntaxNode, BoundExpression operand, TypeSymbol typeFrom)
        {
            SpecialMember member;

            switch (typeFrom.SpecialType)
            {
                case SpecialType.System_Char: member = SpecialMember.System_Decimal__op_Implicit_FromChar; break;
                case SpecialType.System_SByte: member = SpecialMember.System_Decimal__op_Implicit_FromSByte; break;
                case SpecialType.System_Byte: member = SpecialMember.System_Decimal__op_Implicit_FromByte; break;
                case SpecialType.System_Int16: member = SpecialMember.System_Decimal__op_Implicit_FromInt16; break;
                case SpecialType.System_UInt16: member = SpecialMember.System_Decimal__op_Implicit_FromUInt16; break;
                case SpecialType.System_Int32: member = SpecialMember.System_Decimal__op_Implicit_FromInt32; break;
                case SpecialType.System_UInt32: member = SpecialMember.System_Decimal__op_Implicit_FromUInt32; break;
                case SpecialType.System_Int64: member = SpecialMember.System_Decimal__op_Implicit_FromInt64; break;
                case SpecialType.System_UInt64: member = SpecialMember.System_Decimal__op_Implicit_FromUInt64; break;
                case SpecialType.System_Single: member = SpecialMember.System_Decimal__op_Explicit_FromSingle; break;
                case SpecialType.System_Double: member = SpecialMember.System_Decimal__op_Explicit_FromDouble; break;
                default:
                    Debug.Assert(false); // Cannot reach here
                    return null;
            }

            // call the method
            var method = (MethodSymbol)this.compilation.Assembly.GetSpecialTypeMember(member);
            Debug.Assert(method != null); // Should have been checked during Warnings pass

            return BoundCall.Synthesized(
                syntaxNode,
                null,
                method,
                operand);
        }

        private BoundNode RewriteDecimalArithmeticBinaryOperator(BinaryOperatorKind oper, SyntaxNode syntaxNode, BoundExpression left, BoundExpression right)
        {
            Debug.Assert(left.Type.SpecialType == SpecialType.System_Decimal);
            Debug.Assert(right.Type.SpecialType == SpecialType.System_Decimal);

            SpecialMember member;

            switch (oper)
            {
                case BinaryOperatorKind.DecimalAddition: member = SpecialMember.System_Decimal__AddDecimalDecimal; break;
                case BinaryOperatorKind.DecimalSubtraction: member = SpecialMember.System_Decimal__SubtractDecimalDecimal; break;
                case BinaryOperatorKind.DecimalMultiplication: member = SpecialMember.System_Decimal__MultiplyDecimalDecimal; break;
                case BinaryOperatorKind.DecimalDivision: member = SpecialMember.System_Decimal__DivideDecimalDecimal; break;
                case BinaryOperatorKind.DecimalRemainder: member = SpecialMember.System_Decimal__RemainderDecimalDecimal; break;
                default:
                    Debug.Assert(false); // Cannot reach here
                    return null;
            }

            // call Decimal.Operator (left, right)
            return BuildBinaryOperatorCall(syntaxNode, left, right, member);
        }

        private BoundNode RewriteDecimalComparisonOperator(BinaryOperatorKind operatorKind, SyntaxNode syntaxNode, BoundExpression left, BoundExpression right)
        {
            Debug.Assert(left.Type.SpecialType == SpecialType.System_Decimal);
            Debug.Assert(right.Type.SpecialType == SpecialType.System_Decimal);

            SpecialMember member;

            switch (operatorKind)
            {
                case BinaryOperatorKind.DecimalEqual: member = SpecialMember.System_Decimal__op_Equality; break;
                case BinaryOperatorKind.DecimalNotEqual: member = SpecialMember.System_Decimal__op_Inequality; break;
                case BinaryOperatorKind.DecimalLessThan: member = SpecialMember.System_Decimal__op_LessThan; break;
                case BinaryOperatorKind.DecimalLessThanOrEqual: member = SpecialMember.System_Decimal__op_LessThanOrEqual; break;
                case BinaryOperatorKind.DecimalGreaterThan: member = SpecialMember.System_Decimal__op_GreaterThan; break;
                case BinaryOperatorKind.DecimalGreaterThanOrEqual: member = SpecialMember.System_Decimal__op_GreaterThanOrEqual; break;
                default:
                    Debug.Assert(false); // Cannot reach here
                    return null;
            }

            // call Decimal.Operator (left, right)
            return BuildBinaryOperatorCall(syntaxNode, left, right, member);
        }

        private BoundNode BuildBinaryOperatorCall(SyntaxNode syntaxNode, BoundExpression left, BoundExpression right, SpecialMember member)
        {
            // call Operator (left, right)
            var memberSymbol = (MethodSymbol)this.compilation.Assembly.GetSpecialTypeMember(member);
            Debug.Assert(memberSymbol != null); // Should have been checked during Warnings pass

            return BoundCall.Synthesized(
                syntaxNode,
                null,
                memberSymbol,
                ReadOnlyArray<BoundExpression>.CreateFrom(new BoundExpression[] { left, right }));
        }
    }
}