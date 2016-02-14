// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    public interface IHasArgumentsExpression : IOperation
    {
        /// <summary>
        /// Arguments of the invocation, excluding the instance argument. Arguments are in parameter order,
        /// and params/ParamArray arguments have been collected into arrays. Default values are supplied for
        /// optional arguments missing in source.
        /// </summary>
        ImmutableArray<IArgument> ArgumentsInParameterOrder { get; }
        /// <summary>
        /// Find the argument supplied for a given parameter of the target method.
        /// </summary>
        /// <param name="parameter">Parameter of the target method.</param>
        /// <returns>Argument corresponding to the parameter.</returns>
        IArgument GetArgumentMatchingParameter(IParameterSymbol parameter);
    }

    /// <summary>
    /// Represents a C# or VB method invocation.
    /// </summary>
    public interface IInvocationExpression : IHasArgumentsExpression
    {
        /// <summary>
        /// Method to be invoked.
        /// </summary>
        IMethodSymbol TargetMethod { get; }
        /// <summary>
        /// 'This' or 'Me' instance to be supplied to the method, or null if the method is static.
        /// </summary>
        IOperation Instance { get; }
        /// <summary>
        /// True if the invocation uses a virtual mechanism, and false otherwise.
        /// </summary>
        bool IsVirtual { get; }
        /// <summary>
        /// Arguments of the invocation, excluding the instance argument. Arguments are in the order specified in source,
        /// and params/ParamArray arguments have been collected into arrays. Arguments are not present
        /// unless supplied in source.
        /// </summary>
        ImmutableArray<IArgument> ArgumentsInSourceOrder { get; }
    }

    /// <summary>
    /// Represents an argument in a method invocation.
    /// </summary>
    public interface IArgument : IOperation
    {
        /// <summary>
        /// Kind of argument.
        /// </summary>
        ArgumentKind ArgumentKind { get; }
        /// <summary>
        /// Parameter the argument matches.
        /// </summary>
        IParameterSymbol Parameter { get; }
        /// <summary>
        /// Value supplied for the argument.
        /// </summary>
        IOperation Value { get; }
        /// <summary>
        /// Conversion applied to the argument value passing it into the target method. Applicable only to VB Reference arguments.
        /// </summary>
        IOperation InConversion { get; }
        /// <summary>
        /// Conversion applied to the argument value after the invocation. Applicable only to VB Reference arguments.
        /// </summary>
        IOperation OutConversion { get; }
    }

    /// <summary>
    /// Kinds of arguments.
    /// </summary>
    public enum ArgumentKind
    {
        None = 0x0,

        /// <summary>
        /// Argument is specified positionally and matches the parameter of the same ordinality.
        /// </summary>
        Positional = 0x1,
        /// <summary>
        /// Argument is specified by name and matches the parameter of the same name.
        /// </summary>
        Named = 0x2,
        /// <summary>
        /// Argument becomes an element of an array that matches a trailing C# params or VB ParamArray parameter.
        /// </summary>
        ParamArray = 0x3,
        /// <summary>
        /// Argument was omitted in source but has a default value supplied automatically.
        /// </summary>
        DefaultValue = 0x4
    }

    /// <summary>
    /// Represents a reference, which refers to a symbol or an element of a collection.
    /// </summary>
    public interface IReferenceExpression : IOperation
    {
    }

    /// <summary>
    /// Represents a reference to an array element.
    /// </summary>
    public interface IArrayElementReferenceExpression : IReferenceExpression
    {
        /// <summary>
        /// Array to be indexed.
        /// </summary>
        IOperation ArrayReference { get; }
        /// <summary>
        /// Indices that specify an individual element.
        /// </summary>
        ImmutableArray<IOperation> Indices { get; }
    }

    /// <summary>
    /// Represents a reference through a pointer.
    /// </summary>
    public interface IPointerIndirectionReferenceExpression : IReferenceExpression
    {
        /// <summary>
        /// Pointer to be dereferenced.
        /// </summary>
        IOperation Pointer { get; }
    }

    /// <summary>
    /// Represents a reference to a declared local variable.
    /// </summary>
    public interface ILocalReferenceExpression : IReferenceExpression
    {
        /// <summary>
        /// Referenced local variable.
        /// </summary>
        ILocalSymbol Local { get; }
    }

    /// <summary>
    /// Represents a reference to a parameter.
    /// </summary>
    public interface IParameterReferenceExpression : IReferenceExpression
    {
        /// <summary>
        /// Referenced parameter.
        /// </summary>
        IParameterSymbol Parameter { get; }
    }

    /// <summary>
    /// Represents a reference to a local variable synthesized by language analysis.
    /// </summary>
    public interface ISyntheticLocalReferenceExpression : IReferenceExpression
    {
        /// <summary>
        /// Kind of the synthetic local.
        /// </summary>
        SyntheticLocalKind SyntheticLocalKind { get; }
        /// <summary>
        /// Statement defining the lifetime of the synthetic local.
        /// </summary>
        IOperation ContainingStatement { get; }
    }

    /// <summary>
    /// Kinds of synthetic local references.
    /// </summary>
    public enum SyntheticLocalKind
    {
        None = 0x0,

        /// <summary>
        /// Created to capture the step value of a VB for loop.
        /// </summary>
        ForLoopStepValue = 0x1,
        /// <summary>
        /// Created to capture the limit value of a VB for loop.
        /// </summary>
        ForLoopLimitValue = 0x2
    }

    /// <summary>
    /// Represents a C# this or base expression, or a VB Me, MyClass, or MyBase expression.
    /// </summary>
    public interface IInstanceReferenceExpression : IOperation
    {
        ///
        /// <summary>
        /// Kind of instance reference.
        /// </summary>
        InstanceReferenceKind InstanceReferenceKind { get; }
    }

    public enum InstanceReferenceKind
    {
        None = 0x0,
        /// <summary>Indicates an implicit this or Me expression.</summary>
        Implicit = 0x1,
        /// <summary>Indicates an explicit this or Me expression.</summary>
        Explicit = 0x2,
        /// <summary>Indicates an explicit base or MyBase expression.</summary>
        BaseClass = 0x3,
        /// <summary>Indicates an explicit MyClass expression.</summary>
        ThisClass = 0x4
    }
    
    /// <summary>
    /// Represents a reference to a member of a class, struct, or interface.
    /// </summary>
    public interface IMemberReferenceExpression : IReferenceExpression
    {
        /// <summary>
        /// Instance of the type. Null if the reference is to a static/shared member.
        /// </summary>
        IOperation Instance { get; }

        /// <summary>
        /// Referenced member.  
        /// </summary>  
        ISymbol Member { get; }
    }

    /// <summary>
    /// Represents a reference to a field.
    /// </summary>
    public interface IFieldReferenceExpression : IMemberReferenceExpression
    {
        /// <summary>
        /// Referenced field.
        /// </summary>
        IFieldSymbol Field { get; }
    }

    /// <summary>
    /// Represents a reference to a method other than as the target of an invocation.
    /// </summary>
    public interface IMethodBindingExpression : IMemberReferenceExpression
    {
        /// <summary>
        /// Referenced method.
        /// </summary>
        IMethodSymbol Method { get; }

        /// <summary>
        /// Indicates whether the reference uses virtual semantics.
        /// </summary>
        bool IsVirtual { get; }
    }

    /// <summary>
    /// Represents a reference to a property.
    /// </summary>
    public interface IPropertyReferenceExpression : IMemberReferenceExpression
    {
        /// <summary>
        /// Referenced property.
        /// </summary>
        IPropertySymbol Property { get; }
    }

    /// <summary>
    /// Represents a reference to an indexed property.
    /// </summary>
    public interface IIndexedPropertyReferenceExpression : IPropertyReferenceExpression, IHasArgumentsExpression
    {
    }

    /// <summary>
    /// Represents a reference to an event.
    /// </summary>
    public interface IEventReferenceExpression : IMemberReferenceExpression
    {
        /// <summary>
        /// Referenced event.
        /// </summary>
        IEventSymbol Event { get; }
    }

    /// <summary>
    /// Represents a binding of an event.
    /// </summary>
    public interface IEventAssignmentExpression : IOperation
    {
        /// <summary>
        /// Event being bound.
        /// </summary>
        IEventSymbol Event { get; }

        /// <summary>
        /// Instance used to refer to the event being bound.
        /// </summary>
        IOperation EventInstance { get; }

        /// <summary>
        /// Handler supplied for the event.
        /// </summary>
        IOperation HandlerValue { get; }

        /// <summary>
        /// True for adding a binding, false for removing one.
        /// </summary>
        bool Adds { get; }
    }

    /// <summary>
    /// Represents an expression that includes a ? or ?. conditional access instance expression.
    /// </summary>
    public interface IConditionalAccessExpression : IOperation
    {
        /// <summary>
        /// Expression to be evaluated if the conditional instance is non null.
        /// </summary>
        IOperation ConditionalValue { get; }
        /// <summary>
        /// Expresson that is conditionally accessed.
        /// </summary>
        IOperation ConditionalInstance { get; }
    }

    /// <summary>
    /// Represents the value of a conditionally-accessed expression within an expression containing a conditional access.
    /// </summary>
    public interface IConditionalAccessInstanceExpression : IOperation
    {
    }

    /// <summary>
    /// Represents a general placeholder when no more specific kind of placeholder is available.
    /// A placeholder is an expression whose meaning is inferred from context.
    /// </summary>
    public interface IPlaceholderExpression : IOperation
    {
    }

    /// <summary>
    /// Represents a unary, binary, relational, or conversion operation that can use an operator method.
    /// </summary>
    public interface IHasOperatorMethodExpression : IOperation
    {
        /// <summary>
        /// True if and only if the operation is performed by an operator method.
        /// </summary>
        bool UsesOperatorMethod { get; }
        /// <summary>
        /// Operation method used by the operation, null if the operation does not use an operator method.
        /// </summary>
        IMethodSymbol OperatorMethod { get; }
    }

    /// <summary>
    /// Represents an operation with one operand.
    /// </summary>
    public interface IUnaryOperatorExpression : IHasOperatorMethodExpression
    {
        /// <summary>
        /// Kind of unary operation.
        /// </summary>
        UnaryOperationKind UnaryOperationKind { get; }
        /// <summary>
        /// Single operand.
        /// </summary>
        IOperation Operand { get; }
    }

    public enum SimpleUnaryOperationKind
    {
        None = 0x0,

        BitwiseNegation = 0x1,
        LogicalNot = 0x2,
        PostfixIncrement = 0x3,
        PostfixDecrement = 0x4,
        PrefixIncrement = 0x5,
        PrefixDecrement = 0x6,
        Plus = 0x7,
        Minus = 0x8,
        True = 0x9,
        False = 0xa,
        BitwiseOrLogicalNot = 0xb
    }

    public enum UnaryOperandKind
    {
        None = 0x0,

        OperatorMethod = 0x100,
        Integer = 0x200,
        Unsigned = 0x300,
        Floating = 0x400,
        Decimal = 0x500,
        Boolean = 0x600,
        Enum = 0x700,
        Dynamic = 0x800,
        Object = 0x900,
        Pointer = 0xa00
    }

    /// <summary>
    /// Kinds of unary operations.
    /// </summary>
    public enum UnaryOperationKind
    {
        None = 0x0,

        OperatorMethodBitwiseNegation = UnaryOperandKind.OperatorMethod | SimpleUnaryOperationKind.BitwiseNegation,
        OperatorMethodLogicalNot = UnaryOperandKind.OperatorMethod | SimpleUnaryOperationKind.LogicalNot,
        OperatorMethodPostfixIncrement = UnaryOperandKind.OperatorMethod | SimpleUnaryOperationKind.PostfixIncrement,
        OperatorMethodPostfixDecrement = UnaryOperandKind.OperatorMethod | SimpleUnaryOperationKind.PostfixDecrement,
        OperatorMethodPrefixIncrement = UnaryOperandKind.OperatorMethod | SimpleUnaryOperationKind.PrefixIncrement,
        OperatorMethodPrefixDecrement = UnaryOperandKind.OperatorMethod | SimpleUnaryOperationKind.PrefixDecrement,
        OperatorMethodPlus = UnaryOperandKind.OperatorMethod | SimpleUnaryOperationKind.Plus,
        OperatorMethodMinus = UnaryOperandKind.OperatorMethod | SimpleUnaryOperationKind.Minus,
        OperatorMethodTrue = UnaryOperandKind.OperatorMethod | SimpleUnaryOperationKind.True,
        OperatorMethodFalse = UnaryOperandKind.OperatorMethod | SimpleUnaryOperationKind.False,

        IntegerBitwiseNegation = UnaryOperandKind.Integer | SimpleUnaryOperationKind.BitwiseNegation,
        IntegerPlus = UnaryOperandKind.Integer | SimpleUnaryOperationKind.Plus,
        IntegerMinus = UnaryOperandKind.Integer | SimpleUnaryOperationKind.Minus,
        IntegerPostfixIncrement = UnaryOperandKind.Integer | SimpleUnaryOperationKind.PostfixIncrement,
        IntegerPostfixDecrement = UnaryOperandKind.Integer | SimpleUnaryOperationKind.PostfixDecrement,
        IntegerPrefixIncrement = UnaryOperandKind.Integer | SimpleUnaryOperationKind.PrefixIncrement,
        IntegerPrefixDecrement = UnaryOperandKind.Integer | SimpleUnaryOperationKind.PrefixDecrement,

        UnsignedPostfixIncrement = UnaryOperandKind.Unsigned | SimpleUnaryOperationKind.PostfixIncrement,
        UnsignedPostfixDecrement = UnaryOperandKind.Unsigned | SimpleUnaryOperationKind.PostfixDecrement,
        UnsignedPrefixIncrement = UnaryOperandKind.Unsigned | SimpleUnaryOperationKind.PrefixIncrement,
        UnsignedPrefixDecrement = UnaryOperandKind.Unsigned | SimpleUnaryOperationKind.PrefixDecrement,

        FloatingPlus = UnaryOperandKind.Floating | SimpleUnaryOperationKind.Plus,
        FloatingMinus = UnaryOperandKind.Floating | SimpleUnaryOperationKind.Minus,
        FloatingPostfixIncrement = UnaryOperandKind.Floating | SimpleUnaryOperationKind.PostfixIncrement,
        FloatingPostfixDecrement = UnaryOperandKind.Floating | SimpleUnaryOperationKind.PostfixDecrement,
        FloatingPrefixIncrement = UnaryOperandKind.Floating | SimpleUnaryOperationKind.PrefixIncrement,
        FloatingPrefixDecrement = UnaryOperandKind.Floating | SimpleUnaryOperationKind.PrefixDecrement,

        DecimalPlus = UnaryOperandKind.Decimal | SimpleUnaryOperationKind.Plus,
        DecimalMinus = UnaryOperandKind.Decimal | SimpleUnaryOperationKind.Minus,
        DecimalPostfixIncrement = UnaryOperandKind.Decimal | SimpleUnaryOperationKind.PostfixIncrement,
        DecimalPostfixDecrement = UnaryOperandKind.Decimal | SimpleUnaryOperationKind.PostfixDecrement,
        DecimalPrefixIncrement = UnaryOperandKind.Decimal | SimpleUnaryOperationKind.PrefixIncrement,
        DecimalPrefixDecrement = UnaryOperandKind.Decimal | SimpleUnaryOperationKind.PrefixDecrement,

        BooleanBitwiseNegation = UnaryOperandKind.Boolean | SimpleUnaryOperationKind.BitwiseNegation,
        BooleanLogicalNot = UnaryOperandKind.Boolean | SimpleUnaryOperationKind.LogicalNot,

        EnumPostfixIncrement = UnaryOperandKind.Enum | SimpleUnaryOperationKind.PostfixIncrement,
        EnumPostfixDecrement = UnaryOperandKind.Enum | SimpleUnaryOperationKind.PostfixDecrement,
        EnumPrefixIncrement = UnaryOperandKind.Enum | SimpleUnaryOperationKind.PrefixIncrement,
        EnumPrefixDecrement = UnaryOperandKind.Enum | SimpleUnaryOperationKind.PrefixDecrement,

        PointerPostfixIncrement = UnaryOperandKind.Pointer | SimpleUnaryOperationKind.PostfixIncrement,
        PointerPostfixDecrement = UnaryOperandKind.Pointer | SimpleUnaryOperationKind.PostfixDecrement,
        PointerPrefixIncrement = UnaryOperandKind.Pointer | SimpleUnaryOperationKind.PrefixIncrement,
        PointerPrefixDecrement = UnaryOperandKind.Pointer | SimpleUnaryOperationKind.PrefixDecrement,

        DynamicBitwiseNegation = UnaryOperandKind.Dynamic | SimpleUnaryOperationKind.BitwiseNegation,
        DynamicLogicalNot = UnaryOperandKind.Dynamic | SimpleUnaryOperationKind.LogicalNot,
        DynamicTrue = UnaryOperandKind.Dynamic | SimpleUnaryOperationKind.True,
        DynamicFalse = UnaryOperandKind.Dynamic | SimpleUnaryOperationKind.False,
        DynamicPlus = UnaryOperandKind.Dynamic | SimpleUnaryOperationKind.Plus,
        DynamicMinus = UnaryOperandKind.Dynamic | SimpleUnaryOperationKind.Minus,
        DynamicPostfixIncrement = UnaryOperandKind.Dynamic | SimpleUnaryOperationKind.PostfixIncrement,
        DynamicPostfixDecrement = UnaryOperandKind.Dynamic | SimpleUnaryOperationKind.PostfixDecrement,
        DynamicPrefixIncrement = UnaryOperandKind.Dynamic | SimpleUnaryOperationKind.PrefixIncrement,
        DynamicPrefixDecrement = UnaryOperandKind.Dynamic | SimpleUnaryOperationKind.PrefixDecrement,

        ObjectPlus = UnaryOperandKind.Object | SimpleUnaryOperationKind.Plus,
        ObjectMinus = UnaryOperandKind.Object | SimpleUnaryOperationKind.Minus,
        ObjectNot = UnaryOperandKind.Object | SimpleUnaryOperationKind.BitwiseOrLogicalNot
    }

    
    /// <summary>
    /// Represents an operation with two operands that produces a result with the same type as at least one of the operands.
    /// </summary>
    public interface IBinaryOperatorExpression : IHasOperatorMethodExpression
    {
        /// <summary>
        /// Kind of binary operation.
        /// </summary>
        BinaryOperationKind BinaryOperationKind { get; }
        /// <summary>
        /// Left operand.
        /// </summary>
        IOperation Left { get; }
        /// <summary>
        /// Right operand.
        /// </summary>
        IOperation Right { get; }
    }

    public enum SimpleBinaryOperationKind
    {
        None = 0x0,

        Add = 0x1,
        Subtract = 0x2,
        Multiply = 0x3,
        Divide = 0x4,
        IntegerDivide = 0x5,
        Remainder = 0x6,
        Power = 0x7,
        LeftShift = 0x8,
        RightShift = 0x9,
        And = 0xa,
        Or = 0xb,
        ExclusiveOr = 0xc,
        ConditionalAnd = 0xd,
        ConditionalOr = 0xe,
        Concatenate = 0xf,

        // Relational operations.

        Equals = 0x10,
        ObjectValueEquals = 0x11,
        NotEquals = 0x12,
        ObjectValueNotEquals = 0x13,
        LessThan = 0x14,
        LessThanOrEqual = 0x15,
        GreaterThanOrEqual = 0x16,
        GreaterThan = 0x17,

        Like = 0x18
    }

    public enum BinaryOperandsKind
    {
        None = 0x0,

        OperatorMethod = 0x100,
        Integer = 0x200,
        Unsigned = 0x300,
        Floating = 0x400,
        Decimal = 0x500,
        Boolean = 0x600,
        Enum = 0x700,
        Dynamic = 0x800,
        Object = 0x900,
        Pointer = 0xa00,
        PointerInteger = 0xb00,
        IntegerPointer = 0xc00,
        String = 0xd00,
        Delegate = 0xe00,
        Nullable = 0xf00
    }

    /// <summary>
    /// Kinds of binary operations.
    /// </summary>
    public enum BinaryOperationKind
    {
        None = 0x0,

        OperatorMethodAdd = BinaryOperandsKind.OperatorMethod | SimpleBinaryOperationKind.Add,
        OperatorMethodSubtract = BinaryOperandsKind.OperatorMethod | SimpleBinaryOperationKind.Subtract,
        OperatorMethodMultiply = BinaryOperandsKind.OperatorMethod | SimpleBinaryOperationKind.Multiply,
        OperatorMethodDivide = BinaryOperandsKind.OperatorMethod | SimpleBinaryOperationKind.Divide,
        OperatorMethodRemainder = BinaryOperandsKind.OperatorMethod | SimpleBinaryOperationKind.Remainder,
        OperatorMethodLeftShift = BinaryOperandsKind.OperatorMethod | SimpleBinaryOperationKind.LeftShift,
        OperatorMethodRightShift = BinaryOperandsKind.OperatorMethod | SimpleBinaryOperationKind.RightShift,
        OperatorMethodAnd = BinaryOperandsKind.OperatorMethod | SimpleBinaryOperationKind.And,
        OperatorMethodOr = BinaryOperandsKind.OperatorMethod | SimpleBinaryOperationKind.Or,
        OperatorMethodExclusiveOr = BinaryOperandsKind.OperatorMethod | SimpleBinaryOperationKind.ExclusiveOr,
        OperatorMethodConditionalAnd = BinaryOperandsKind.OperatorMethod | SimpleBinaryOperationKind.ConditionalAnd,
        OperatorMethodConditionalOr = BinaryOperandsKind.OperatorMethod | SimpleBinaryOperationKind.ConditionalOr,

        IntegerAdd = BinaryOperandsKind.Integer | SimpleBinaryOperationKind.Add,
        IntegerSubtract = BinaryOperandsKind.Integer | SimpleBinaryOperationKind.Subtract,
        IntegerMultiply = BinaryOperandsKind.Integer | SimpleBinaryOperationKind.Multiply,
        IntegerDivide = BinaryOperandsKind.Integer | SimpleBinaryOperationKind.Divide,
        IntegerRemainder = BinaryOperandsKind.Integer | SimpleBinaryOperationKind.Remainder,
        IntegerLeftShift = BinaryOperandsKind.Integer | SimpleBinaryOperationKind.LeftShift,
        IntegerRightShift = BinaryOperandsKind.Integer | SimpleBinaryOperationKind.RightShift,
        IntegerAnd = BinaryOperandsKind.Integer | SimpleBinaryOperationKind.And,
        IntegerOr = BinaryOperandsKind.Integer | SimpleBinaryOperationKind.Or,
        IntegerExclusiveOr = BinaryOperandsKind.Integer | SimpleBinaryOperationKind.ExclusiveOr,

        UnsignedAdd = BinaryOperandsKind.Unsigned | SimpleBinaryOperationKind.Add,
        UnsignedSubtract = BinaryOperandsKind.Unsigned | SimpleBinaryOperationKind.Subtract,
        UnsignedMultiply = BinaryOperandsKind.Unsigned | SimpleBinaryOperationKind.Multiply,
        UnsignedDivide = BinaryOperandsKind.Unsigned | SimpleBinaryOperationKind.Divide,
        UnsignedRemainder = BinaryOperandsKind.Unsigned | SimpleBinaryOperationKind.Remainder,
        UnsignedLeftShift = BinaryOperandsKind.Unsigned | SimpleBinaryOperationKind.LeftShift,
        UnsignedRightShift = BinaryOperandsKind.Unsigned | SimpleBinaryOperationKind.RightShift,
        UnsignedAnd = BinaryOperandsKind.Unsigned | SimpleBinaryOperationKind.And,
        UnsignedOr = BinaryOperandsKind.Unsigned | SimpleBinaryOperationKind.Or,
        UnsignedExclusiveOr = BinaryOperandsKind.Unsigned | SimpleBinaryOperationKind.ExclusiveOr,

        FloatingAdd = BinaryOperandsKind.Floating | SimpleBinaryOperationKind.Add,
        FloatingSubtract = BinaryOperandsKind.Floating | SimpleBinaryOperationKind.Subtract,
        FloatingMultiply = BinaryOperandsKind.Floating | SimpleBinaryOperationKind.Multiply,
        FloatingDivide = BinaryOperandsKind.Floating | SimpleBinaryOperationKind.Divide,
        FloatingRemainder = BinaryOperandsKind.Floating | SimpleBinaryOperationKind.Remainder,
        FloatingPower = BinaryOperandsKind.Floating | SimpleBinaryOperationKind.Power,

        DecimalAdd = BinaryOperandsKind.Decimal | SimpleBinaryOperationKind.Add,
        DecimalSubtract = BinaryOperandsKind.Decimal | SimpleBinaryOperationKind.Subtract,
        DecimalMultiply = BinaryOperandsKind.Decimal | SimpleBinaryOperationKind.Multiply,
        DecimalDivide = BinaryOperandsKind.Decimal | SimpleBinaryOperationKind.Divide,

        BooleanAnd = BinaryOperandsKind.Boolean | SimpleBinaryOperationKind.And,
        BooleanOr = BinaryOperandsKind.Boolean | SimpleBinaryOperationKind.Or,
        BooleanExclusiveOr = BinaryOperandsKind.Boolean | SimpleBinaryOperationKind.ExclusiveOr,
        BooleanConditionalAnd = BinaryOperandsKind.Boolean | SimpleBinaryOperationKind.ConditionalAnd,
        BooleanConditionalOr = BinaryOperandsKind.Boolean | SimpleBinaryOperationKind.ConditionalOr,

        EnumAdd = BinaryOperandsKind.Enum | SimpleBinaryOperationKind.Add,
        EnumSubtract = BinaryOperandsKind.Enum | SimpleBinaryOperationKind.Subtract,
        EnumAnd = BinaryOperandsKind.Enum | SimpleBinaryOperationKind.And,
        EnumOr = BinaryOperandsKind.Enum | SimpleBinaryOperationKind.Or,
        EnumExclusiveOr = BinaryOperandsKind.Enum | SimpleBinaryOperationKind.ExclusiveOr,

        PointerIntegerAdd = BinaryOperandsKind.PointerInteger | SimpleBinaryOperationKind.Add,
        IntegerPointerAdd = BinaryOperandsKind.IntegerPointer | SimpleBinaryOperationKind.Add,
        PointerIntegerSubtract = BinaryOperandsKind.PointerInteger | SimpleBinaryOperationKind.Subtract,
        PointerSubtract = BinaryOperandsKind.Pointer | SimpleBinaryOperationKind.Subtract,

        DynamicAdd = BinaryOperandsKind.Dynamic | SimpleBinaryOperationKind.Add,
        DynamicSubtract = BinaryOperandsKind.Dynamic | SimpleBinaryOperationKind.Subtract,
        DynamicMultiply = BinaryOperandsKind.Dynamic | SimpleBinaryOperationKind.Multiply,
        DynamicDivide = BinaryOperandsKind.Dynamic | SimpleBinaryOperationKind.Divide,
        DynamicRemainder = BinaryOperandsKind.Dynamic | SimpleBinaryOperationKind.Remainder,
        DynamicLeftShift = BinaryOperandsKind.Dynamic | SimpleBinaryOperationKind.LeftShift,
        DynamicRightShift = BinaryOperandsKind.Dynamic | SimpleBinaryOperationKind.RightShift,
        DynamicAnd = BinaryOperandsKind.Dynamic | SimpleBinaryOperationKind.And,
        DynamicOr = BinaryOperandsKind.Dynamic | SimpleBinaryOperationKind.Or,
        DynamicExclusiveOr = BinaryOperandsKind.Dynamic | SimpleBinaryOperationKind.ExclusiveOr,

        ObjectAdd = BinaryOperandsKind.Object | SimpleBinaryOperationKind.Add,
        ObjectSubtract = BinaryOperandsKind.Object | SimpleBinaryOperationKind.Subtract,
        ObjectMultiply = BinaryOperandsKind.Object | SimpleBinaryOperationKind.Multiply,
        ObjectDivide = BinaryOperandsKind.Object | SimpleBinaryOperationKind.Divide,
        ObjectIntegerDivide = BinaryOperandsKind.Object | SimpleBinaryOperationKind.IntegerDivide,
        ObjectRemainder = BinaryOperandsKind.Object | SimpleBinaryOperationKind.Remainder,
        ObjectPower = BinaryOperandsKind.Object | SimpleBinaryOperationKind.Power,
        ObjectLeftShift = BinaryOperandsKind.Object | SimpleBinaryOperationKind.LeftShift,
        ObjectRightShift = BinaryOperandsKind.Object | SimpleBinaryOperationKind.RightShift,
        ObjectAnd = BinaryOperandsKind.Object | SimpleBinaryOperationKind.And,
        ObjectOr = BinaryOperandsKind.Object | SimpleBinaryOperationKind.Or,
        ObjectExclusiveOr = BinaryOperandsKind.Object | SimpleBinaryOperationKind.ExclusiveOr,
        ObjectConditionalAnd = BinaryOperandsKind.Object | SimpleBinaryOperationKind.ConditionalAnd,
        ObjectConditionalOr = BinaryOperandsKind.Object | SimpleBinaryOperationKind.ConditionalOr,
        ObjectConcatenate = BinaryOperandsKind.Object | SimpleBinaryOperationKind.Concatenate,

        StringConcatenate = BinaryOperandsKind.String | SimpleBinaryOperationKind.Concatenate,

        // Relational operations.

        OperatorMethodEquals = BinaryOperandsKind.OperatorMethod | SimpleBinaryOperationKind.Equals,
        OperatorMethodNotEquals = BinaryOperandsKind.OperatorMethod | SimpleBinaryOperationKind.NotEquals,
        OperatorMethodLessThan = BinaryOperandsKind.OperatorMethod | SimpleBinaryOperationKind.LessThan,
        OperatorMethodLessThanOrEqual = BinaryOperandsKind.OperatorMethod | SimpleBinaryOperationKind.LessThanOrEqual,
        OperatorMethodGreaterThanOrEqual = BinaryOperandsKind.OperatorMethod | SimpleBinaryOperationKind.GreaterThanOrEqual,
        OperatorMethodGreaterThan = BinaryOperandsKind.OperatorMethod | SimpleBinaryOperationKind.GreaterThan,

        IntegerEquals = BinaryOperandsKind.Integer | SimpleBinaryOperationKind.Equals,
        IntegerNotEquals = BinaryOperandsKind.Integer | SimpleBinaryOperationKind.NotEquals,
        IntegerLessThan = BinaryOperandsKind.Integer | SimpleBinaryOperationKind.LessThan,
        IntegerLessThanOrEqual = BinaryOperandsKind.Integer | SimpleBinaryOperationKind.LessThanOrEqual,
        IntegerGreaterThanOrEqual = BinaryOperandsKind.Integer | SimpleBinaryOperationKind.GreaterThanOrEqual,
        IntegerGreaterThan = BinaryOperandsKind.Integer | SimpleBinaryOperationKind.GreaterThan,

        UnsignedLessThan = BinaryOperandsKind.Unsigned | SimpleBinaryOperationKind.LessThan,
        UnsignedLessThanOrEqual = BinaryOperandsKind.Unsigned | SimpleBinaryOperationKind.LessThanOrEqual,
        UnsignedGreaterThanOrEqual = BinaryOperandsKind.Unsigned | SimpleBinaryOperationKind.GreaterThanOrEqual,
        UnsignedGreaterThan = BinaryOperandsKind.Unsigned | SimpleBinaryOperationKind.GreaterThan,

        FloatingEquals = BinaryOperandsKind.Floating | SimpleBinaryOperationKind.Equals,
        FloatingNotEquals = BinaryOperandsKind.Floating | SimpleBinaryOperationKind.NotEquals,
        FloatingLessThan = BinaryOperandsKind.Floating | SimpleBinaryOperationKind.LessThan,
        FloatingLessThanOrEqual = BinaryOperandsKind.Floating | SimpleBinaryOperationKind.LessThanOrEqual,
        FloatingGreaterThanOrEqual = BinaryOperandsKind.Floating | SimpleBinaryOperationKind.GreaterThanOrEqual,
        FloatingGreaterThan = BinaryOperandsKind.Floating | SimpleBinaryOperationKind.GreaterThan,

        DecimalEquals = BinaryOperandsKind.Decimal | SimpleBinaryOperationKind.Equals,
        DecimalNotEquals = BinaryOperandsKind.Decimal | SimpleBinaryOperationKind.NotEquals,
        DecimalLessThan = BinaryOperandsKind.Decimal | SimpleBinaryOperationKind.LessThan,
        DecimalLessThanOrEqual = BinaryOperandsKind.Decimal | SimpleBinaryOperationKind.LessThanOrEqual,
        DecimalGreaterThanOrEqual = BinaryOperandsKind.Decimal | SimpleBinaryOperationKind.GreaterThanOrEqual,
        DecimalGreaterThan = BinaryOperandsKind.Decimal | SimpleBinaryOperationKind.GreaterThan,

        BooleanEquals = BinaryOperandsKind.Boolean | SimpleBinaryOperationKind.Equals,
        BooleanNotEquals = BinaryOperandsKind.Boolean | SimpleBinaryOperationKind.NotEquals,

        StringEquals = BinaryOperandsKind.String | SimpleBinaryOperationKind.Equals,
        StringNotEquals = BinaryOperandsKind.String | SimpleBinaryOperationKind.NotEquals,
        StringLike = BinaryOperandsKind.String | SimpleBinaryOperationKind.Like,

        DelegateEquals = BinaryOperandsKind.Delegate | SimpleBinaryOperationKind.Equals,
        DelegateNotEquals = BinaryOperandsKind.Delegate | SimpleBinaryOperationKind.NotEquals,

        NullableEquals = BinaryOperandsKind.Nullable | SimpleBinaryOperationKind.Equals,
        NullableNotEquals = BinaryOperandsKind.Nullable | SimpleBinaryOperationKind.NotEquals,

        ObjectEquals = BinaryOperandsKind.Object | SimpleBinaryOperationKind.Equals,
        ObjectNotEquals = BinaryOperandsKind.Object | SimpleBinaryOperationKind.NotEquals,
        ObjectVBEquals = BinaryOperandsKind.Object | SimpleBinaryOperationKind.ObjectValueEquals,
        ObjectVBNotEquals = BinaryOperandsKind.Object | SimpleBinaryOperationKind.ObjectValueNotEquals,
        ObjectLike = BinaryOperandsKind.Object | SimpleBinaryOperationKind.Like,
        ObjectLessThan = BinaryOperandsKind.Object | SimpleBinaryOperationKind.LessThan,
        ObjectLessThanOrEqual = BinaryOperandsKind.Object | SimpleBinaryOperationKind.LessThanOrEqual,
        ObjectGreaterThanOrEqual = BinaryOperandsKind.Object | SimpleBinaryOperationKind.GreaterThanOrEqual,
        ObjectGreaterThan = BinaryOperandsKind.Object | SimpleBinaryOperationKind.GreaterThan,

        EnumEquals = BinaryOperandsKind.Enum | SimpleBinaryOperationKind.Equals,
        EnumNotEquals = BinaryOperandsKind.Enum | SimpleBinaryOperationKind.NotEquals,
        EnumLessThan = BinaryOperandsKind.Enum | SimpleBinaryOperationKind.LessThan,
        EnumLessThanOrEqual = BinaryOperandsKind.Enum | SimpleBinaryOperationKind.LessThanOrEqual,
        EnumGreaterThanOrEqual = BinaryOperandsKind.Enum | SimpleBinaryOperationKind.GreaterThanOrEqual,
        EnumGreaterThan = BinaryOperandsKind.Enum | SimpleBinaryOperationKind.GreaterThan,

        PointerEquals = BinaryOperandsKind.Pointer | SimpleBinaryOperationKind.Equals,
        PointerNotEquals = BinaryOperandsKind.Pointer | SimpleBinaryOperationKind.NotEquals,
        PointerLessThan = BinaryOperandsKind.Pointer | SimpleBinaryOperationKind.LessThan,
        PointerLessThanOrEqual = BinaryOperandsKind.Pointer | SimpleBinaryOperationKind.LessThanOrEqual,
        PointerGreaterThanOrEqual = BinaryOperandsKind.Pointer | SimpleBinaryOperationKind.GreaterThanOrEqual,
        PointerGreaterThan = BinaryOperandsKind.Pointer | SimpleBinaryOperationKind.GreaterThan,

        DynamicEquals = BinaryOperandsKind.Dynamic | SimpleBinaryOperationKind.Equals,
        DynamicNotEquals = BinaryOperandsKind.Dynamic | SimpleBinaryOperationKind.NotEquals,
        DynamicLessThan = BinaryOperandsKind.Dynamic | SimpleBinaryOperationKind.LessThan,
        DynamicLessThanOrEqual = BinaryOperandsKind.Dynamic | SimpleBinaryOperationKind.LessThanOrEqual,
        DynamicGreaterThanOrEqual = BinaryOperandsKind.Dynamic | SimpleBinaryOperationKind.GreaterThanOrEqual,
        DynamicGreaterThan = BinaryOperandsKind.Dynamic | SimpleBinaryOperationKind.GreaterThan
    }

    public static class UnaryAndBinaryOperationExtensions
    {
        const int SimpleUnaryOperationKindMask = 0xff;
        const int UnaryOperandKindMask = 0xff00;
        const int SimpleBinaryOperationKindMask = 0xff;
        const int BinaryOperandsKindMask = 0xff00;

        /// <summary>
        /// Get unary operation kind independent of data type.
        /// </summary>
        public static SimpleUnaryOperationKind GetSimpleUnaryOperationKind(this IUnaryOperatorExpression unary)
        {
            return GetSimpleUnaryOperationKind(unary.UnaryOperationKind);
        }

        /// <summary>
        /// Get unary operation kind independent of data type.
        /// </summary>
        public static SimpleUnaryOperationKind GetSimpleUnaryOperationKind(this IIncrementExpression increment)
        {
            return GetSimpleUnaryOperationKind(increment.IncrementOperationKind);
        }

        /// <summary>
        /// Get unary operand kind.
        /// </summary>
        public static UnaryOperandKind GetUnaryOperandKind(this IUnaryOperatorExpression unary)
        {
            return GetUnaryOperandKind(unary.UnaryOperationKind);
        }

        /// <summary>
        /// Get unary operand kind.
        /// </summary>
        public static UnaryOperandKind GetUnaryOperandKind(this IIncrementExpression increment)
        {
            return GetUnaryOperandKind(increment.IncrementOperationKind);
        }

        /// <summary>
        /// Get binary operation kind independent of data type.
        /// </summary>
        public static SimpleBinaryOperationKind GetSimpleBinaryOperationKind(this IBinaryOperatorExpression binary)
        {
            return GetSimpleBinaryOperationKind(binary.BinaryOperationKind);
        }

        /// <summary>
        /// Get binary operation kind independent of data type.
        /// </summary>
        public static SimpleBinaryOperationKind GetSimpleBinaryOperationKind(this ICompoundAssignmentExpression compoundAssignment)
        {
            return GetSimpleBinaryOperationKind(compoundAssignment.BinaryOperationKind);
        }

        /// <summary>
        /// Get binary operand kinds.
        /// </summary>
        public static BinaryOperandsKind GetBinaryOperandsKind(this IBinaryOperatorExpression binary)
        {
            return GetBinaryOperandsKind(binary.BinaryOperationKind);
        }

        /// <summary>
        /// Get binary operand kinds.
        /// </summary>
        public static BinaryOperandsKind GetBinaryOperandsKind(this ICompoundAssignmentExpression compoundAssignment)
        {
            return GetBinaryOperandsKind(compoundAssignment.BinaryOperationKind);
        }

        public static SimpleUnaryOperationKind GetSimpleUnaryOperationKind(UnaryOperationKind kind)
        {
            return (SimpleUnaryOperationKind)((int)kind & UnaryAndBinaryOperationExtensions.SimpleUnaryOperationKindMask);
        }

        public static UnaryOperandKind GetUnaryOperandKind(UnaryOperationKind kind)
        {
            return (UnaryOperandKind)((int)kind & UnaryAndBinaryOperationExtensions.UnaryOperandKindMask);
        }

        public static SimpleBinaryOperationKind GetSimpleBinaryOperationKind(BinaryOperationKind kind)
        {
            return (SimpleBinaryOperationKind)((int)kind & UnaryAndBinaryOperationExtensions.SimpleBinaryOperationKindMask);
        }

        public static BinaryOperandsKind GetBinaryOperandsKind(BinaryOperationKind kind)
        {
            return (BinaryOperandsKind)((int)kind & UnaryAndBinaryOperationExtensions.BinaryOperandsKindMask);
        }
    }

    /// <summary>
    /// Represents a conversion operation.
    /// </summary>
    public interface IConversionExpression : IHasOperatorMethodExpression
    {
        /// <summary>
        /// Value to be converted.
        /// </summary>
        IOperation Operand { get; }
        /// <summary>
        /// Kind of conversion.
        /// </summary>
        ConversionKind ConversionKind { get; }
        /// <summary>
        /// True if and only if the conversion is indicated explicity by a cast operation in the source code.
        /// </summary>
        bool IsExplicit { get; }
    }

    /// <summary>
    /// Kinds of conversions.
    /// </summary>
    public enum ConversionKind
    {
        None = 0x0,
        /// <summary>
        /// Conversion is defined by the underlying type system and throws an exception if it fails.
        /// </summary>
        Cast = 0x1,
        /// <summary>
        /// Conversion is defined by the underlying type system and produces a null result if it fails.
        /// </summary>
        TryCast = 0x2,
        /// <summary>
        /// Conversion has VB-specific semantics.
        /// </summary>
        Basic = 0x3,
        /// <summary>
        /// Conversion has C#-specific semantics.
        /// </summary>
        CSharp = 0x4,
        /// <summary>
        /// Conversion is implemented by a conversion operator method.
        /// </summary>
        OperatorMethod = 0x5
    }

    /// <summary>
    /// Represents a C# ?: or VB If expression.
    /// </summary>
    public interface IConditionalChoiceExpression : IOperation
    {
        /// <summary>
        /// Condition to be tested.
        /// </summary>
        IOperation Condition { get; }
        /// <summary>
        /// Value evaluated if the Condition is true.
        /// </summary>
        IOperation IfTrueValue { get; }
        /// <summary>
        /// Value evaluated if the Condition is false.
        /// </summary>
        IOperation IfFalseValue { get; }
    }

    /// <summary>
    /// Represents a null-coalescing expression.
    /// </summary>
    public interface INullCoalescingExpression : IOperation
    {
        /// <summary>
        /// Value to be unconditionally evaluated.
        /// </summary>
        IOperation Primary { get; }
        /// <summary>
        /// Value to be evaluated if Primary evaluates to null/Nothing.
        /// </summary>
        IOperation Secondary { get; }
    }

    /// <summary>
    /// Represents an expression that tests if a value is of a specific type.
    /// </summary>
    public interface IIsTypeExpression : IOperation
    {
        /// <summary>
        /// Value to test.
        /// </summary>
        IOperation Operand { get; }
        /// <summary>
        /// Type for which to test.
        /// </summary>
        ITypeSymbol IsType { get; }
    }

    /// <summary>
    /// Represents an expression operating on a type.
    /// </summary>
    public interface ITypeOperationExpression : IOperation
    {
        /// <summary>
        /// Type operand.
        /// </summary>
        ITypeSymbol TypeOperand { get; }
    }

    /// <summary>
    /// Represents a SizeOf expression.
    /// </summary>
    public interface ISizeOfExpression : ITypeOperationExpression
    {
    }

    /// <summary>
    /// Represents a TypeOf expression.
    /// </summary>
    public interface ITypeOfExpression : ITypeOperationExpression
    {
    }

    /// <summary>
    /// Represents a lambda expression.
    /// </summary>
    public interface ILambdaExpression : IOperation
    {
        /// <summary>
        /// Signature of the lambda.
        /// </summary>
        IMethodSymbol Signature { get; }
        /// <summary>
        /// Body of the lambda.
        /// </summary>
        IBlockStatement Body { get; }
    }

    /// <summary>
    /// Represents a textual literal numeric, string, etc. expression.
    /// </summary>
    public interface ILiteralExpression : IOperation
    {
        /// <summary>
        /// Textual representation of the literal.
        /// </summary>
        string Text { get; }
    }

    /// <summary>
    /// Represents an await expression.
    /// </summary>
    public interface IAwaitExpression : IOperation
    {
        /// <summary>
        /// Value to be awaited.
        /// </summary>
        IOperation AwaitedValue { get; }
    }

    /// <summary>
    /// Represents an expression that creates a pointer value by taking the address of a reference.
    /// </summary>
    public interface IAddressOfExpression : IOperation
    {
        /// <summary>
        /// Addressed reference.
        /// </summary>
        IReferenceExpression Reference { get; }
    }

    /// <summary>
    /// Represents a new/New expression.
    /// </summary>
    public interface IObjectCreationExpression : IHasArgumentsExpression
    {
        /// <summary>
        /// Constructor to be invoked on the created instance.
        /// </summary>
        IMethodSymbol Constructor { get; }
        /// <summary>
        /// Explicitly-specified member initializers.
        /// </summary>
        ImmutableArray<ISymbolInitializer> MemberInitializers { get; }
    }

    /// <summary>
    /// Represents an initializer for a field, property, or parameter.
    /// </summary>
    public interface ISymbolInitializer : IOperation
    {
        IOperation Value { get; }
    }

    /// <summary>
    /// Represents an initialization of a field.
    /// </summary>
    public interface IFieldInitializer : ISymbolInitializer
    {
        /// <summary>
        /// Initialized fields. There can be multiple fields for Visual Basic fields declared with As New.
        /// </summary>
        ImmutableArray<IFieldSymbol> InitializedFields { get; }
    }

    /// <summary>
    /// Represents an initialization of a property.
    /// </summary>
    public interface IPropertyInitializer : ISymbolInitializer
    {
        /// <summary>
        /// Set method used to initialize the property.
        /// </summary>
        IPropertySymbol InitializedProperty { get; }
    }

    /// <summary>
    /// Represents an initialization of a parameter at the point of declaration.
    /// </summary>
    public interface IParameterInitializer : ISymbolInitializer
    {
        /// <summary>
        /// Initialized parameter.
        /// </summary>
        IParameterSymbol Parameter { get; }
    }

    /// <summary>
    /// Represents the creation of an array instance.
    /// </summary>
    public interface IArrayCreationExpression : IOperation
    {
        /// <summary>
        /// Element type of the created array instance.
        /// </summary>
        ITypeSymbol ElementType { get; }
        /// <summary>
        /// Sizes of the dimensions of the created array instance.
        /// </summary>
        ImmutableArray<IOperation> DimensionSizes { get; }
        /// <summary>
        /// Values of elements of the created array instance.
        /// </summary>
        IArrayInitializer Initializer { get; }
    }

    /// <summary>
    /// Represents the initialization of an array instance.
    /// </summary>
    public interface IArrayInitializer : IOperation
    {
        /// <summary>
        /// Values to initialize array elements.
        /// </summary>
        ImmutableArray<IOperation> ElementValues { get; }
    }

    /// <summary>
    /// Represents an assignment expression.
    /// </summary>
    public interface IAssignmentExpression : IOperation
    {
        /// <summary>
        /// Target of the assignment.
        /// </summary>
        IReferenceExpression Target { get; }
        /// <summary>
        /// Value to be assigned to the target of the assignment.
        /// </summary>
        IOperation Value { get; }
    }

    /// <summary>
    /// Represents an assignment expression that includes a binary operation.
    /// </summary>
    public interface ICompoundAssignmentExpression : IAssignmentExpression, IHasOperatorMethodExpression
    {
        /// <summary>
        /// Kind of binary operation.
        /// </summary>
        BinaryOperationKind BinaryOperationKind { get; }
    }

    /// <summary>
    /// Represents an increment expression.
    /// </summary>
    public interface IIncrementExpression : ICompoundAssignmentExpression
    {
        /// <summary>
        /// Kind of increment.
        /// </summary>
        UnaryOperationKind IncrementOperationKind { get; }
    }

    /// <summary>
    /// Represents a parenthesized expression.
    /// </summary>
    public interface IParenthesizedExpression : IOperation
    {
        /// <summary>
        /// Operand enclosed in parentheses.
        /// </summary>
        IOperation Operand { get; }
    }

    /// <summary>
    /// Represents a late-bound reference to a member of a class or struct.
    /// </summary>
    public interface ILateBoundMemberReferenceExpression : IReferenceExpression
    {
        /// <summary>
        /// Instance used to bind the member reference.
        /// </summary>
        IOperation Instance { get; }
        /// <summary>
        /// Name of the member.
        /// </summary>
        string MemberName { get; }
    }

    /// <summary>
    /// Represents an argument value that has been omitted in an invocation.
    /// </summary>
    public interface IOmittedArgumentExpression : IOperation
    {
    }

    public interface IUnboundLambdaExpression : IOperation
    {
    }

    public interface IDefaultValueExpression : IOperation
    {
    }

    public interface ITypeParameterObjectCreationExpression : IOperation
    {
    }

    public interface IInvalidExpression : IOperation
    {
    }
}
