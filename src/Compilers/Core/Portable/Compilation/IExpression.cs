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
        /// If the expression evaluates to a constant value, the value of the expression, and otherwise null.
        /// </summary>
        object ConstantValue { get; }
    }

    /// <summary>
    /// Represents a C# or VB method invocation.
    /// </summary>
    public interface IInvocation : IExpression
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
        /// and params/ParamArray arguments have not been collected into arrays. Arguments are not present
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
    public interface IArgument
    {
        /// <summary>
        /// Kind of argument.
        /// </summary>
        ArgumentKind Kind { get; }
        /// <summary>
        /// Mode of argument.
        /// </summary>
        ArgumentMode Mode { get; }
        /// <summary>
        /// Value supplied for the argument.
        /// </summary>
        IExpression Value { get; }
        /// <summary>
        /// Conversion applied to the argument value passing it into the target method. Applicable only to In or Reference arguments.
        /// </summary>
        IExpression InConversion { get; }
        /// <summary>
        /// Conversion applied to the argument value after the invocation. Applicable only to Out or Reference arguments.
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
    /// Modes of arguments.
    /// </summary>
    public enum ArgumentMode
    {
        /// <summary>
        /// Argument is passed as an input value only.
        /// </summary>
        In,
        /// <summary>
        /// Argument is passed without an input value and is assigned a value by the time the invoked method returns.
        /// </summary>
        Out,
        /// <summary>
        /// Argument is passed by reference.
        /// </summary>
        Reference
    }

    /// <summary>
    /// Represents a named argument.
    /// </summary>
    public interface INamedArgument : IArgument
    {
        /// <summary>
        /// Name of the parameter that the argument matches.
        /// </summary>
        string Name { get; }
    }

    /// <summary>
    /// Represents a reference, which refers to a symbol or an element of a collection.
    /// </summary>
    public interface IReference : IExpression
    {
        /// <summary>
        /// Kind of the reference.
        /// </summary>
        ReferenceKind ReferenceKind { get; }
    }

    /// <summary>
    /// Kinds of references.
    /// </summary>
    public enum ReferenceKind
    {
        Local,
        Parameter,
        SyntheticLocal,
        ArrayElement,
        PointerIndirection,
        StaticField,
        InstanceField,
        StaticMethod,
        InstanceMethod,
        StaticPropertyGet,
        InstancePropertyGet,
        StaticPropertySet,
        InstancePropertySet,
        StaticProperty,
        InstanceProperty,
        ConstantField,
        LateBoundMember
    }

    public interface IArrayElementReference : IReference
    {
        IExpression ArrayReference { get; }
        ImmutableArray<IExpression> Indices { get; }
    }

    public interface IPointerIndirectionReference : IReference
    {
        IExpression Pointer { get; }
    }

    public interface ILocalReference : IReference
    {
        ILocalSymbol Local { get; }
    }

    public interface IParameterReference : IReference
    {
        IParameterSymbol Parameter { get; }
    }

    /// <summary>
    /// Represents a reference to a local variable synthesized by language analysis.
    /// </summary>
    public interface ISyntheticLocalReference : IReference
    {
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
        StepValue,
        /// <summary>
        /// Created to capture the limit value of a VB for loop.
        /// </summary>
        LimitValue
    }

    public interface IThisReference : IParameterReference
    {
        bool IsExplicit { get; }
    }

    public interface IMemberReference : IReference
    {
        IExpression Instance { get; }
    }

    public interface IFieldReference : IMemberReference
    {
        IFieldSymbol Field { get; }
    }

    public interface IMethodReference : IMemberReference
    {
        IMethodSymbol Method { get; }
        bool IsVirtual { get; }
    }
    
    public interface IPropertyReference : IMemberReference
    {
        IPropertySymbol Property { get; }
    }

    public interface IConditionalAccess : IExpression
    {
        IExpression Access { get; }
    }

    /// <summary>
    /// Represents a unary, binary, relational, or conversion operation that can use an operator method.
    /// </summary>
    public interface IWithOperator : IExpression
    {
        /// <summary>
        /// True if and only if the operation is performed by an operator method.
        /// </summary>
        bool UsesOperatorMethod { get; }
        /// <summary>
        /// Operation method used by the operation, null if the operation does not use an operator method.
        /// </summary>
        IMethodSymbol Operator { get; }
    }

    /// <summary>
    /// Represents an operation with one operand.
    /// </summary>
    public interface IUnary : IWithOperator
    {
        UnaryOperationKind UnaryKind { get; }
        IExpression Operand { get; }
    }

    /// <summary>
    /// Kinds of unary operations.
    /// </summary>
    public enum UnaryOperationKind
    {
        None,

        OperatorBitwiseNegation,
        OperatorLogicalNot,
        OperatorPostfixIncrement,
        OperatorPostfixDecrement,
        OperatorPrefixIncrement,
        OperatorPrefixDecrement,
        OperatorPlus,
        OperatorMinus,
        OperatorTrue,
        OperatorFalse,

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
    public interface IBinary : IWithOperator
    {
        BinaryOperationKind BinaryKind { get; }
        IExpression Left { get; }
        IExpression Right { get; }
    }

    /// <summary>
    /// Kinds of binary operations.
    /// </summary>
    public enum BinaryOperationKind
    {
        None,

        OperatorAdd,
        OperatorSubtract,
        OperatorMultiply,
        OperatorDivide,
        OperatorRemainder,
        OperatorLeftShift,
        OperatorRightShift,
        OperatorAnd,
        OperatorOr,
        OperatorXor,
        OperatorConditionalAnd,
        OperatorConditionalOr,

        IntegerAdd,
        IntegerSubtract,
        IntegerMultiply,
        IntegerDivide,
        IntegerRemainder,
        IntegerLeftShift,
        IntegerRightShift,
        IntegerAnd,
        IntegerOr,
        IntegerXor,

        UnsignedAdd,
        UnsignedSubtract,
        UnsignedMultiply,
        UnsignedDivide,
        UnsignedRemainder,
        UnsignedLeftShift,
        UnsignedRightShift,
        UnsignedAnd,
        UnsignedOr,
        UnsignedXor,

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
        BooleanXor,
        BooleanConditionalAnd,
        BooleanConditionalOr,

        EnumAdd,
        EnumSubtract,
        EnumAnd,
        EnumOr,
        EnumXor,

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
        DynamicXor,

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
        ObjectXor,
        ObjectConditionalAnd,
        ObjectConditionalOr,
        ObjectConcatenation,

        StringConcatenation
    }

    /// <summary>
    /// Represents an operation that compares two operands and produces a boolean result.
    /// </summary>
    public interface IRelational : IWithOperator
    {
        RelationalOperationKind RelationalKind { get; }
        IExpression Left { get; }
        IExpression Right { get; }
    }

    /// <summary>
    /// Kinds of relational operations.
    /// </summary>
    public enum RelationalOperationKind
    {
        None,

        OperatorEqual,
        OperatorNotEqual,
        OperatorLess,
        OperatorLessEqual,
        OperatorGreaterEqual,
        OperatorGreater,

        IntegerEqual,
        IntegerNotEqual,
        IntegerLess,
        IntegerLessEqual,
        IntegerGreaterEqual,
        IntegerGreater,
        UnsignedLess,
        UnsignedLessEqual,
        UnsignedGreaterEqual,
        UnsignedGreater,

        FloatingEqual,
        FloatingNotEqual,
        FloatingLess,
        FloatingLessEqual,
        FloatingGreaterEqual,
        FloatingGreater,

        DecimalEqual,
        DecimalNotEqual,
        DecimalLess,
        DecimalLessEqual,
        DecimalGreaterEqual,
        DecimalGreater,

        BooleanEqual,
        BooleanNotEqual,

        StringEqual,
        StringNotEqual,
        StringLike,

        DelegateEqual,
        DelegateNotEqual,

        NullableEqual,
        NullableNotEqual,

        ObjectLess,
        ObjectLessEqual,
        ObjectEqual,
        ObjectNotEqual,
        ObjectVBEqual,
        ObjectVBNotEqual,
        ObjectLike,
        ObjectGreaterEqual,
        ObjectGreater,

        EnumEqual,
        EnumNotEqual,
        EnumLess,
        EnumLessEqual,
        EnumGreaterEqual,
        EnumGreater,

        PointerEqual,
        PointerNotEqual,
        PointerLess,
        PointerLessEqual,
        PointerGreaterEqual,
        PointerGreater,

        DynamicEqual,
        DynamicNotEqual,
        DynamicLess,
        DynamicLessEqual,
        DynamicGreaterEqual,
        DynamicGreater
    }

    /// <summary>
    /// Represents a conversion operation.
    /// </summary>
    public interface IConversion : IWithOperator
    {
        IExpression Operand { get; }
        ConversionKind Conversion { get; }
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
        Operator
    }

    public interface IConditionalChoice : IExpression
    {
        IExpression Condition { get; }
        IExpression IfTrue { get; }
        IExpression IfFalse { get; }
    }

    public interface INullCoalescing : IExpression
    {
        IExpression Primary { get; }
        IExpression Secondary { get; }
    }

    public interface IIs : IExpression
    {
        IExpression Operand { get; }
        ITypeSymbol IsType { get; }
    }

    public interface ITypeOperation : IExpression
    {
        TypeOperationKind TypeOperationClass { get; }
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

    public interface ILambda : IExpression
    {
        IMethodSymbol Signature { get; }
        IBlock Body { get; }
    }

    public interface ILiteral : IExpression
    {
        LiteralKind LiteralClass { get; }
        string Spelling { get; }
    }

    /// <summary>
    /// Kinds of literals.
    /// </summary>
    public enum LiteralKind
    {
        None,
        Boolean,
        DateTime,
        Integer,
        Floating,
        Character,
        Decimal,
        String
    }

    public interface IAwait : IExpression
    {
        IExpression Upon { get; }
    }

    public interface IAddressOf : IExpression
    {
        IReference Addressed { get; }
    }

    public interface IObjectCreation : IExpression
    {
        IMethodSymbol Constructor { get; }
        ImmutableArray<IArgument> ConstructorArguments { get; }
        IArgument ArgumentMatchingParameter(IParameterSymbol parameter);
        ImmutableArray<IMemberInitializer> MemberInitializers { get; }
    }

    public interface IMemberInitializer
    {
        MemberInitializerKind MemberClass { get; }
        IExpression Value { get; }
    }

    /// <summary>
    /// Kinds of member initializers.
    /// </summary>
    public enum MemberInitializerKind
    {
        Field,
        Property
    }

    public interface IFieldInitializer : IMemberInitializer
    {
        IFieldSymbol Field { get; }
    }

    public interface IPropertyInitializer : IMemberInitializer
    {
        IMethodSymbol Setter { get; }
    }

    public interface IArrayCreation : IExpression
    {
        ITypeSymbol ElementType { get; }
        ImmutableArray<IExpression> DimensionSizes { get; }
        IArrayInitializer ElementValues { get; }
    }

    public interface IArrayInitializer
    {
        ArrayInitializerKind ArrayClass { get; }
    }

    /// <summary>
    /// Kinds of array initializers.
    /// </summary>
    public enum ArrayInitializerKind
    {
        /// <summary>
        /// Initializer specifies a single element value.
        /// </summary>
        Expression,
        /// <summary>
        /// Initializer specifies multiple elements of a dimension of the array. 
        /// </summary>
        Dimension
    }

    public interface IExpressionArrayInitializer : IArrayInitializer
    {
        IExpression ElementValue { get; }
    }

    public interface IDimensionArrayInitializer : IArrayInitializer
    {
        ImmutableArray<IArrayInitializer> ElementValues { get; }
    }

    public interface IAssignment : IExpression
    {
        IReference Target { get; }
        IExpression Value { get; }
    }

    public interface ICompoundAssignment : IAssignment, IWithOperator
    {
        BinaryOperationKind BinaryKind { get; }
    }

    public interface IIncrement : ICompoundAssignment
    {
        UnaryOperationKind IncrementKind { get; }
    }

    public interface IParenthesized : IExpression
    {
        IExpression Operand { get; }
    }

    public interface ILateBoundMemberReference : IReference
    {
        IExpression Instance { get; }
        string MemberName { get; }
    }

    /// <summary>
    /// Defines extension methods useful for IExpression instances.
    /// </summary>
    public static class IExpressionExtensions
    {
        public static bool IsStatic(this IInvocation invocation)
        {
            return invocation.TargetMethod.IsStatic;
        }
    }
}
