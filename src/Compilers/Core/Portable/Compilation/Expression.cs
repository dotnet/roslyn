// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

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

        public static BinaryOperationKind DeriveAdditionKind(ITypeSymbol type)
        {
            switch (type.SpecialType)
            {
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                case SpecialType.System_Int16:
                case SpecialType.System_SByte:
                    return BinaryOperationKind.IntegerAdd;
                case SpecialType.System_UInt32:
                case SpecialType.System_UInt64:
                case SpecialType.System_UInt16:
                case SpecialType.System_Byte:
                case SpecialType.System_Char:
                case SpecialType.System_Boolean:
                    return BinaryOperationKind.UnsignedAdd;
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                    return BinaryOperationKind.FloatingAdd;
                case SpecialType.System_Object:
                    return BinaryOperationKind.ObjectAdd;
            }

            if (type.TypeKind == TypeKind.Enum)
            {
                return Semantics.BinaryOperationKind.EnumAdd;
            }

            return Semantics.BinaryOperationKind.None;
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
        public Relational(RelationalOperationKind relationalKind, IExpression left, IExpression right, ITypeSymbol resultType, IMethodSymbol operatorMethod, SyntaxNode syntax)
        {
            this.RelationalKind = relationalKind;
            this.Left = left;
            this.Right = right;
            this.ResultType = resultType;
            this.Operator = operatorMethod;
            this.Syntax = syntax;
        }

        public RelationalOperationKind RelationalKind { get; }
        
        public IExpression Left { get; }
        
        public IExpression Right { get; }
        
        public SyntaxNode Syntax { get; }

        public bool UsesOperatorMethod => this.Operator != null;

        public IMethodSymbol Operator { get; }

        public ITypeSymbol ResultType { get; }

        public OperationKind Kind => OperationKind.RelationalOperator;

        public object ConstantValue => null;
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

        public IExpression Condition { get; }

        public IExpression IfTrue { get; }

        public IExpression IfFalse { get; }

        public ITypeSymbol ResultType { get; }

        public SyntaxNode Syntax { get; }

        public OperationKind Kind => OperationKind.ConditionalChoice;

        public object ConstantValue => null;
    }

    public class Assignment : IExpressionStatement
    {
        AssignmentExpression assignment;

        public Assignment(IReference target, IExpression value, SyntaxNode syntax)
        {
            this.assignment = new AssignmentExpression(target, value, syntax);
            this.Syntax = syntax;
        }

        public SyntaxNode Syntax { get; }

        public OperationKind Kind => OperationKind.ExpressionStatement;

        public IExpression Expression => this.assignment;

        class AssignmentExpression : IAssignment
        {
            public AssignmentExpression(IReference target, IExpression value, SyntaxNode syntax)
            {
                this.Value = value;
                this.Target = target;
                this.Syntax = syntax;
            }

            public IReference Target { get; }

            public IExpression Value { get; }

            public SyntaxNode Syntax { get; }

            public ITypeSymbol ResultType => this.Target.ResultType;

            public OperationKind Kind => OperationKind.Assignment;

            public object ConstantValue => null;
        }
    }

    public class CompoundAssignment : IExpressionStatement
    {
        CompoundAssignmentExpression _compoundAssignment;

        public CompoundAssignment(IReference target, IExpression value, BinaryOperationKind binaryKind, IMethodSymbol operatorMethod, SyntaxNode syntax)
        {
            _compoundAssignment = new CompoundAssignmentExpression(target, value, binaryKind, operatorMethod, syntax);
            this.Syntax = syntax;
        }

        public SyntaxNode Syntax { get; }

        public OperationKind Kind => OperationKind.ExpressionStatement;

        public IExpression Expression => _compoundAssignment;

        class CompoundAssignmentExpression : ICompoundAssignment
        {
            public CompoundAssignmentExpression(IReference target, IExpression value, BinaryOperationKind binaryKind, IMethodSymbol operatorMethod, SyntaxNode syntax)
            {
                this.Target = target;
                this.Value = value;
                this.BinaryKind = binaryKind;
                this.Operator = operatorMethod;
                this.Syntax = syntax;
            }

            public IReference Target { get; }

            public IExpression Value { get; }

            public BinaryOperationKind BinaryKind { get; }

            public IMethodSymbol Operator { get; }

            public SyntaxNode Syntax { get; }

            public ITypeSymbol ResultType => this.Target.ResultType;

            public OperationKind Kind => OperationKind.CompoundAssignment;

            public object ConstantValue => null;

            public bool UsesOperatorMethod => this.Operator != null;
        }
    }

    public class IntegerLiteral : ILiteral
    {
        long _value;

        public IntegerLiteral(long value, ITypeSymbol resultType, SyntaxNode syntax)
        {
            _value = value;
            this.ResultType = resultType;
            this.Syntax = syntax;
        }

        public LiteralKind LiteralClass => LiteralKind.Integer;

        public string Spelling =>_value.ToString();

        public ITypeSymbol ResultType { get; }

        public OperationKind Kind => OperationKind.Literal;

        public object ConstantValue => _value;

        public SyntaxNode Syntax { get; }
    }

    internal class Literal : ILiteral
    {
        ConstantValue _value;

        public Literal(ConstantValue value, ITypeSymbol resultType, SyntaxNode syntax)
        {
            _value = value;
            this.ResultType = resultType;
            this.Syntax = syntax;
        }

        public LiteralKind LiteralClass => Semantics.Expression.DeriveLiteralKind(this.ResultType);

        public string Spelling => _value.Value.ToString();

        public ITypeSymbol ResultType { get; }

        public OperationKind Kind => OperationKind.Literal;

        public object ConstantValue => _value.Value;

        public SyntaxNode Syntax { get; }
    }

    public class Binary : IBinary
    {
        public Binary(BinaryOperationKind binaryKind, IExpression left, IExpression right, ITypeSymbol resultType, SyntaxNode syntax)
        {
            this.BinaryKind = binaryKind;
            this.Left = left;
            this.Right = right;
            this.ResultType = resultType;
            this.Syntax = syntax;
        }

        public BinaryOperationKind BinaryKind { get; }

        public IExpression Left { get; }

        public IExpression Right { get; }

        public bool UsesOperatorMethod => false;

        public IMethodSymbol Operator => null;

        public ITypeSymbol ResultType { get; }

        public OperationKind Kind => OperationKind.BinaryOperator;

        public object ConstantValue => null;

        public SyntaxNode Syntax { get; }
    }

    public class ArrayCreation: IArrayCreation
    {
        private readonly IArrayTypeSymbol _arrayType;

        public ArrayCreation(IArrayTypeSymbol arrayType, IEnumerable<IExpression> elementValues, SyntaxNode syntax)
        {
            _arrayType = arrayType;
            this.DimensionSizes = ImmutableArray.Create<IExpression>(new IntegerLiteral(elementValues.Count(), null, syntax));
            this.ElementValues = new DimensionInitializer(elementValues);
            this.Syntax = syntax;
        }

        public ITypeSymbol ResultType => _arrayType;

        public ImmutableArray<IExpression> DimensionSizes { get; }

        public ITypeSymbol ElementType => _arrayType.ElementType;

        public IArrayInitializer ElementValues { get; }

        public SyntaxNode Syntax { get; }

        public OperationKind Kind => OperationKind.ArrayCreation;

        public object ConstantValue => null;

        private class DimensionInitializer : IDimensionArrayInitializer
        {
            private ImmutableArray<IArrayInitializer> _elementValues;

            public DimensionInitializer(IEnumerable<IExpression> elementValues)
            {
                ImmutableArray<IArrayInitializer>.Builder builder = ImmutableArray.CreateBuilder<IArrayInitializer>();
                foreach (IExpression element in elementValues)
                {
                    builder.Add(new ExpressionInitializer(element));
                }

                _elementValues = builder.ToImmutable();
            }

            public ArrayInitializerKind ArrayClass => ArrayInitializerKind.Dimension;

            public ImmutableArray<IArrayInitializer> ElementValues => _elementValues;
        }

        private class ExpressionInitializer : IExpressionArrayInitializer
        {
            public ExpressionInitializer(IExpression expression)
            {
                ElementValue = expression;
            }

            public ArrayInitializerKind ArrayClass => ArrayInitializerKind.Expression;

            public IExpression ElementValue { get; }
        }
    }
    
}
