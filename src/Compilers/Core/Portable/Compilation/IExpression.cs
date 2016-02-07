// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents a C# or VB expression.
    /// </summary>
    public interface IExpression : IOperation
    {
        /// <summary>
        /// Result type of the expression.
        /// </summary>
        ITypeSymbol ResultType { get; }
        /// <summary>
        /// If the expression evaluates to a constant value, <see cref="Optional{Object}.HasValue"/> is true and <see cref="Optional{Object}.Value"/> is the value of the expression, and otherwise <see cref="Optional{Object}.HasValue"/> is false.
        /// </summary>
        Optional<object> ConstantValue { get; }
    }

    /// <summary>
    /// Represents a C# or VB method invocation.
    /// </summary>
    public interface IInvocationExpression : IExpression
    {
        /// <summary>
        /// Method to be invoked.
        /// </summary>
        IMethodSymbol TargetMethod { get; }
        /// <summary>
        /// 'This' or 'Me' argument to be supplied to the method.
        /// </summary>
        IExpression Instance { get; }
        /// <summary>
        /// True if the invocation uses a virtual mechanism, and false otherwise.
        /// </summary>
        bool IsVirtual { get; }
        /// <summary>
        /// Arguments of the invocation, excluding the instance argument. Arguments are in parameter order,
        /// and params/ParamArray arguments have been collected into arrays. Default values are supplied for
        /// optional arguments missing in source.
        /// </summary>
        ImmutableArray<IArgument> ArgumentsInParameterOrder { get; }
        /// <summary>
        /// Arguments of the invocation, excluding the instance argument. Arguments are in the order specified in source,
        /// and params/ParamArray arguments have been collected into arrays. Arguments are not present
        /// unless supplied in source.
        /// </summary>
        ImmutableArray<IArgument> ArgumentsInSourceOrder { get; }
        /// <summary>
        /// Find the argument supplied for a given parameter of the target method.
        /// </summary>
        /// <param name="parameter">Parameter of the target method.</param>
        /// <returns>Argument corresponding to the parameter.</returns>
        IArgument ArgumentMatchingParameter(IParameterSymbol parameter);
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
        IExpression Value { get; }
        /// <summary>
        /// Conversion applied to the argument value passing it into the target method. Applicable only to VB Reference arguments.
        /// </summary>
        IExpression InConversion { get; }
        /// <summary>
        /// Conversion applied to the argument value after the invocation. Applicable only to VB Reference arguments.
        /// </summary>
        IExpression OutConversion { get; }
    }

    /// <summary>
    /// Kinds of arguments.
    /// </summary>
    public enum ArgumentKind
    {
        /// <summary>
        /// Argument is specified positionally and matches the parameter of the same ordinality.
        /// </summary>
        Positional,
        /// <summary>
        /// Argument is specified by name and matches the parameter of the same name.
        /// </summary>
        Named,
        /// <summary>
        /// Argument becomes an element of an array that matches a trailing C# params or VB ParamArray parameter.
        /// </summary>
        ParamArray,
        /// <summary>
        /// Argument was omitted in source but has a default value supplied automatically.
        /// </summary>
        DefaultValue
    }

    /// <summary>
    /// Represents a reference, which refers to a symbol or an element of a collection.
    /// </summary>
    public interface IReferenceExpression : IExpression
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
        IExpression ArrayReference { get; }
        /// <summary>
        /// Indices that specify an individual element.
        /// </summary>
        ImmutableArray<IExpression> Indices { get; }
    }

    /// <summary>
    /// Represents a reference through a pointer.
    /// </summary>
    public interface IPointerIndirectionReferenceExpression : IReferenceExpression
    {
        /// <summary>
        /// Pointer to be dereferenced.
        /// </summary>
        IExpression Pointer { get; }
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
        IStatement ContainingStatement { get; }
    }

    /// <summary>
    /// Kinds of synthetic local references.
    /// </summary>
    public enum SyntheticLocalKind
    {
        None,

        /// <summary>
        /// Created to capture the step value of a VB for loop.
        /// </summary>
        ForLoopStepValue,
        /// <summary>
        /// Created to capture the limit value of a VB for loop.
        /// </summary>
        ForLoopLimitValue
    }

    /// <summary>
    /// Represents a reference to a C# this or VB Me parameter.
    /// </summary>
    public interface IInstanceReferenceExpression : IParameterReferenceExpression
    {
        /// <summary>
        /// Indicates whether the reference is explicit or implicit in source.
        /// </summary>
        bool IsExplicit { get; }
    }

    /// <summary>
    /// Represents a reference to a member of a class, struct, or interface.
    /// </summary>
    public interface IMemberReferenceExpression : IReferenceExpression
    {
        /// <summary>
        /// Instance of the type. Null if the reference is to a static/shared member.
        /// </summary>
        IExpression Instance { get; }

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
    public interface IEventAssignmentExpression : IExpression
    {
        /// <summary>
        /// Event being bound.
        /// </summary>
        IEventSymbol Event { get; }

        /// <summary>
        /// Instance used to refer to the event being bound.
        /// </summary>
        IExpression EventInstance { get; }

        /// <summary>
        /// Handler supplied for the event.
        /// </summary>
        IExpression HandlerValue { get; }

        /// <summary>
        /// True for adding a binding, false for removing one.
        /// </summary>
        bool Adds { get; }
    }

    /// <summary>
    /// Represents a conditional access expression.
    /// </summary>
    public interface IConditionalAccessExpression : IExpression
    {
        /// <summary>
        /// Expression subject to conditional access.
        /// </summary>
        IExpression Access { get; }
    }

    /// <summary>
    /// Represents a unary, binary, relational, or conversion operation that can use an operator method.
    /// </summary>
    public interface IHasOperatorMethodExpression : IExpression
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
        IExpression Operand { get; }
    }

    enum SimpleUnaryOperationKind
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
        False = 0xa
    }

    enum UnaryOperandKind
    {
        OperatorMethod = 0x1,
        Integer = 0x2,
        Unsigned = 0x3,
        Floating = 0x4,
        Decimal = 0x5,
        Boolean = 0x6,
        Enum = 0x7,
        Dynamic = 0x8,
        Object = 0x9,
        Pointer = 0xa
    }

    /// <summary>
    /// Kinds of unary operations.
    /// </summary>
    public enum UnaryOperationKind
    {
        None,

        OperatorMethodBitwiseNegation,
        OperatorMethodLogicalNot,
        OperatorMethodPostfixIncrement,
        OperatorMethodPostfixDecrement,
        OperatorMethodPrefixIncrement,
        OperatorMethodPrefixDecrement,
        OperatorMethodPlus,
        OperatorMethodMinus,
        OperatorMethodTrue,
        OperatorMethodFalse,

        IntegerBitwiseNegation,
        IntegerPlus,
        IntegerMinus,
        IntegerPostfixIncrement,
        IntegerPostfixDecrement,
        IntegerPrefixIncrement,
        IntegerPrefixDecrement,

        UnsignedPostfixIncrement,
        UnsignedPostfixDecrement,
        UnsignedPrefixIncrement,
        UnsignedPrefixDecrement,

        FloatingPlus,
        FloatingMinus,
        FloatingPostfixIncrement,
        FloatingPostfixDecrement,
        FloatingPrefixIncrement,
        FloatingPrefixDecrement,

        DecimalPlus,
        DecimalMinus,
        DecimalPostfixIncrement,
        DecimalPostfixDecrement,
        DecimalPrefixIncrement,
        DecimalPrefixDecrement,

        BooleanBitwiseNegation,
        BooleanLogicalNot,

        EnumPostfixIncrement,
        EnumPostfixDecrement,
        EnumPrefixIncrement,
        EnumPrefixDecrement,

        PointerPostfixIncrement,
        PointerPostfixDecrement,
        PointerPrefixIncrement,
        PointerPrefixDecrement,

        DynamicBitwiseNegation,
        DynamicLogicalNot,
        DynamicTrue,
        DynamicFalse,
        DynamicPlus,
        DynamicMinus,
        DynamicPostfixIncrement,
        DynamicPostfixDecrement,
        DynamicPrefixIncrement,
        DynamicPrefixDecrement,

        ObjectPlus,
        ObjectMinus,
        ObjectNot
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
        IExpression Left { get; }
        /// <summary>
        /// Right operand.
        /// </summary>
        IExpression Right { get; }
    }

    enum SimpleBinaryOperationKind
    {
        None = 0x0,

        Add = 0x1,
        Subtract = 0x2,
        Multiply = 0x3,
        Divide = 0x4,
        Remainder = 0x5,
        Power = 0x6,
        LeftShift = 0x7,
        RightShift = 0x8,
        And = 0x9,
        Or = 0xa,
        ExclusiveOr = 0xb,
        ConditionalAnd = 0xc,
        ConditionalOr = 0xd,
        Concatenate = 0xe,

        // Relational operations.

        Equals = 0xf,
        NotEquals = 0x10,
        LessThan = 0x11,
        LessThanOrEqual = 0x12,
        GreaterThanOrEqual = 0x13,
        GreaterThan = 0x14,

        Like = 0x15
    }

    enum BinaryOperandsKind
    {
        OperatorMethod = 0x1001,
        Integer = 0x1002,
        Unsigned = 0x1003,
        Floating = 0x1004,
        Decimal = 0x1005,
        Boolean = 0x1006,
        Enum = 0x1007,
        Dynamic = 0x1008,
        Object = 0x1009,
        PointerInteger = 0x100a,
        IntegerPointer = 0x100b,
        String = 0x100c,
        Delegate = 0x100d,
        Nullable = 0x100e
    }

    /// <summary>
    /// Kinds of binary operations.
    /// </summary>
    public enum BinaryOperationKind
    {
        None,

        OperatorMethodAdd,
        OperatorMethodSubtract,
        OperatorMethodMultiply,
        OperatorMethodDivide,
        OperatorMethodRemainder,
        OperatorMethodLeftShift,
        OperatorMethodRightShift,
        OperatorMethodAnd,
        OperatorMethodOr,
        OperatorMethodExclusiveOr,
        OperatorMethodConditionalAnd,
        OperatorMethodConditionalOr,

        IntegerAdd,
        IntegerSubtract,
        IntegerMultiply,
        IntegerDivide,
        IntegerRemainder,
        IntegerLeftShift,
        IntegerRightShift,
        IntegerAnd,
        IntegerOr,
        IntegerExclusiveOr,

        UnsignedAdd,
        UnsignedSubtract,
        UnsignedMultiply,
        UnsignedDivide,
        UnsignedRemainder,
        UnsignedLeftShift,
        UnsignedRightShift,
        UnsignedAnd,
        UnsignedOr,
        UnsignedExclusiveOr,

        FloatingAdd,
        FloatingSubtract,
        FloatingMultiply,
        FloatingDivide,
        FloatingRemainder,
        FloatingPower,

        DecimalAdd,
        DecimalSubtract,
        DecimalMultiply,
        DecimalDivide,

        BooleanAnd,
        BooleanOr,
        BooleanExclusiveOr,
        BooleanConditionalAnd,
        BooleanConditionalOr,

        EnumAdd,
        EnumSubtract,
        EnumAnd,
        EnumOr,
        EnumExclusiveOr,

        PointerIntegerAdd,
        IntegerPointerAdd,
        PointerIntegerSubtract,
        PointerSubtract,

        DynamicAdd,
        DynamicSubtract,
        DynamicMultiply,
        DynamicDivide,
        DynamicRemainder,
        DynamicLeftShift,
        DynamicRightShift,
        DynamicAnd,
        DynamicOr,
        DynamicExclusiveOr,

        ObjectAdd,
        ObjectSubtract,
        ObjectMultiply,
        ObjectDivide,
        ObjectPower,
        ObjectIntegerDivide,
        ObjectRemainder,
        ObjectLeftShift,
        ObjectRightShift,
        ObjectAnd,
        ObjectOr,
        ObjectExclusiveOr,
        ObjectConditionalAnd,
        ObjectConditionalOr,
        ObjectConcatenation,

        StringConcatenation,

        // Relational operations.

        OperatorMethodEquals,
        OperatorMethodNotEquals,
        OperatorMethodLessThan,
        OperatorMethodLessThanOrEqual,
        OperatorMethodGreaterThanOrEqual,
        OperatorMethodGreaterThan,

        IntegerEquals,
        IntegerNotEquals,
        IntegerLessThan,
        IntegerLessThanOrEqual,
        IntegerGreaterThanOrEqual,
        IntegerGreaterThan,
        UnsignedLessThan,
        UnsignedLessThanOrEqual,
        UnsignedGreaterThanOrEqual,
        UnsignedGreaterThan,

        FloatingEquals,
        FloatingNotEquals,
        FloatingLessThan,
        FloatingLessThanOrEqual,
        FloatingGreaterThanOrEqual,
        FloatingGreaterThan,

        DecimalEquals,
        DecimalNotEquals,
        DecimalLessThan,
        DecimalLessThanOrEqual,
        DecimalGreaterThanOrEqual,
        DecimalGreaterThan,

        BooleanEquals,
        BooleanNotEquals,

        StringEquals,
        StringNotEquals,
        StringLike,

        DelegateEquals,
        DelegateNotEquals,

        NullableEquals,
        NullableNotEquals,

        ObjectEquals,
        ObjectNotEquals,
        ObjectVBEquals,
        ObjectVBNotEquals,
        ObjectLike,
        ObjectLessThan,
        ObjectLessThanOrEqual,
        ObjectGreaterThanOrEqual,
        ObjectGreaterThan,

        EnumEquals,
        EnumNotEquals,
        EnumLessThan,
        EnumLessThanOrEqual,
        EnumGreaterThanOrEqual,
        EnumGreaterThan,

        PointerEquals,
        PointerNotEquals,
        PointerLessThan,
        PointerLessThanOrEqual,
        PointerGreaterThanOrEqual,
        PointerGreaterThan,

        DynamicEquals,
        DynamicNotEquals,
        DynamicLessThan,
        DynamicLessThanOrEqual,
        DynamicGreaterThanOrEqual,
        DynamicGreaterThan
    }

    /// <summary>
    /// Represents a conversion operation.
    /// </summary>
    public interface IConversionExpression : IHasOperatorMethodExpression
    {
        /// <summary>
        /// Value to be converted.
        /// </summary>
        IExpression Operand { get; }
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
        None,
        /// <summary>
        /// Conversion is defined by the underlying type system and throws an exception if it fails.
        /// </summary>
        Cast,
        /// <summary>
        /// Conversion is defined by the underlying type system and produces a null result if it fails.
        /// </summary>
        AsCast,
        /// <summary>
        /// Conversion has VB-specific semantics.
        /// </summary>
        Basic,
        /// <summary>
        /// Conversion has C#-specific semantics.
        /// </summary>
        CSharp,
        /// <summary>
        /// Conversion is implemented by a conversion operator method.
        /// </summary>
        OperatorMethod
    }

    /// <summary>
    /// Represents a C# ?: or VB If expression.
    /// </summary>
    public interface IConditionalChoiceExpression : IExpression
    {
        /// <summary>
        /// Condition to be tested.
        /// </summary>
        IExpression Condition { get; }
        /// <summary>
        /// Value evaluated if the Condition is true.
        /// </summary>
        IExpression IfTrue { get; }
        /// <summary>
        /// Value evaluated if the Condition is false.
        /// </summary>
        IExpression IfFalse { get; }
    }

    /// <summary>
    /// Represents a null-coalescing expression.
    /// </summary>
    public interface INullCoalescingExpression : IExpression
    {
        /// <summary>
        /// Value to be unconditionally evaluated.
        /// </summary>
        IExpression Primary { get; }
        /// <summary>
        /// Value to be evaluated if Primary evaluates to null/Nothing.
        /// </summary>
        IExpression Secondary { get; }
    }

    /// <summary>
    /// Represents an expression that tests if a value is of a specific type.
    /// </summary>
    public interface IIsExpression : IExpression
    {
        /// <summary>
        /// Value to test.
        /// </summary>
        IExpression Operand { get; }
        /// <summary>
        /// Type for which to test.
        /// </summary>
        ITypeSymbol IsType { get; }
    }

    /// <summary>
    /// Represents an expression operating on a type.
    /// </summary>
    public interface ITypeOperationExpression : IExpression
    {
        /// <summary>
        /// Kind of type operation.
        /// </summary>
        TypeOperationKind TypeOperationKind { get; }
        /// <summary>
        /// Type operand.
        /// </summary>
        ITypeSymbol TypeOperand { get; }
    }

    /// <summary>
    /// Kinds of type operations.
    /// </summary>
    public enum TypeOperationKind
    {
        None,

        SizeOf,
        TypeOf
    }

    /// <summary>
    /// Represents a lambda expression.
    /// </summary>
    public interface ILambdaExpression : IExpression
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
    public interface ILiteralExpression : IExpression
    {
        /// <summary>
        /// Textual representation of the literal.
        /// </summary>
        string Spelling { get; }
    }

    /// <summary>
    /// Represents an await expression.
    /// </summary>
    public interface IAwaitExpression : IExpression
    {
        /// <summary>
        /// Value to be awaited.
        /// </summary>
        IExpression Upon { get; }
    }

    /// <summary>
    /// Represents an expression that creates a pointer value by taking the address of a reference.
    /// </summary>
    public interface IAddressOfExpression : IExpression
    {
        /// <summary>
        /// Addressed reference.
        /// </summary>
        IReferenceExpression Addressed { get; }
    }

    /// <summary>
    /// Represents a new/New expression.
    /// </summary>
    public interface IObjectCreationExpression : IExpression
    {
        /// <summary>
        /// Constructor to be invoked for the created instance.
        /// </summary>
        IMethodSymbol Constructor { get; }
        /// <summary>
        /// Arguments to the constructor.
        /// </summary>
        ImmutableArray<IArgument> ConstructorArguments { get; }
        IArgument ArgumentMatchingParameter(IParameterSymbol parameter);
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
        IExpression Value { get; }
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
    public interface IArrayCreationExpression : IExpression
    {
        /// <summary>
        /// Element type of the created array instance.
        /// </summary>
        ITypeSymbol ElementType { get; }
        /// <summary>
        /// Sizes of the dimensions of the created array instance.
        /// </summary>
        ImmutableArray<IExpression> DimensionSizes { get; }
        /// <summary>
        /// Values of elements of the created array instance.
        /// </summary>
        IArrayInitializer Initializer { get; }
    }

    /// <summary>
    /// Represents the initialization of an array instance.
    /// </summary>
    public interface IArrayInitializer : IExpression
    {
        /// <summary>
        /// Values to initialize array elements.
        /// </summary>
        ImmutableArray<IExpression> ElementValues { get; }
    }

    /// <summary>
    /// Represents an assignment expression.
    /// </summary>
    public interface IAssignmentExpression : IExpression
    {
        /// <summary>
        /// Target of the assignment.
        /// </summary>
        IReferenceExpression Target { get; }
        /// <summary>
        /// Value to be assigned to the target of the assignment.
        /// </summary>
        IExpression Value { get; }
    }

    /// <summary>
    /// Represents an assignment expression that includes a binary operation.
    /// </summary>
    public interface ICompoundAssignmentExpression : IAssignmentExpression, IHasOperatorMethodExpression
    {
        /// <summary>
        /// Kind of binary operation.
        /// </summary>
        BinaryOperationKind BinaryKind { get; }
    }

    /// <summary>
    /// Represents an increment expression.
    /// </summary>
    public interface IIncrementExpression : ICompoundAssignmentExpression
    {
        /// <summary>
        /// Kind of increment.
        /// </summary>
        UnaryOperationKind IncrementKind { get; }
    }

    /// <summary>
    /// Represents a parenthesized expression.
    /// </summary>
    public interface IParenthesizedExpression : IExpression
    {
        /// <summary>
        /// Operand enclosed in parentheses.
        /// </summary>
        IExpression Operand { get; }
    }

    /// <summary>
    /// Represents a late-bound reference to a member of a class or struct.
    /// </summary>
    public interface ILateBoundMemberReferenceExpression : IReferenceExpression
    {
        /// <summary>
        /// Instance used to bind the member reference.
        /// </summary>
        IExpression Instance { get; }
        /// <summary>
        /// Name of the member.
        /// </summary>
        string MemberName { get; }
    }

    /// <summary>
    /// Defines extension methods useful for IExpression instances.
    /// </summary>
    public static class IExpressionExtensions
    {
        /// <summary>
        /// Tests if an invocation is to a static/shared method.
        /// </summary>
        /// <param name="invocation">Invocation to be tested.</param>
        /// <returns>True if the invoked method is static/shared, false otherwise.</returns>
        public static bool IsStatic(this IInvocationExpression invocation)
        {
            return invocation.TargetMethod.IsStatic;
        }
    }
}
