using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    internal class Expression
    {
        public static ConstantValue SynthesizeNumeric(ITypeSymbol type, int value)
        {
            switch (type.SpecialType)
            {
                case SpecialType.System_Int32:
                    return ConstantValue.Create(value);
                case SpecialType.System_Int64:
                    return ConstantValue.Create((long)value);
                case SpecialType.System_UInt32:
                    return ConstantValue.Create((uint)value);
                case SpecialType.System_UInt64:
                    return ConstantValue.Create((ulong)value);
                case SpecialType.System_UInt16:
                    return ConstantValue.Create((ushort)value);
                case SpecialType.System_Int16:
                    return ConstantValue.Create((short)value);
                case SpecialType.System_SByte:
                    return ConstantValue.Create((sbyte)value);
                case SpecialType.System_Byte:
                    return ConstantValue.Create((byte)value);
                case SpecialType.System_Char:
                    return ConstantValue.Create((char)value);
                case SpecialType.System_Boolean:
                    return ConstantValue.Create(value != 0 ? true : false);
                case SpecialType.System_Single:
                    return ConstantValue.Create((float)value);
                case SpecialType.System_Double:
                    return ConstantValue.Create((double)value);
                case SpecialType.System_Object:
                    return ConstantValue.Create(1, ConstantValueTypeDiscriminator.Int32);
            }

            if (type.TypeKind == TypeKind.Enum)
            {
                return SynthesizeNumeric(((INamedTypeSymbol)type).EnumUnderlyingType, value);
            }

            return null;
        }

        public static BinaryOperatorCode DeriveAdditionCode(ITypeSymbol type)
        {
            switch (type.SpecialType)
            {
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                case SpecialType.System_Int16:
                case SpecialType.System_SByte:
                    return BinaryOperatorCode.IntegerAdd;
                case SpecialType.System_UInt32:
                case SpecialType.System_UInt64:
                case SpecialType.System_UInt16:
                case SpecialType.System_Byte:
                case SpecialType.System_Char:
                case SpecialType.System_Boolean:
                    return BinaryOperatorCode.UnsignedAdd;
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                    return BinaryOperatorCode.FloatingAdd;
                case SpecialType.System_Object:
                    return BinaryOperatorCode.ObjectAdd;
            }

            if (type.TypeKind == TypeKind.Enum)
            {
                return Semantics.BinaryOperatorCode.EnumAdd;
            }

            return Semantics.BinaryOperatorCode.None;
        }

        public static LiteralKind DeriveLiteralKind(ITypeSymbol type)
        {
            switch (type.SpecialType)
            {
                case SpecialType.System_Boolean:
                    return Semantics.LiteralKind.Boolean;
                case SpecialType.System_Byte:
                case SpecialType.System_Int16:
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                case SpecialType.System_IntPtr:
                case SpecialType.System_SByte:
                case SpecialType.System_UInt16:
                case SpecialType.System_UInt32:
                case SpecialType.System_UInt64:
                case SpecialType.System_UIntPtr:
                    return Semantics.LiteralKind.Integer;
                case SpecialType.System_DateTime:
                    return Semantics.LiteralKind.DateTime;
                case SpecialType.System_Decimal:
                    return Semantics.LiteralKind.Decimal;
                case SpecialType.System_Double:
                case SpecialType.System_Single:
                    return Semantics.LiteralKind.Floating;
                case SpecialType.System_Char:
                    return Semantics.LiteralKind.Character;
                case SpecialType.System_String:
                    return Semantics.LiteralKind.String;
            }

            return Semantics.LiteralKind.None;
        }
    }

    public class Relational : IRelational
    {
        public Relational(RelationalOperatorCode relationalCode, IExpression left, IExpression right, ITypeSymbol resultType, IMethodSymbol operatorMethod, SyntaxNode syntax)
        {
            this.RelationalCode = relationalCode;
            this.Left = left;
            this.Right = right;
            this.ResultType = resultType;
            this.Operator = operatorMethod;
            this.Syntax = syntax;
        }

        public RelationalOperatorCode RelationalCode { get; set; }
        
        public IExpression Left { get; set; }
        
        public IExpression Right { get; set; }
        
        public SyntaxNode Syntax { get; set; }

        public bool UsesOperatorMethod
        {
            get { return this.Operator != null; }
        }

        public IMethodSymbol Operator { get; set; }

        public ITypeSymbol ResultType { get; set; }

        public OperationKind Kind
        {
            get { return OperationKind.RelationalOperator; }
        }

        public object ConstantValue
        {
            get { return null; }
        }
    }

    public class ConditionalChoice : IConditionalChoice
    {
        public ConditionalChoice(IExpression condition, IExpression ifTrue, IExpression ifFalse, ITypeSymbol resultType, SyntaxNode syntax)
        {
            this.Condition = condition;
            this.IfTrue = ifTrue;
            this.IfFalse = ifFalse;
            this.ResultType = resultType;
            this.Syntax = syntax;
        }

        public IExpression Condition { get; set; }

        public IExpression IfTrue { get; set; }

        public IExpression IfFalse { get; set; }

        public ITypeSymbol ResultType { get; set; }

        public SyntaxNode Syntax { get; set; }

        public OperationKind Kind
        {
            get { return OperationKind.ConditionalChoice; }
        }

        public object ConstantValue
        {
            get { return null; }
        }
    }

    public class Assignment : IExpressionStatement
    {
        AssignmentExpression assignment;

        public Assignment(IReference target, IExpression value, SyntaxNode syntax)
        {
            this.assignment = new AssignmentExpression(target, value, syntax);
            this.Syntax = syntax;
        }

        public SyntaxNode Syntax { get; set; }

        public OperationKind Kind
        {
            get { return OperationKind.ExpressionStatement; }
        }

        public IExpression Expression
        {
            get { return this.assignment; }
        }

        class AssignmentExpression : IAssignment
        {
            public AssignmentExpression(IReference target, IExpression value, SyntaxNode syntax)
            {
                this.Value = value;
                this.Target = target;
                this.Syntax = syntax;
            }

            public IReference Target { get; set; }

            public IExpression Value { get; set; }

            public SyntaxNode Syntax { get; set; }

            public ITypeSymbol ResultType
            {
                get { return this.Target.ResultType; }
            }

            public OperationKind Kind
            {
                get { return OperationKind.Assignment; }
            }

            public object ConstantValue
            {
                get { return null; }
            }
        }
    }

    public class CompoundAssignment : IExpressionStatement
    {
        CompoundAssignmentExpression compoundAssignment;

        public CompoundAssignment(IReference target, IExpression value, BinaryOperatorCode operation, IMethodSymbol operatorMethod, SyntaxNode syntax)
        {
            this.compoundAssignment = new CompoundAssignmentExpression(target, value, operation, operatorMethod, syntax);
            this.Syntax = syntax;
        }

        public SyntaxNode Syntax { get; set; }

        public OperationKind Kind
        {
            get { return OperationKind.ExpressionStatement; }
        }

        public IExpression Expression
        {
            get { return this.compoundAssignment; }
        }

        class CompoundAssignmentExpression : ICompoundAssignment
        {
            public CompoundAssignmentExpression(IReference target, IExpression value, BinaryOperatorCode operation, IMethodSymbol operatorMethod, SyntaxNode syntax)
            {
                this.Target = target;
                this.Value = value;
                this.Operation = operation;
                this.Operator = operatorMethod;
                this.Syntax = syntax;
            }

            public IReference Target { get; set; }

            public IExpression Value { get; set; }

            public BinaryOperatorCode Operation { get; set; }

            public IMethodSymbol Operator { get; set; }

            public SyntaxNode Syntax { get; set; }

            public ITypeSymbol ResultType
            {
                get { return this.Target.ResultType; }
            }

            public OperationKind Kind
            {
                get { return OperationKind.CompoundAssignment; }
            }

            public object ConstantValue
            {
                get { return null; }
            }

            public bool UsesOperatorMethod
            {
                get { return this.Operator != null; }
            }
        }
    }

    public class IntegerLiteral : ILiteral
    {
        long value;

        public IntegerLiteral(long value, ITypeSymbol resultType, SyntaxNode syntax)
        {
            this.value = value;
            this.ResultType = resultType;
            this.Syntax = syntax;
        }

        public LiteralKind LiteralClass
        {
            get { return LiteralKind.Integer; }
        }

        public string Spelling
        {
            get { return this.value.ToString(); }
        }

        public ITypeSymbol ResultType { get; set; }

        public OperationKind Kind
        {
            get { return OperationKind.Literal; }
        }

        public object ConstantValue
        {
            get { return this.value; }
        }

        public SyntaxNode Syntax { get; set; }
    }

    public class Binary : IBinary
    {
        public Binary(BinaryOperatorCode operation, IExpression left, IExpression right, ITypeSymbol resultType, SyntaxNode syntax)
        {
            this.Operation = operation;
            this.Left = left;
            this.Right = right;
            this.ResultType = resultType;
            this.Syntax = syntax;
        }

        public BinaryOperatorCode Operation { get; set; }

        public IExpression Left { get; set; }

        public IExpression Right { get; set; }

        public bool UsesOperatorMethod
        {
            get { return false; }
        }

        public IMethodSymbol Operator
        {
            get { return null; }
        }

        public ITypeSymbol ResultType { get; set; }

        public OperationKind Kind
        {
            get { return OperationKind.BinaryOperator; }
        }

        public object ConstantValue
        {
            get { return null; }
        }

        public SyntaxNode Syntax { get; set; }
    }
}
