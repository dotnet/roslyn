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
                    return ConstantValue.Create(value != 0);
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
    }

    public sealed class ConditionalChoice : IConditionalChoiceExpression
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

        public OperationKind Kind => OperationKind.ConditionalChoiceExpression;

        public bool IsInvalid => Condition == null || Condition.IsInvalid || IfTrue == null || IfTrue.IsInvalid || IfFalse == null || IfFalse.IsInvalid;

        public object ConstantValue => null;
    }

    public sealed class Assignment : IExpressionStatement
    {
        private readonly AssignmentExpression _assignment;

        public Assignment(IReferenceExpression target, IExpression value, SyntaxNode syntax)
        {
            _assignment = new AssignmentExpression(target, value, syntax);
            this.Syntax = syntax;
        }

        public SyntaxNode Syntax { get; }

        public OperationKind Kind => OperationKind.ExpressionStatement;

        public bool IsInvalid => _assignment.IsInvalid;

        public IExpression Expression => _assignment;

        class AssignmentExpression : IAssignmentExpression
        {
            public AssignmentExpression(IReferenceExpression target, IExpression value, SyntaxNode syntax)
            {
                this.Value = value;
                this.Target = target;
                this.Syntax = syntax;
            }

            public IReferenceExpression Target { get; }

            public IExpression Value { get; }

            public SyntaxNode Syntax { get; }

            public ITypeSymbol ResultType => this.Target.ResultType;

            public OperationKind Kind => OperationKind.AssignmentExpression;

            public bool IsInvalid => Target == null || Target.IsInvalid || Value == null || Value.IsInvalid;

            public object ConstantValue => null;
        }
    }

    public sealed class CompoundAssignment : IExpressionStatement
    {
        private readonly CompoundAssignmentExpression _compoundAssignment;

        public CompoundAssignment(IReferenceExpression target, IExpression value, BinaryOperationKind binaryKind, IMethodSymbol operatorMethod, SyntaxNode syntax)
        {
            _compoundAssignment = new CompoundAssignmentExpression(target, value, binaryKind, operatorMethod, syntax);
            this.Syntax = syntax;
        }

        public SyntaxNode Syntax { get; }

        public OperationKind Kind => OperationKind.ExpressionStatement;

        public bool IsInvalid => _compoundAssignment.IsInvalid;

        public IExpression Expression => _compoundAssignment;

        class CompoundAssignmentExpression : ICompoundAssignmentExpression
        {
            public CompoundAssignmentExpression(IReferenceExpression target, IExpression value, BinaryOperationKind binaryKind, IMethodSymbol operatorMethod, SyntaxNode syntax)
            {
                this.Target = target;
                this.Value = value;
                this.BinaryKind = binaryKind;
                this.Operator = operatorMethod;
                this.Syntax = syntax;
            }

            public IReferenceExpression Target { get; }

            public IExpression Value { get; }

            public BinaryOperationKind BinaryKind { get; }

            public IMethodSymbol Operator { get; }

            public SyntaxNode Syntax { get; }

            public ITypeSymbol ResultType => this.Target.ResultType;

            public OperationKind Kind => OperationKind.CompoundAssignmentExpression;

            public bool IsInvalid => Target == null || Target.IsInvalid || Value == null || Value.IsInvalid;

            public object ConstantValue => null;

            public bool UsesOperatorMethod => this.Operator != null;
        }
    }

    public sealed class IntegerLiteral : ILiteralExpression
    {
        private readonly long _value;

        public IntegerLiteral(long value, ITypeSymbol resultType, SyntaxNode syntax)
        {
            _value = value;
            this.ResultType = resultType;
            this.Syntax = syntax;
        }
        
        public string Spelling =>_value.ToString();

        public ITypeSymbol ResultType { get; }

        public OperationKind Kind => OperationKind.LiteralExpression;

        public bool IsInvalid => false;

        public object ConstantValue => _value;

        public SyntaxNode Syntax { get; }
    }

    internal class Literal : ILiteralExpression
    {
        private readonly ConstantValue _value;

        public Literal(ConstantValue value, ITypeSymbol resultType, SyntaxNode syntax)
        {
            _value = value;
            this.ResultType = resultType;
            this.Syntax = syntax;
        }

        public string Spelling => _value.Value.ToString();

        public ITypeSymbol ResultType { get; }

        public OperationKind Kind => OperationKind.LiteralExpression;

        public bool IsInvalid => false;

        public object ConstantValue => _value.Value;

        public SyntaxNode Syntax { get; }
    }

    public sealed class Binary : IBinaryOperatorExpression
    {
        public Binary(BinaryOperationKind binaryKind, IExpression left, IExpression right, ITypeSymbol resultType, SyntaxNode syntax)
        {
            this.BinaryOperationKind = binaryKind;
            this.Left = left;
            this.Right = right;
            this.ResultType = resultType;
            this.Syntax = syntax;
        }

        public BinaryOperationKind BinaryOperationKind { get; }

        public IExpression Left { get; }

        public IExpression Right { get; }

        public bool UsesOperatorMethod => false;

        public IMethodSymbol Operator => null;

        public ITypeSymbol ResultType { get; }

        public OperationKind Kind => OperationKind.BinaryOperatorExpression;

        public bool IsInvalid => Left == null || Left.IsInvalid || Right == null || Right.IsInvalid;

        public object ConstantValue => null;

        public SyntaxNode Syntax { get; }
    }

    public sealed class ArrayCreation: IArrayCreationExpression
    {
        private readonly IArrayTypeSymbol _arrayType;

        public ArrayCreation(IArrayTypeSymbol arrayType, ImmutableArray<IExpression> elementValues, SyntaxNode syntax)
        {
            _arrayType = arrayType;
            this.DimensionSizes = ImmutableArray.Create<IExpression>(new IntegerLiteral(elementValues.Count(), null, syntax));
            this.Initializer = new ArrayInitializer(elementValues, syntax, arrayType);
            this.Syntax = syntax;
        }

        public ITypeSymbol ResultType => _arrayType;

        public ImmutableArray<IExpression> DimensionSizes { get; }

        public ITypeSymbol ElementType => _arrayType.ElementType;

        public IArrayInitializer Initializer { get; }

        public SyntaxNode Syntax { get; }

        public OperationKind Kind => OperationKind.ArrayCreationExpression;

        public bool IsInvalid => IsInvalidInitializer(Initializer);
       
        static bool IsInvalidInitializer(IArrayInitializer initializer) => initializer.IsInvalid;

        public object ConstantValue => null;

        private class ArrayInitializer : IArrayInitializer
        {
            public ArrayInitializer(ImmutableArray<IExpression> elementValues, SyntaxNode syntax, ITypeSymbol arrayType)
            {
                ArrayBuilder<IExpression> builder = ArrayBuilder<IExpression>.GetInstance();
                foreach (IExpression element in elementValues)
                {
                    builder.Add(element);
                }

                ElementValues = builder.ToImmutableAndFree();
                Syntax = syntax;
                ResultType = arrayType;
            }

            public object ConstantValue => null;

            public ImmutableArray<IExpression> ElementValues { get; }

            public bool IsInvalid => ElementValues.Any(v => v.IsInvalid);

            public OperationKind Kind => OperationKind.ArrayInitializer;

            public ITypeSymbol ResultType { get; }

            public SyntaxNode Syntax { get; }
        }
    }
    
}
