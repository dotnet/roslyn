// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
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
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
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
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
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
    /// Represents a reference to an array element.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IArrayElementReferenceExpression : IOperation
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
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IPointerIndirectionReferenceExpression : IOperation
    {
        /// <summary>
        /// Pointer to be dereferenced.
        /// </summary>
        IOperation Pointer { get; }
    }

    /// <summary>
    /// Represents a reference to a declared local variable.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface ILocalReferenceExpression : IOperation
    {
        /// <summary>
        /// Referenced local variable.
        /// </summary>
        ILocalSymbol Local { get; }
    }

    /// <summary>
    /// Represents a reference to a parameter.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IParameterReferenceExpression : IOperation
    {
        /// <summary>
        /// Referenced parameter.
        /// </summary>
        IParameterSymbol Parameter { get; }
    }

    /// <summary>
    /// Represents a reference to a local variable synthesized by language analysis.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface ISyntheticLocalReferenceExpression : IOperation
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
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
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
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IMemberReferenceExpression : IOperation
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
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
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
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
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
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
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
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IIndexedPropertyReferenceExpression : IPropertyReferenceExpression, IHasArgumentsExpression
    {
    }

    /// <summary>
    /// Represents a reference to an event.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
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
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
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
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
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
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IConditionalAccessInstanceExpression : IOperation
    {
    }

    /// <summary>
    /// Represents a general placeholder when no more specific kind of placeholder is available.
    /// A placeholder is an expression whose meaning is inferred from context.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IPlaceholderExpression : IOperation
    {
    }

    /// <summary>
    /// Represents a unary, binary, relational, or conversion operation that can use an operator method.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
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
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
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

    /// <summary>
    /// Kinds of unary operations.
    /// </summary>
    public enum UnaryOperationKind
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
        BitwiseOrLogicalNot = 0xb,

        Invalid = 0xff
    }

    internal enum UnaryOperandKind
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

        Invalid = 0xff00
    }

    /// <summary>
    /// Kinds of unary operations dependent of operands type.
    /// </summary>
    internal enum TypedUnaryOperationKind
    {
        None = 0x0,

        OperatorMethodBitwiseNegation = UnaryOperandKind.OperatorMethod | UnaryOperationKind.BitwiseNegation,
        OperatorMethodLogicalNot = UnaryOperandKind.OperatorMethod | UnaryOperationKind.LogicalNot,
        OperatorMethodPostfixIncrement = UnaryOperandKind.OperatorMethod | UnaryOperationKind.PostfixIncrement,
        OperatorMethodPostfixDecrement = UnaryOperandKind.OperatorMethod | UnaryOperationKind.PostfixDecrement,
        OperatorMethodPrefixIncrement = UnaryOperandKind.OperatorMethod | UnaryOperationKind.PrefixIncrement,
        OperatorMethodPrefixDecrement = UnaryOperandKind.OperatorMethod | UnaryOperationKind.PrefixDecrement,
        OperatorMethodPlus = UnaryOperandKind.OperatorMethod | UnaryOperationKind.Plus,
        OperatorMethodMinus = UnaryOperandKind.OperatorMethod | UnaryOperationKind.Minus,
        OperatorMethodTrue = UnaryOperandKind.OperatorMethod | UnaryOperationKind.True,
        OperatorMethodFalse = UnaryOperandKind.OperatorMethod | UnaryOperationKind.False,

        IntegerBitwiseNegation = UnaryOperandKind.Integer | UnaryOperationKind.BitwiseNegation,
        IntegerPlus = UnaryOperandKind.Integer | UnaryOperationKind.Plus,
        IntegerMinus = UnaryOperandKind.Integer | UnaryOperationKind.Minus,
        IntegerPostfixIncrement = UnaryOperandKind.Integer | UnaryOperationKind.PostfixIncrement,
        IntegerPostfixDecrement = UnaryOperandKind.Integer | UnaryOperationKind.PostfixDecrement,
        IntegerPrefixIncrement = UnaryOperandKind.Integer | UnaryOperationKind.PrefixIncrement,
        IntegerPrefixDecrement = UnaryOperandKind.Integer | UnaryOperationKind.PrefixDecrement,

        UnsignedPostfixIncrement = UnaryOperandKind.Unsigned | UnaryOperationKind.PostfixIncrement,
        UnsignedPostfixDecrement = UnaryOperandKind.Unsigned | UnaryOperationKind.PostfixDecrement,
        UnsignedPrefixIncrement = UnaryOperandKind.Unsigned | UnaryOperationKind.PrefixIncrement,
        UnsignedPrefixDecrement = UnaryOperandKind.Unsigned | UnaryOperationKind.PrefixDecrement,

        FloatingPlus = UnaryOperandKind.Floating | UnaryOperationKind.Plus,
        FloatingMinus = UnaryOperandKind.Floating | UnaryOperationKind.Minus,
        FloatingPostfixIncrement = UnaryOperandKind.Floating | UnaryOperationKind.PostfixIncrement,
        FloatingPostfixDecrement = UnaryOperandKind.Floating | UnaryOperationKind.PostfixDecrement,
        FloatingPrefixIncrement = UnaryOperandKind.Floating | UnaryOperationKind.PrefixIncrement,
        FloatingPrefixDecrement = UnaryOperandKind.Floating | UnaryOperationKind.PrefixDecrement,

        DecimalPlus = UnaryOperandKind.Decimal | UnaryOperationKind.Plus,
        DecimalMinus = UnaryOperandKind.Decimal | UnaryOperationKind.Minus,
        DecimalPostfixIncrement = UnaryOperandKind.Decimal | UnaryOperationKind.PostfixIncrement,
        DecimalPostfixDecrement = UnaryOperandKind.Decimal | UnaryOperationKind.PostfixDecrement,
        DecimalPrefixIncrement = UnaryOperandKind.Decimal | UnaryOperationKind.PrefixIncrement,
        DecimalPrefixDecrement = UnaryOperandKind.Decimal | UnaryOperationKind.PrefixDecrement,

        BooleanBitwiseNegation = UnaryOperandKind.Boolean | UnaryOperationKind.BitwiseNegation,
        BooleanLogicalNot = UnaryOperandKind.Boolean | UnaryOperationKind.LogicalNot,

        EnumPostfixIncrement = UnaryOperandKind.Enum | UnaryOperationKind.PostfixIncrement,
        EnumPostfixDecrement = UnaryOperandKind.Enum | UnaryOperationKind.PostfixDecrement,
        EnumPrefixIncrement = UnaryOperandKind.Enum | UnaryOperationKind.PrefixIncrement,
        EnumPrefixDecrement = UnaryOperandKind.Enum | UnaryOperationKind.PrefixDecrement,

        PointerPostfixIncrement = UnaryOperandKind.Pointer | UnaryOperationKind.PostfixIncrement,
        PointerPostfixDecrement = UnaryOperandKind.Pointer | UnaryOperationKind.PostfixDecrement,
        PointerPrefixIncrement = UnaryOperandKind.Pointer | UnaryOperationKind.PrefixIncrement,
        PointerPrefixDecrement = UnaryOperandKind.Pointer | UnaryOperationKind.PrefixDecrement,

        DynamicBitwiseNegation = UnaryOperandKind.Dynamic | UnaryOperationKind.BitwiseNegation,
        DynamicLogicalNot = UnaryOperandKind.Dynamic | UnaryOperationKind.LogicalNot,
        DynamicTrue = UnaryOperandKind.Dynamic | UnaryOperationKind.True,
        DynamicFalse = UnaryOperandKind.Dynamic | UnaryOperationKind.False,
        DynamicPlus = UnaryOperandKind.Dynamic | UnaryOperationKind.Plus,
        DynamicMinus = UnaryOperandKind.Dynamic | UnaryOperationKind.Minus,
        DynamicPostfixIncrement = UnaryOperandKind.Dynamic | UnaryOperationKind.PostfixIncrement,
        DynamicPostfixDecrement = UnaryOperandKind.Dynamic | UnaryOperationKind.PostfixDecrement,
        DynamicPrefixIncrement = UnaryOperandKind.Dynamic | UnaryOperationKind.PrefixIncrement,
        DynamicPrefixDecrement = UnaryOperandKind.Dynamic | UnaryOperationKind.PrefixDecrement,

        ObjectPlus = UnaryOperandKind.Object | UnaryOperationKind.Plus,
        ObjectMinus = UnaryOperandKind.Object | UnaryOperationKind.Minus,
        ObjectNot = UnaryOperandKind.Object | UnaryOperationKind.BitwiseOrLogicalNot,

        Invalid = UnaryOperandKind.Invalid | UnaryOperationKind.Invalid
    }

    /// <summary>
    /// Represents an operation with two operands that produces a result with the same type as at least one of the operands.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IBinaryOperatorExpression : IHasOperatorMethodExpression
    {
        /// <summary>
        /// Kind of binary operation.
        /// </summary>
        BinaryOperationKind BinaryOperationKind { get; }
        /// <summary>
        /// Left operand.
        /// </summary>
        IOperation LeftOperand { get; }
        /// <summary>
        /// Right operand.
        /// </summary>
        IOperation RightOperand { get; }
    }

    /// <summary>
    /// Kinds of binary operations.
    /// </summary>
    public enum BinaryOperationKind
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

        Like = 0x18,

        Invalid = 0xff
    }

    internal enum BinaryOperandsKind
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
        Nullable = 0xf00,

        Invalid = 0xff00
    }

    /// <summary>
    /// Kinds of type dependent binary operations.
    /// </summary>
    internal enum TypedBinaryOperationKind
    {
        None = 0x0,

        OperatorMethodAdd = BinaryOperandsKind.OperatorMethod | BinaryOperationKind.Add,
        OperatorMethodSubtract = BinaryOperandsKind.OperatorMethod | BinaryOperationKind.Subtract,
        OperatorMethodMultiply = BinaryOperandsKind.OperatorMethod | BinaryOperationKind.Multiply,
        OperatorMethodDivide = BinaryOperandsKind.OperatorMethod | BinaryOperationKind.Divide,
        OperatorMethodIntegerDivide = BinaryOperandsKind.OperatorMethod | BinaryOperationKind.IntegerDivide,
        OperatorMethodRemainder = BinaryOperandsKind.OperatorMethod | BinaryOperationKind.Remainder,
        OperatorMethodLeftShift = BinaryOperandsKind.OperatorMethod | BinaryOperationKind.LeftShift,
        OperatorMethodRightShift = BinaryOperandsKind.OperatorMethod | BinaryOperationKind.RightShift,
        OperatorMethodAnd = BinaryOperandsKind.OperatorMethod | BinaryOperationKind.And,
        OperatorMethodOr = BinaryOperandsKind.OperatorMethod | BinaryOperationKind.Or,
        OperatorMethodExclusiveOr = BinaryOperandsKind.OperatorMethod | BinaryOperationKind.ExclusiveOr,
        OperatorMethodConditionalAnd = BinaryOperandsKind.OperatorMethod | BinaryOperationKind.ConditionalAnd,
        OperatorMethodConditionalOr = BinaryOperandsKind.OperatorMethod | BinaryOperationKind.ConditionalOr,

        IntegerAdd = BinaryOperandsKind.Integer | BinaryOperationKind.Add,
        IntegerSubtract = BinaryOperandsKind.Integer | BinaryOperationKind.Subtract,
        IntegerMultiply = BinaryOperandsKind.Integer | BinaryOperationKind.Multiply,
        IntegerDivide = BinaryOperandsKind.Integer | BinaryOperationKind.Divide,
        IntegerRemainder = BinaryOperandsKind.Integer | BinaryOperationKind.Remainder,
        IntegerLeftShift = BinaryOperandsKind.Integer | BinaryOperationKind.LeftShift,
        IntegerRightShift = BinaryOperandsKind.Integer | BinaryOperationKind.RightShift,
        IntegerAnd = BinaryOperandsKind.Integer | BinaryOperationKind.And,
        IntegerOr = BinaryOperandsKind.Integer | BinaryOperationKind.Or,
        IntegerExclusiveOr = BinaryOperandsKind.Integer | BinaryOperationKind.ExclusiveOr,

        UnsignedAdd = BinaryOperandsKind.Unsigned | BinaryOperationKind.Add,
        UnsignedSubtract = BinaryOperandsKind.Unsigned | BinaryOperationKind.Subtract,
        UnsignedMultiply = BinaryOperandsKind.Unsigned | BinaryOperationKind.Multiply,
        UnsignedDivide = BinaryOperandsKind.Unsigned | BinaryOperationKind.Divide,
        UnsignedRemainder = BinaryOperandsKind.Unsigned | BinaryOperationKind.Remainder,
        UnsignedLeftShift = BinaryOperandsKind.Unsigned | BinaryOperationKind.LeftShift,
        UnsignedRightShift = BinaryOperandsKind.Unsigned | BinaryOperationKind.RightShift,
        UnsignedAnd = BinaryOperandsKind.Unsigned | BinaryOperationKind.And,
        UnsignedOr = BinaryOperandsKind.Unsigned | BinaryOperationKind.Or,
        UnsignedExclusiveOr = BinaryOperandsKind.Unsigned | BinaryOperationKind.ExclusiveOr,

        FloatingAdd = BinaryOperandsKind.Floating | BinaryOperationKind.Add,
        FloatingSubtract = BinaryOperandsKind.Floating | BinaryOperationKind.Subtract,
        FloatingMultiply = BinaryOperandsKind.Floating | BinaryOperationKind.Multiply,
        FloatingDivide = BinaryOperandsKind.Floating | BinaryOperationKind.Divide,
        FloatingRemainder = BinaryOperandsKind.Floating | BinaryOperationKind.Remainder,
        FloatingPower = BinaryOperandsKind.Floating | BinaryOperationKind.Power,

        DecimalAdd = BinaryOperandsKind.Decimal | BinaryOperationKind.Add,
        DecimalSubtract = BinaryOperandsKind.Decimal | BinaryOperationKind.Subtract,
        DecimalMultiply = BinaryOperandsKind.Decimal | BinaryOperationKind.Multiply,
        DecimalDivide = BinaryOperandsKind.Decimal | BinaryOperationKind.Divide,

        BooleanAnd = BinaryOperandsKind.Boolean | BinaryOperationKind.And,
        BooleanOr = BinaryOperandsKind.Boolean | BinaryOperationKind.Or,
        BooleanExclusiveOr = BinaryOperandsKind.Boolean | BinaryOperationKind.ExclusiveOr,
        BooleanConditionalAnd = BinaryOperandsKind.Boolean | BinaryOperationKind.ConditionalAnd,
        BooleanConditionalOr = BinaryOperandsKind.Boolean | BinaryOperationKind.ConditionalOr,

        EnumAdd = BinaryOperandsKind.Enum | BinaryOperationKind.Add,
        EnumSubtract = BinaryOperandsKind.Enum | BinaryOperationKind.Subtract,
        EnumAnd = BinaryOperandsKind.Enum | BinaryOperationKind.And,
        EnumOr = BinaryOperandsKind.Enum | BinaryOperationKind.Or,
        EnumExclusiveOr = BinaryOperandsKind.Enum | BinaryOperationKind.ExclusiveOr,

        PointerIntegerAdd = BinaryOperandsKind.PointerInteger | BinaryOperationKind.Add,
        IntegerPointerAdd = BinaryOperandsKind.IntegerPointer | BinaryOperationKind.Add,
        PointerIntegerSubtract = BinaryOperandsKind.PointerInteger | BinaryOperationKind.Subtract,
        PointerSubtract = BinaryOperandsKind.Pointer | BinaryOperationKind.Subtract,

        DynamicAdd = BinaryOperandsKind.Dynamic | BinaryOperationKind.Add,
        DynamicSubtract = BinaryOperandsKind.Dynamic | BinaryOperationKind.Subtract,
        DynamicMultiply = BinaryOperandsKind.Dynamic | BinaryOperationKind.Multiply,
        DynamicDivide = BinaryOperandsKind.Dynamic | BinaryOperationKind.Divide,
        DynamicRemainder = BinaryOperandsKind.Dynamic | BinaryOperationKind.Remainder,
        DynamicLeftShift = BinaryOperandsKind.Dynamic | BinaryOperationKind.LeftShift,
        DynamicRightShift = BinaryOperandsKind.Dynamic | BinaryOperationKind.RightShift,
        DynamicAnd = BinaryOperandsKind.Dynamic | BinaryOperationKind.And,
        DynamicOr = BinaryOperandsKind.Dynamic | BinaryOperationKind.Or,
        DynamicExclusiveOr = BinaryOperandsKind.Dynamic | BinaryOperationKind.ExclusiveOr,

        ObjectAdd = BinaryOperandsKind.Object | BinaryOperationKind.Add,
        ObjectSubtract = BinaryOperandsKind.Object | BinaryOperationKind.Subtract,
        ObjectMultiply = BinaryOperandsKind.Object | BinaryOperationKind.Multiply,
        ObjectDivide = BinaryOperandsKind.Object | BinaryOperationKind.Divide,
        ObjectIntegerDivide = BinaryOperandsKind.Object | BinaryOperationKind.IntegerDivide,
        ObjectRemainder = BinaryOperandsKind.Object | BinaryOperationKind.Remainder,
        ObjectPower = BinaryOperandsKind.Object | BinaryOperationKind.Power,
        ObjectLeftShift = BinaryOperandsKind.Object | BinaryOperationKind.LeftShift,
        ObjectRightShift = BinaryOperandsKind.Object | BinaryOperationKind.RightShift,
        ObjectAnd = BinaryOperandsKind.Object | BinaryOperationKind.And,
        ObjectOr = BinaryOperandsKind.Object | BinaryOperationKind.Or,
        ObjectExclusiveOr = BinaryOperandsKind.Object | BinaryOperationKind.ExclusiveOr,
        ObjectConditionalAnd = BinaryOperandsKind.Object | BinaryOperationKind.ConditionalAnd,
        ObjectConditionalOr = BinaryOperandsKind.Object | BinaryOperationKind.ConditionalOr,
        ObjectConcatenate = BinaryOperandsKind.Object | BinaryOperationKind.Concatenate,

        StringConcatenate = BinaryOperandsKind.String | BinaryOperationKind.Concatenate,

        // Relational operations.

        OperatorMethodEquals = BinaryOperandsKind.OperatorMethod | BinaryOperationKind.Equals,
        OperatorMethodNotEquals = BinaryOperandsKind.OperatorMethod | BinaryOperationKind.NotEquals,
        OperatorMethodLessThan = BinaryOperandsKind.OperatorMethod | BinaryOperationKind.LessThan,
        OperatorMethodLessThanOrEqual = BinaryOperandsKind.OperatorMethod | BinaryOperationKind.LessThanOrEqual,
        OperatorMethodGreaterThanOrEqual = BinaryOperandsKind.OperatorMethod | BinaryOperationKind.GreaterThanOrEqual,
        OperatorMethodGreaterThan = BinaryOperandsKind.OperatorMethod | BinaryOperationKind.GreaterThan,
        OperatorMethodPower = BinaryOperandsKind.OperatorMethod | BinaryOperationKind.Power,

        IntegerEquals = BinaryOperandsKind.Integer | BinaryOperationKind.Equals,
        IntegerNotEquals = BinaryOperandsKind.Integer | BinaryOperationKind.NotEquals,
        IntegerLessThan = BinaryOperandsKind.Integer | BinaryOperationKind.LessThan,
        IntegerLessThanOrEqual = BinaryOperandsKind.Integer | BinaryOperationKind.LessThanOrEqual,
        IntegerGreaterThanOrEqual = BinaryOperandsKind.Integer | BinaryOperationKind.GreaterThanOrEqual,
        IntegerGreaterThan = BinaryOperandsKind.Integer | BinaryOperationKind.GreaterThan,

        UnsignedLessThan = BinaryOperandsKind.Unsigned | BinaryOperationKind.LessThan,
        UnsignedLessThanOrEqual = BinaryOperandsKind.Unsigned | BinaryOperationKind.LessThanOrEqual,
        UnsignedGreaterThanOrEqual = BinaryOperandsKind.Unsigned | BinaryOperationKind.GreaterThanOrEqual,
        UnsignedGreaterThan = BinaryOperandsKind.Unsigned | BinaryOperationKind.GreaterThan,

        FloatingEquals = BinaryOperandsKind.Floating | BinaryOperationKind.Equals,
        FloatingNotEquals = BinaryOperandsKind.Floating | BinaryOperationKind.NotEquals,
        FloatingLessThan = BinaryOperandsKind.Floating | BinaryOperationKind.LessThan,
        FloatingLessThanOrEqual = BinaryOperandsKind.Floating | BinaryOperationKind.LessThanOrEqual,
        FloatingGreaterThanOrEqual = BinaryOperandsKind.Floating | BinaryOperationKind.GreaterThanOrEqual,
        FloatingGreaterThan = BinaryOperandsKind.Floating | BinaryOperationKind.GreaterThan,

        DecimalEquals = BinaryOperandsKind.Decimal | BinaryOperationKind.Equals,
        DecimalNotEquals = BinaryOperandsKind.Decimal | BinaryOperationKind.NotEquals,
        DecimalLessThan = BinaryOperandsKind.Decimal | BinaryOperationKind.LessThan,
        DecimalLessThanOrEqual = BinaryOperandsKind.Decimal | BinaryOperationKind.LessThanOrEqual,
        DecimalGreaterThanOrEqual = BinaryOperandsKind.Decimal | BinaryOperationKind.GreaterThanOrEqual,
        DecimalGreaterThan = BinaryOperandsKind.Decimal | BinaryOperationKind.GreaterThan,

        BooleanEquals = BinaryOperandsKind.Boolean | BinaryOperationKind.Equals,
        BooleanNotEquals = BinaryOperandsKind.Boolean | BinaryOperationKind.NotEquals,

        StringEquals = BinaryOperandsKind.String | BinaryOperationKind.Equals,
        StringNotEquals = BinaryOperandsKind.String | BinaryOperationKind.NotEquals,
        StringLike = BinaryOperandsKind.String | BinaryOperationKind.Like,

        DelegateEquals = BinaryOperandsKind.Delegate | BinaryOperationKind.Equals,
        DelegateNotEquals = BinaryOperandsKind.Delegate | BinaryOperationKind.NotEquals,

        NullableEquals = BinaryOperandsKind.Nullable | BinaryOperationKind.Equals,
        NullableNotEquals = BinaryOperandsKind.Nullable | BinaryOperationKind.NotEquals,

        ObjectEquals = BinaryOperandsKind.Object | BinaryOperationKind.Equals,
        ObjectNotEquals = BinaryOperandsKind.Object | BinaryOperationKind.NotEquals,
        ObjectVBEquals = BinaryOperandsKind.Object | BinaryOperationKind.ObjectValueEquals,
        ObjectVBNotEquals = BinaryOperandsKind.Object | BinaryOperationKind.ObjectValueNotEquals,
        ObjectLike = BinaryOperandsKind.Object | BinaryOperationKind.Like,
        ObjectLessThan = BinaryOperandsKind.Object | BinaryOperationKind.LessThan,
        ObjectLessThanOrEqual = BinaryOperandsKind.Object | BinaryOperationKind.LessThanOrEqual,
        ObjectGreaterThanOrEqual = BinaryOperandsKind.Object | BinaryOperationKind.GreaterThanOrEqual,
        ObjectGreaterThan = BinaryOperandsKind.Object | BinaryOperationKind.GreaterThan,

        EnumEquals = BinaryOperandsKind.Enum | BinaryOperationKind.Equals,
        EnumNotEquals = BinaryOperandsKind.Enum | BinaryOperationKind.NotEquals,
        EnumLessThan = BinaryOperandsKind.Enum | BinaryOperationKind.LessThan,
        EnumLessThanOrEqual = BinaryOperandsKind.Enum | BinaryOperationKind.LessThanOrEqual,
        EnumGreaterThanOrEqual = BinaryOperandsKind.Enum | BinaryOperationKind.GreaterThanOrEqual,
        EnumGreaterThan = BinaryOperandsKind.Enum | BinaryOperationKind.GreaterThan,

        PointerEquals = BinaryOperandsKind.Pointer | BinaryOperationKind.Equals,
        PointerNotEquals = BinaryOperandsKind.Pointer | BinaryOperationKind.NotEquals,
        PointerLessThan = BinaryOperandsKind.Pointer | BinaryOperationKind.LessThan,
        PointerLessThanOrEqual = BinaryOperandsKind.Pointer | BinaryOperationKind.LessThanOrEqual,
        PointerGreaterThanOrEqual = BinaryOperandsKind.Pointer | BinaryOperationKind.GreaterThanOrEqual,
        PointerGreaterThan = BinaryOperandsKind.Pointer | BinaryOperationKind.GreaterThan,

        DynamicEquals = BinaryOperandsKind.Dynamic | BinaryOperationKind.Equals,
        DynamicNotEquals = BinaryOperandsKind.Dynamic | BinaryOperationKind.NotEquals,
        DynamicLessThan = BinaryOperandsKind.Dynamic | BinaryOperationKind.LessThan,
        DynamicLessThanOrEqual = BinaryOperandsKind.Dynamic | BinaryOperationKind.LessThanOrEqual,
        DynamicGreaterThanOrEqual = BinaryOperandsKind.Dynamic | BinaryOperationKind.GreaterThanOrEqual,
        DynamicGreaterThan = BinaryOperandsKind.Dynamic | BinaryOperationKind.GreaterThan,

        Invalid = BinaryOperandsKind.Invalid | BinaryOperationKind.Invalid
    }

    /// <summary>
    /// Represents a conversion operation.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
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
        OperatorMethod = 0x5,
        /// <summary>
        /// Conversion is invalid.
        /// </summary>
        Invalid = 0xf
    }

    /// <summary>
    /// Represents a C# ?: or VB If expression.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
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
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface INullCoalescingExpression : IOperation
    {
        /// <summary>
        /// Value to be unconditionally evaluated.
        /// </summary>
        IOperation PrimaryOperand { get; }
        /// <summary>
        /// Value to be evaluated if Primary evaluates to null/Nothing.
        /// </summary>
        IOperation SecondaryOperand { get; }
    }

    /// <summary>
    /// Represents an expression that tests if a value is of a specific type.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
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
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
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
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface ISizeOfExpression : ITypeOperationExpression
    {
    }

    /// <summary>
    /// Represents a TypeOf expression.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface ITypeOfExpression : ITypeOperationExpression
    {
    }

    /// <summary>
    /// Represents a lambda expression.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
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
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
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
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
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
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IAddressOfExpression : IOperation
    {
        /// <summary>
        /// Addressed reference.
        /// </summary>
        IOperation Reference { get; }
    }

    /// <summary>
    /// Represents a new/New expression.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
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
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface ISymbolInitializer : IOperation
    {
        IOperation Value { get; }
    }

    /// <summary>
    /// Represents an initialization of a field.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
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
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
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
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
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
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
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
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
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
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IAssignmentExpression : IOperation
    {
        /// <summary>
        /// Target of the assignment.
        /// </summary>
        IOperation Target { get; }
        /// <summary>
        /// Value to be assigned to the target of the assignment.
        /// </summary>
        IOperation Value { get; }
    }

    /// <summary>
    /// Represents an assignment expression that includes a binary operation.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
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
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
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
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
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
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface ILateBoundMemberReferenceExpression : IOperation
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
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IOmittedArgumentExpression : IOperation
    {
    }

    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IUnboundLambdaExpression : IOperation
    {
    }

    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IDefaultValueExpression : IOperation
    {
    }

    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface ITypeParameterObjectCreationExpression : IOperation
    {
    }

    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IInvalidExpression : IOperation
    {
    }
}
