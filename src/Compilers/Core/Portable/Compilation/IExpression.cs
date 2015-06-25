using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    public interface IExpression : IOperation
    {
        ITypeSymbol ResultType { get; }
        object ConstantValue { get; }
    }

    public interface IInvocation : IExpression
    {
        IMethodSymbol TargetMethod { get; }
        IExpression Instance { get; }
        InvocationKind InvocationClass { get; }
        ImmutableArray<IArgument> Arguments { get; }
        IArgument ArgumentMatchingParameter(IParameterSymbol parameter);
    }

    public interface IArgument
    {
        ArgumentKind ArgumentClass { get; }
        ArgumentMode Mode { get; }
        IExpression Value { get; }
        IExpression InConversion { get; }
        IExpression OutConversion { get; }
    }

    public interface INamedArgument : IArgument
    {
        string Name { get; }
    }

    public interface IReference : IExpression
    {
        ReferenceKind ReferenceClass { get; }
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

    public interface ITemporaryReference : IReference
    {
        TemporaryKind TemporaryKind { get; }
        IStatement ContainingStatement { get; }
    }

    public enum TemporaryKind
    {
        None,

        StepValue,
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
    }

    public interface IPropertyGetReference : IMemberReference
    {
        IMethodSymbol Getter { get; }
    }

    public interface IPropertySetReference : IMemberReference
    {
        IMethodSymbol Setter { get; }
    }

    public interface IPropertyReference : IMemberReference
    {
        IPropertySymbol Property { get; }
    }

    public enum ReferenceKind
    {
        Local,
        Parameter,
        Temporary,
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

    public interface IConditionalAccess : IExpression
    {
        IExpression Access { get; }
    }

    public interface IOperator : IExpression
    {
        bool UsesOperatorMethod { get; }
        IMethodSymbol Operator { get; }
    }

    public interface IUnary : IOperator
    {
        UnaryOperatorCode Operation { get; }
        IExpression Operand { get; }
    }

    public interface IBinary : IOperator
    {
        BinaryOperatorCode Operation { get; }
        IExpression Left { get; }
        IExpression Right { get; }
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

    public interface IRelational : IOperator
    {
        RelationalOperatorCode RelationalCode { get; }
        IExpression Left { get; }
        IExpression Right { get; }
    }

    public interface IConversion : IOperator
    {
        IExpression Operand { get; }
        ConversionKind Conversion { get; }
        bool IsExplicit { get; }
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

    public interface IExpressionArrayInitializer : IArrayInitializer
    {
        IExpression ElementValue { get; }
    }

    public interface IDimensionArrayInitializer : IArrayInitializer
    {
        ImmutableArray<IArrayInitializer> ElementValues { get; }
    }

    public enum ArrayInitializerKind
    {
        Expression,
        Dimension
    }

    public enum MemberInitializerKind
    {
        Field,
        Property
    }

    public interface IAssignment : IExpression
    {
        IReference Target { get; }
        IExpression Value { get; }
    }

    public interface ICompoundAssignment : IAssignment, IOperator
    {
        BinaryOperatorCode Operation { get; }
    }

    public interface IIncrement : ICompoundAssignment
    {
        UnaryOperatorCode IncrementOperation { get; }
    }

    public interface IParenthesized: IExpression
    {
        IExpression Operand { get; }
    }

    public interface ILateBoundMemberReference : IReference
    {
        IExpression Instance { get; }
        string MemberName { get; }
    }

    public enum ConversionKind
    {
        None,
        Cast,                   // Defined by underlying type system.
        AsCast,                 // Defined by the underlying type system.
        Basic,                  // Basic specific.
        CSharp,                 // C# specific.
        Operator                // Implemented by conversion operator.
    }

    public enum InvocationKind
    {
        Virtual,
        Static,
        NonVirtualInstance
    }

    public enum ArgumentKind
    {
        Positional,
        Named,
        ParamArray
    }

    public enum ArgumentMode
    {
        In,
        Out,
        Reference
    }

    public enum UnaryOperatorCode
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

    public enum BinaryOperatorCode
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

    public enum RelationalOperatorCode
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
}
