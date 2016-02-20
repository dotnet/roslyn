// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Semantics;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Root type for representing the abstract semantics of C# and VB statements and expressions.
    /// </summary>
    public interface IOperation
    {
        /// <summary>
        /// Identifies the kind of the operation.
        /// </summary>
        OperationKind Kind { get; }

        /// <summary>
        ///  Indicates whether the operation is invalid, either semantically or syntactically.
        /// </summary>
        bool IsInvalid { get; }

        /// <summary>
        /// Syntax that was analyzed to produce the operation.
        /// </summary>
        SyntaxNode Syntax { get; }

        /// <summary>
        /// Result type of the operation, or null if the operation does not produce a result.
        /// </summary>
        ITypeSymbol Type { get; }

        /// <summary>
        /// If the operation is an expression that evaluates to a constant value, <see cref="Optional{Object}.HasValue"/> is true and <see cref="Optional{Object}.Value"/> is the value of the expression. Otherwise, <see cref="Optional{Object}.HasValue"/> is false.
        /// </summary>
        Optional<object> ConstantValue { get; }

        void Accept(OperationVisitor visitor);

        TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument);
    }

    /// <summary>
    /// All of the kinds of operations, including statements and expressions.
    /// </summary>
    public enum OperationKind
    {
        None = 0x0,

        // Statements

        /// <summary>Indicates an <see cref="IInvalidStatement"/>.</summary>
        InvalidStatement = 0x1,
        /// <summary>Indicates an <see cref="IBlockStatement"/>.</summary>
        BlockStatement = 0x2,
        /// <summary>Indicates an <see cref="IVariableDeclarationStatement"/>.</summary>
        VariableDeclarationStatement = 0x3,
        /// <summary>Indicates an <see cref="ISwitchStatement"/>.</summary>
        SwitchStatement = 0x4,
        /// <summary>Indicates an <see cref="IIfStatement"/>.</summary>
        IfStatement = 0x5,
        /// <summary>Indicates an <see cref="ILoopStatement"/>.</summary>
        LoopStatement = 0x6,
        /// <summary>Indicates an <see cref="ILabelStatement"/>.</summary>
        LabelStatement = 0x7,
        /// <summary>Indicates an <see cref="IBranchStatement"/>.</summary>
        BranchStatement = 0x8,
        /// <summary>Indicates an <see cref="IEmptyStatement"/>.</summary>
        EmptyStatement = 0x9,
        /// <summary>Indicates an <see cref="IThrowStatement"/>.</summary>
        ThrowStatement = 0xa,
        /// <summary>Indicates an <see cref="IReturnStatement"/>.</summary>
        ReturnStatement = 0xb,
        /// <summary>Indicates an <see cref="IReturnStatement"/>.</summary>
        YieldBreakStatement = 0xc,
        /// <summary>Indicates an <see cref="ILockStatement"/>.</summary>
        LockStatement = 0xd,
        /// <summary>Indicates an <see cref="ITryStatement"/>.</summary>
        TryStatement = 0xe,
        /// <summary>Indicates an <see cref="IUsingStatement"/>.</summary>
        UsingStatement = 0xf,
        /// <summary>Indicates an <see cref="IReturnStatement"/>.</summary>
        YieldReturnStatement = 0x10,
        /// <summary>Indicates an <see cref="IExpressionStatement"/>.</summary>
        ExpressionStatement = 0x11,

        // Statements that occur only C#.

        /// <summary>Indicates an <see cref="IFixedStatement"/>.</summary>
        FixedStatement = 0x30,

        // Statements that occur only in Visual Basic.

        /// <summary>Indicates an <see cref="IStopStatement"/>.</summary>
        StopStatement = 0x50,
        /// <summary>Indicates an <see cref="IEndStatement"/>.</summary>
        EndStatement = 0x51,
        /// <summary>Indicates an <see cref="IWithStatement"/>.</summary>
        WithStatement = 0x52,

        // Expressions

        /// <summary>Indicates an <see cref="IInvalidExpression"/>.</summary>
        InvalidExpression = 0x100,
        /// <summary>Indicates an <see cref="ILiteralExpression"/>.</summary>
        LiteralExpression = 0x101,
        /// <summary>Indicates an <see cref="IConversionExpression"/>.</summary>
        ConversionExpression = 0x102,
        /// <summary>Indicates an <see cref="IInvocationExpression"/>.</summary>
        InvocationExpression = 0x103,
        /// <summary>Indicates an <see cref="IArrayElementReferenceExpression"/>.</summary>
        ArrayElementReferenceExpression = 0x104,
        /// <summary>Indicates an <see cref="ILocalReferenceExpression"/>.</summary>
        LocalReferenceExpression = 0x105,
        /// <summary>Indicates an <see cref="IParameterReferenceExpression"/>.</summary>
        ParameterReferenceExpression = 0x106,
        /// <summary>Indicates an <see cref="ISyntheticLocalReferenceExpression"/>.</summary>
        SyntheticLocalReferenceExpression = 0x107,
        /// <summary>Indicates an <see cref="IFieldReferenceExpression"/>.</summary>
        FieldReferenceExpression = 0x108,
        /// <summary>Indicates an <see cref="IMethodBindingExpression"/>.</summary>
        MethodBindingExpression = 0x109,
        /// <summary>Indicates an <see cref="IPropertyReferenceExpression"/>.</summary>
        PropertyReferenceExpression = 0x10a,
        /// <summary>Indicates an <see cref="IIndexedPropertyReferenceExpression"/>.</summary>
        IndexedPropertyReferenceExpression = 0x10b,
        /// <summary>Indicates an <see cref="IEventReferenceExpression"/>.</summary>
        EventReferenceExpression = 0x10c,
        /// <summary>Indicates an <see cref="IUnaryOperatorExpression"/>.</summary>
        UnaryOperatorExpression = 0x10d,
        /// <summary>Indicates an <see cref="IBinaryOperatorExpression"/>.</summary>
        BinaryOperatorExpression = 0x10e,
        /// <summary>Indicates an <see cref="IConditionalChoiceExpression"/>.</summary>
        ConditionalChoiceExpression = 0x10f,
        /// <summary>Indicates an <see cref="INullCoalescingExpression"/>.</summary>
        NullCoalescingExpression = 0x110,
        /// <summary>Indicates an <see cref="ILambdaExpression"/>.</summary>
        LambdaExpression = 0x111,
        /// <summary>Indicates an <see cref="IObjectCreationExpression"/>.</summary>
        ObjectCreationExpression = 0x112,
        /// <summary>Indicates an <see cref="ITypeParameterObjectCreationExpression"/>.</summary>
        TypeParameterObjectCreationExpression = 0x113,
        /// <summary>Indicates an <see cref="IArrayCreationExpression"/>.</summary>
        ArrayCreationExpression = 0x114,
        /// <summary>Indicates an <see cref="IInstanceReferenceExpression"/>.</summary>
        InstanceReferenceExpression = 0x115,
        /// <summary>Indicates an <see cref="IIsTypeExpression"/>.</summary>
        IsTypeExpression = 0x116,
        /// <summary>Indicates an <see cref="IAwaitExpression"/>.</summary>
        AwaitExpression = 0x117,
        /// <summary>Indicates an <see cref="IAssignmentExpression"/>.</summary>
        AssignmentExpression = 0x118,
        /// <summary>Indicates an <see cref="ICompoundAssignmentExpression"/>.</summary>
        CompoundAssignmentExpression = 0x119,
        /// <summary>Indicates an <see cref="IParenthesizedExpression"/>.</summary>
        ParenthesizedExpression = 0x11a,
        /// <summary>Indicates an <see cref="IEventAssignmentExpression"/>.</summary>
        EventAssignmentExpression = 0x11b,
        /// <summary>Indicates an <see cref="IConditionalAccessExpression"/>.</summary>
        ConditionalAccessExpression = 0x11c,
        /// <summary>Indicates an <see cref="IConditionalAccessInstanceExpression"/>.</summary>
        ConditionalAccessInstanceExpression = 0x11d,

        // Expressions that occur only in C#.

        /// <summary>Indicates an <see cref="IDefaultValueExpression"/>.</summary>
        DefaultValueExpression = 0x200,
        /// <summary>Indicates an <see cref="ITypeOfExpression"/>.</summary>
        TypeOfExpression = 0x201,
        /// <summary>Indicates an <see cref="ISizeOfExpression"/>.</summary>
        SizeOfExpression = 0x202,
        /// <summary>Indicates an <see cref="IAddressOfExpression"/>.</summary>
        AddressOfExpression = 0x203,
        /// <summary>Indicates an <see cref="IPointerIndirectionReferenceExpression"/>.</summary>
        PointerIndirectionReferenceExpression = 0x204,
        /// <summary>Indicates an <see cref="IUnboundLambdaExpression"/>.</summary>
        UnboundLambdaExpression = 0x205,
        /// <summary>Indicates an <see cref="IIncrementExpression"/>.</summary>
        IncrementExpression = 0x206,

        // Expressions that occur only in Visual Basic.

        /// <summary>Indicates an <see cref="IOmittedArgumentExpression"/>.</summary>
        OmittedArgumentExpression = 0x300,
        /// <summary>Indicates an <see cref="ILateBoundMemberReferenceExpression"/>.</summary>
        LateBoundMemberReferenceExpression = 0x301, 
        /// <summary>Indicates an <see cref="IPlaceholderExpression"/>.</summary>
        PlaceholderExpression = 0x302,

        // Operations that are constituents of statements, expressions, or declarations.

        /// <summary>Indicates an <see cref="IFieldInitializer"/>.</summary>
        FieldInitializerInCreation = 0x400,
        /// <summary>Indicates an <see cref="IFieldInitializer"/>.</summary>
        FieldInitializerAtDeclaration = 0x401,
        /// <summary>Indicates an <see cref="IPropertyInitializer"/>.</summary>
        PropertyInitializerInCreation = 0x402,
        /// <summary>Indicates an <see cref="IPropertyInitializer"/>.</summary>
        PropertyInitializerAtDeclaration = 0x403,
        /// <summary>Indicates an <see cref="IParameterInitializer"/>.</summary>
        ParameterInitializerAtDeclaration = 0x404,
        /// <summary>Indicates an <see cref="IArrayInitializer"/>.</summary>
        ArrayInitializer = 0x405,
        
        /// <summary>Indicates an <see cref="IVariableDeclaration"/>.</summary>
        VariableDeclaration = 0x406,

        /// <summary>Indicates an <see cref="IArgument"/>.</summary>
        Argument = 0x407,

        /// <summary>Indicates an <see cref="ICatchClause"/>.</summary>
        CatchClause = 0x408,

        /// <summary>Indicates an <see cref="ISwitchCase"/>.</summary>
        SwitchCase = 0x409,
        /// <summary>Indicates an <see cref="ISingleValueCaseClause"/>.</summary>
        SingleValueCaseClause = 0x40a,
        /// <summary>Indicates an <see cref="IRelationalCaseClause"/>.</summary>
        RelationalCaseClause = 0x40b,
        /// <summary>Indicates an <see cref="IRangeCaseClause"/>.</summary>
        RangeCaseClause = 0x40c,
    }
}
