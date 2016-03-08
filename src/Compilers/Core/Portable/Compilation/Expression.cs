﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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

            return ConstantValue.Bad;
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
                return BinaryOperationKind.EnumAdd;
            }

            return BinaryOperationKind.Invalid;
        }
    }

    internal sealed class ConditionalChoice : IConditionalChoiceExpression
    {
        public ConditionalChoice(IOperation condition, IOperation ifTrue, IOperation ifFalse, ITypeSymbol resultType, SyntaxNode syntax)
        {
            this.Condition = condition;
            this.IfTrueValue = ifTrue;
            this.IfFalseValue = ifFalse;
            this.Type = resultType;
            this.Syntax = syntax;
        }

        public IOperation Condition { get; }

        public IOperation IfTrueValue { get; }

        public IOperation IfFalseValue { get; }

        public ITypeSymbol Type { get; }

        public SyntaxNode Syntax { get; }

        public OperationKind Kind => OperationKind.ConditionalChoiceExpression;

        public bool IsInvalid => Condition == null || Condition.IsInvalid || IfTrueValue == null || IfTrueValue.IsInvalid || IfFalseValue == null || IfFalseValue.IsInvalid || Type == null;

        public Optional<object> ConstantValue => default(Optional<object>);

        public void Accept(OperationVisitor visitor)
        {
            visitor.VisitConditionalChoiceExpression(this);
        }

        public TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitConditionalChoiceExpression(this, argument);
        }
    }

    internal sealed class Assignment : IExpressionStatement
    {
        private readonly AssignmentExpression _assignment;

        public Assignment(IOperation target, IOperation value, SyntaxNode syntax)
        {
            _assignment = new AssignmentExpression(target, value, syntax);
            this.Syntax = syntax;
        }

        public SyntaxNode Syntax { get; }

        public OperationKind Kind => OperationKind.ExpressionStatement;

        public bool IsInvalid => _assignment.IsInvalid;

        public IOperation Expression => _assignment;

        public ITypeSymbol Type => null;

        public Optional<object> ConstantValue => default(Optional<object>);

        public void Accept(OperationVisitor visitor)
        {
            visitor.VisitExpressionStatement(this);
        }

        public TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitExpressionStatement(this, argument);
        }

        private sealed class AssignmentExpression : IAssignmentExpression
        {
            public AssignmentExpression(IOperation target, IOperation value, SyntaxNode syntax)
            {
                this.Value = value;
                this.Target = target;
                this.Syntax = syntax;
            }

            public IOperation Target { get; }

            public IOperation Value { get; }

            public SyntaxNode Syntax { get; }

            public ITypeSymbol Type => this.Target.Type;

            public OperationKind Kind => OperationKind.AssignmentExpression;

            public bool IsInvalid => Target == null || Target.IsInvalid || Value == null || Value.IsInvalid;

            public Optional<object> ConstantValue => default(Optional<object>);

            public void Accept(OperationVisitor visitor)
            {
                visitor.VisitAssignmentExpression(this);
            }

            public TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            {
                return visitor.VisitAssignmentExpression(this, argument);
            }
        }
    }

    internal sealed class CompoundAssignment : IExpressionStatement
    {
        private readonly CompoundAssignmentExpression _compoundAssignment;

        public CompoundAssignment(IOperation target, IOperation value, BinaryOperationKind binaryOperationKind, IMethodSymbol operatorMethod, SyntaxNode syntax)
        {
            _compoundAssignment = new CompoundAssignmentExpression(target, value, binaryOperationKind, operatorMethod, syntax);
            this.Syntax = syntax;
        }

        public SyntaxNode Syntax { get; }

        public OperationKind Kind => OperationKind.ExpressionStatement;

        public bool IsInvalid => _compoundAssignment.IsInvalid;

        public IOperation Expression => _compoundAssignment;

        public ITypeSymbol Type => null;

        public Optional<object> ConstantValue => default(Optional<object>);

        public void Accept(OperationVisitor visitor)
        {
            visitor.VisitExpressionStatement(this);
        }

        public TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitExpressionStatement(this, argument);
        }

        private sealed class CompoundAssignmentExpression : ICompoundAssignmentExpression
        {
            public CompoundAssignmentExpression(IOperation target, IOperation value, BinaryOperationKind binaryOperationKind, IMethodSymbol operatorMethod, SyntaxNode syntax)
            {
                this.Target = target;
                this.Value = value;
                this.BinaryOperationKind = binaryOperationKind;
                this.OperatorMethod = operatorMethod;
                this.Syntax = syntax;
            }

            public IOperation Target { get; }

            public IOperation Value { get; }

            public BinaryOperationKind BinaryOperationKind { get; }

            public IMethodSymbol OperatorMethod { get; }

            public SyntaxNode Syntax { get; }

            public ITypeSymbol Type => this.Target.Type;

            public OperationKind Kind => OperationKind.CompoundAssignmentExpression;

            public bool IsInvalid => Target == null || Target.IsInvalid || Value == null || Value.IsInvalid;

            public Optional<object> ConstantValue => default(Optional<object>);

            public bool UsesOperatorMethod => this.OperatorMethod != null;

            public void Accept(OperationVisitor visitor)
            {
                visitor.VisitCompoundAssignmentExpression(this);
            }

            public TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            {
                return visitor.VisitCompoundAssignmentExpression(this, argument);
            }
        }
    }

    internal sealed class IntegerLiteral : ILiteralExpression
    {
        private readonly long _value;

        public IntegerLiteral(long value, ITypeSymbol resultType, SyntaxNode syntax)
        {
            _value = value;
            this.Type = resultType;
            this.Syntax = syntax;
        }

        public string Text =>_value.ToString();

        public ITypeSymbol Type { get; }

        public OperationKind Kind => OperationKind.LiteralExpression;

        public bool IsInvalid => false;

        public Optional<object> ConstantValue => new Optional<object>(_value);

        public SyntaxNode Syntax { get; }

        public void Accept(OperationVisitor visitor)
        {
            visitor.VisitLiteralExpression(this);
        }

        public TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitLiteralExpression(this, argument);
        }
    }

    internal class Literal : ILiteralExpression
    {
        private readonly ConstantValue _value;

        public Literal(ConstantValue value, ITypeSymbol resultType, SyntaxNode syntax)
        {
            Debug.Assert(value != null, "value can't be null");
            _value = value;
            this.Type = resultType;
            this.Syntax = syntax;
        }

        public string Text => _value.GetValueToDisplay();

        public ITypeSymbol Type { get; }

        public OperationKind Kind => OperationKind.LiteralExpression;

        public bool IsInvalid => _value.IsBad;

        public Optional<object> ConstantValue => new Optional<object>(_value.Value);

        public SyntaxNode Syntax { get; }

        public void Accept(OperationVisitor visitor)
        {
            visitor.VisitLiteralExpression(this);
        }

        public TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitLiteralExpression(this, argument);
        }
    }

    internal sealed class Binary : IBinaryOperatorExpression
    {
        public Binary(BinaryOperationKind binaryOperationKind, IOperation left, IOperation right, ITypeSymbol resultType, SyntaxNode syntax)
        {
            this.BinaryOperationKind = binaryOperationKind;
            this.LeftOperand = left;
            this.RightOperand = right;
            this.Type = resultType;
            this.Syntax = syntax;
        }

        public BinaryOperationKind BinaryOperationKind { get; }

        public IOperation LeftOperand { get; }

        public IOperation RightOperand { get; }

        public bool UsesOperatorMethod => false;

        public IMethodSymbol OperatorMethod => null;

        public ITypeSymbol Type { get; }

        public OperationKind Kind => OperationKind.BinaryOperatorExpression;

        public bool IsInvalid => LeftOperand == null 
                                || LeftOperand.IsInvalid 
                                || RightOperand == null
                                || RightOperand.IsInvalid 
                                || BinaryOperationKind == BinaryOperationKind.Invalid 
                                || Type == null;

        public Optional<object> ConstantValue => default(Optional<object>);

        public SyntaxNode Syntax { get; }

        public void Accept(OperationVisitor visitor)
        {
            visitor.VisitBinaryOperatorExpression(this);
        }

        public TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitBinaryOperatorExpression(this, argument);
        }
    }

    internal sealed class ArrayCreation : IArrayCreationExpression
    {
        private readonly IArrayTypeSymbol _arrayType;

        public ArrayCreation(IArrayTypeSymbol arrayType, ImmutableArray<IOperation> elementValues, SyntaxNode syntax)
        {
            _arrayType = arrayType;
            this.DimensionSizes = ImmutableArray.Create<IOperation>(new IntegerLiteral(elementValues.Count(), null, syntax));
            this.Initializer = new ArrayInitializer(elementValues, syntax, arrayType);
            this.Syntax = syntax;
        }

        public ITypeSymbol Type => _arrayType;

        public ImmutableArray<IOperation> DimensionSizes { get; }

        public ITypeSymbol ElementType => _arrayType.ElementType;

        public IArrayInitializer Initializer { get; }

        public SyntaxNode Syntax { get; }

        public OperationKind Kind => OperationKind.ArrayCreationExpression;

        public bool IsInvalid => IsInvalidInitializer(Initializer);

        private static bool IsInvalidInitializer(IArrayInitializer initializer) => initializer.IsInvalid;

        public void Accept(OperationVisitor visitor)
        {
            visitor.VisitArrayCreationExpression(this);
        }

        public TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitArrayCreationExpression(this, argument);
        }

        public Optional<object> ConstantValue => default(Optional<object>);

        private sealed class ArrayInitializer : IArrayInitializer
        {
            public ArrayInitializer(ImmutableArray<IOperation> elementValues, SyntaxNode syntax, ITypeSymbol arrayType)
            {
                ElementValues = elementValues;
                Syntax = syntax;
                Type = arrayType;
            }

            public ImmutableArray<IOperation> ElementValues { get; }

            public bool IsInvalid => ElementValues.Any(v => v.IsInvalid);

            public OperationKind Kind => OperationKind.ArrayInitializer;

            public ITypeSymbol Type { get; }

            public SyntaxNode Syntax { get; }

            public Optional<object> ConstantValue => default(Optional<object>);

            public void Accept(OperationVisitor visitor)
            {
                visitor.VisitArrayInitializer(this);
            }

            public TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            {
                return visitor.VisitArrayInitializer(this, argument);
            }
        }
    }

    internal sealed class InvalidExpression : IInvalidExpression
    {
        public InvalidExpression(SyntaxNode syntax)
        {
            this.Syntax = syntax;
        }

        public Optional<object> ConstantValue => default(Optional<object>);

        public bool IsInvalid => true;

        public OperationKind Kind => OperationKind.InvalidExpression;

        public SyntaxNode Syntax { get; }

        public ITypeSymbol Type => null;

        public void Accept(OperationVisitor visitor)
        {
            visitor.VisitInvalidExpression(this);
        }

        public TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitInvalidExpression(this, argument);
        }
    }
}
