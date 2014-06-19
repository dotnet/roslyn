using System;
using System.Diagnostics;

namespace Roslyn.Compilers.CSharp
{
    /// <summary>
    /// This rewriter lowers pre-/post- increment/decrement operations (initially represented as
    /// unary operators). We use BoundSequenceExpressions because we need to capture the RHS of the
    /// assignment in a temp variable.
    /// </summary>
    /// <remarks>
    /// This rewriter assumes that it will be run before decimal rewriting (so that it does not have
    /// to lower decimal constants and operations) and call rewriting (so that it does not have to
    /// lower property accesses).
    /// </remarks>
    internal sealed class IncrementRewriter : BoundTreeRewriter
    {
        private MethodSymbol containingSymbol;
        private readonly Compilation compilation;

        private IncrementRewriter(MethodSymbol containingSymbol, Compilation compilation)
        {
            this.containingSymbol = containingSymbol;
            this.compilation = compilation;
        }

        /// <summary>
        /// The entrypoint for this rewriter.
        /// </summary>
        /// <param name="node">The root of the bound subtree to be rewritten.</param>
        /// <param name="containingSymbol">The symbol whose declaration encloses the node.</param>
        /// <param name="compilation">For looking up TypeSymbols from SpecialTypes.</param>
        /// <returns>The rewritten bound subtree, which contains no increment or decrement operators.</returns>
        public static BoundStatement Rewrite(BoundStatement node, MethodSymbol containingSymbol, Compilation compilation)
        {
            Debug.Assert(node != null);
            var rewriter = new IncrementRewriter(containingSymbol, compilation);
            return (BoundStatement)rewriter.Visit(node);
        }

        public override BoundNode VisitLambda(BoundLambda node)
        {
            var oldContainingSymbol = this.containingSymbol;
            try
            {
                this.containingSymbol = node.Symbol;
                return base.VisitLambda(node);
            }
            finally
            {
                this.containingSymbol = oldContainingSymbol;
            }
        }

        /// <summary>
        /// Rewrite bound unary operators representing increments or decrements.  Leave other nodes as-is.
        /// </summary>
        public override BoundNode VisitUnaryOperator(BoundUnaryOperator node)
        {
            Debug.Assert(node != null);

            if (node.HasErrors)
            {
                return node;
            }

            switch (node.OperatorKind.Operator())
            {
                case UnaryOperatorKind.PrefixIncrement:
                    return LowerOperator(node, isPrefix: true, isIncrement: true);
                case UnaryOperatorKind.PrefixDecrement:
                    return LowerOperator(node, isPrefix: true, isIncrement: false);
                case UnaryOperatorKind.PostfixIncrement:
                    return LowerOperator(node, isPrefix: false, isIncrement: true);
                case UnaryOperatorKind.PostfixDecrement:
                    return LowerOperator(node, isPrefix: false, isIncrement: false);
                default:
                    return base.VisitUnaryOperator(node);
            }
        }

        /// <summary>
        /// The rewrites are as follows:
        /// 
        /// x++
        ///     temp = x
        ///     x = temp + 1
        ///     return temp
        /// x--
        ///     temp = x
        ///     x = temp - 1
        ///     return temp
        /// ++x
        ///     temp = x + 1
        ///     x = temp
        ///     return temp
        /// --x
        ///     temp = x - 1
        ///     x = temp
        ///     return temp
        ///     
        /// In each case, the literal 1 is of the type required by the builtin addition/subtraction operator that
        /// will be used.  The temp is of the same type as x, but the sum/difference may be wider, in which case a
        /// conversion is required.
        /// </summary>
        /// <param name="node">The unary operator expression representing the increment/decrement.</param>
        /// <param name="isPrefix">True for prefix, false for postfix.</param>
        /// <param name="isIncrement">True for increment, false for decrement.</param>
        /// <returns>A bound sequence that uses a temp to acheive the correct side effects and return value.</returns>
        private BoundNode LowerOperator(BoundUnaryOperator node, bool isPrefix, bool isIncrement)
        {
            BoundExpression operand = node.Operand;
            TypeSymbol operandType = operand.Type; //type of the variable being incremented
            Debug.Assert(operandType == node.Type);

            ConstantValue constantOne;
            BinaryOperatorKind binaryOperatorKind;
            MakeConstantAndOperatorKind(node.OperatorKind.OperandTypes(), node, out constantOne, out binaryOperatorKind);
            binaryOperatorKind |= isIncrement ? BinaryOperatorKind.Addition : BinaryOperatorKind.Subtraction;

            Debug.Assert(constantOne != null);
            Debug.Assert(constantOne.SpecialType != SpecialType.None);
            Debug.Assert(binaryOperatorKind.OperandTypes() != 0);

            TypeSymbol constantType = compilation.GetSpecialType(constantOne.SpecialType);
            BoundExpression boundOne = new BoundLiteral(
                syntax: null,
                syntaxTree: null,
                constantValueOpt: constantOne,
                type: constantType);

            LocalSymbol tempSymbol = new TempLocalSymbol(operandType, RefKind.None, containingSymbol);
            BoundExpression boundTemp = new BoundLocal(
                syntax: null,
                syntaxTree: null,
                localSymbol: tempSymbol,
                constantValueOpt: null,
                type: operandType);

            // NOTE: the LHS may have a narrower type than the operator expects, but that
            // doesn't seem to cause any problems.  If a problem does arise, just add an
            // explicit BoundConversion.
            BoundExpression newValue = new BoundBinaryOperator(
                syntax: null,
                syntaxTree: null,
                operatorKind: binaryOperatorKind,
                left: isPrefix ? operand : boundTemp,
                right: boundOne,
                constantValueOpt: null,
                type: constantType);

            if (constantType != operandType)
            {
                newValue = new BoundConversion(
                    syntax: null,
                    syntaxTree: null,
                    operand: newValue,
                    conversionKind: operandType.IsEnumType() ? ConversionKind.ImplicitEnumeration : ConversionKind.ImplicitNumeric,
                    symbolOpt: null,
                    @checked: false,
                    explicitCastInCode: false,
                    constantValueOpt: null,
                    type: operandType);
            }

            ReadOnlyArray<BoundExpression> assignments = ReadOnlyArray<BoundExpression>.CreateFrom(
                new BoundAssignmentOperator(
                    syntax: null,
                    syntaxTree: null,
                    left: boundTemp,
                    right: isPrefix ? newValue : operand,
                    type: operandType),
                new BoundAssignmentOperator(
                    syntax: null,
                    syntaxTree: null,
                    left: operand,
                    right: isPrefix ? boundTemp : newValue,
                    type: operandType));

            return new BoundSequence(
                syntax: node.Syntax,
                syntaxTree: node.SyntaxTree,
                locals: ReadOnlyArray<LocalSymbol>.CreateFrom(tempSymbol),
                sideEffects: assignments,
                value: boundTemp,
                type: operandType);
        }

        /// <summary>
        /// By examining the node and the UnaryOperatorKind detemine which builtin operator should be used
        /// and create an appropriately-typed constant 1.
        /// </summary>
        /// <param name="unaryOperatorKindType">The operand type of the built-in increment/decrement operator.</param>
        /// <param name="node">The unary operation - used to extract an underlying enum type, if necessary.</param>
        /// <param name="constantOne">Will contain a constant of the type expected by the built-in operator corresponding to binaryOperatorKindType.</param>
        /// <param name="binaryOperatorKindType">The built-in binary operator that will be used to implement the built-in increment/decrement operator.  May have wider types.</param>
        private static void MakeConstantAndOperatorKind(UnaryOperatorKind unaryOperatorKindType, BoundUnaryOperator node, out ConstantValue constantOne, out BinaryOperatorKind binaryOperatorKindType)
        {
            switch (unaryOperatorKindType)
            {
                case UnaryOperatorKind.SByte:
                case UnaryOperatorKind.Short:
                case UnaryOperatorKind.Int:
                    constantOne = ConstantValue.ConstantValueOne.Int32;
                    binaryOperatorKindType = BinaryOperatorKind.Int;
                    break;
                case UnaryOperatorKind.Byte:
                case UnaryOperatorKind.UShort:
                case UnaryOperatorKind.UInt:
                case UnaryOperatorKind.Char:
                    constantOne = ConstantValue.ConstantValueOne.UInt32;
                    binaryOperatorKindType = BinaryOperatorKind.UInt;
                    break;
                case UnaryOperatorKind.Long:
                    constantOne = ConstantValue.ConstantValueOne.Int64;
                    binaryOperatorKindType = BinaryOperatorKind.Long;
                    break;
                case UnaryOperatorKind.ULong:
                    constantOne = ConstantValue.ConstantValueOne.UInt64;
                    binaryOperatorKindType = BinaryOperatorKind.ULong;
                    break;
                case UnaryOperatorKind.Float:
                    constantOne = ConstantValue.ConstantValueOne.Single;
                    binaryOperatorKindType = BinaryOperatorKind.Float;
                    break;
                case UnaryOperatorKind.Double:
                    constantOne = ConstantValue.ConstantValueOne.Double;
                    binaryOperatorKindType = BinaryOperatorKind.Double;
                    break;
                case UnaryOperatorKind.Decimal: //Dev10 special cased this, but we'll let DecimalRewriter handle it
                    constantOne = ConstantValue.ConstantValueOne.Decimal;
                    binaryOperatorKindType = BinaryOperatorKind.Decimal;
                    break;
                case UnaryOperatorKind.Enum:
                    SpecialType underlyingSpecialType = node.Type.GetEnumUnderlyingType().SpecialType;
                    switch (underlyingSpecialType)
                    {
                        case SpecialType.System_Int32: MakeConstantAndOperatorKind(UnaryOperatorKind.Int, node, out constantOne, out binaryOperatorKindType); return;
                        case SpecialType.System_UInt32: MakeConstantAndOperatorKind(UnaryOperatorKind.UInt, node, out constantOne, out binaryOperatorKindType); return;
                        case SpecialType.System_Int64: MakeConstantAndOperatorKind(UnaryOperatorKind.Long, node, out constantOne, out binaryOperatorKindType); return;
                        case SpecialType.System_UInt64: MakeConstantAndOperatorKind(UnaryOperatorKind.ULong, node, out constantOne, out binaryOperatorKindType); return;
                        case SpecialType.System_SByte: MakeConstantAndOperatorKind(UnaryOperatorKind.SByte, node, out constantOne, out binaryOperatorKindType); return;
                        case SpecialType.System_Byte: MakeConstantAndOperatorKind(UnaryOperatorKind.Byte, node, out constantOne, out binaryOperatorKindType); return;
                        case SpecialType.System_Int16: MakeConstantAndOperatorKind(UnaryOperatorKind.Short, node, out constantOne, out binaryOperatorKindType); return;
                        case SpecialType.System_UInt16: MakeConstantAndOperatorKind(UnaryOperatorKind.UShort, node, out constantOne, out binaryOperatorKindType); return;
                        default: throw new InvalidOperationException("Unexpected enum underlying type: " + underlyingSpecialType);
                    }
                case UnaryOperatorKind.Pointer:
                    //UNDONE: pointer operations
                    throw new NotImplementedException();
                case UnaryOperatorKind.UserDefined:
                    //UNDONE: overloaded increment/decrement operators
                    throw new NotImplementedException();
                case UnaryOperatorKind.Bool:
                    Debug.Assert(false); //Operator does not exist
                    goto default;
                default:
                    throw new InvalidOperationException("Unexpected operator type: " + unaryOperatorKindType);
            }
        }
    }
}