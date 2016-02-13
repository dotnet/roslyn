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
        /// <summary>Indicates an <see cref="IReturnStatement"/>.</summary>
        YieldBreakStatement = 0x9,
        /// <summary>Indicates an <see cref="ILabelStatement"/>.</summary>
        LabelStatement = 0xa,
        /// <summary>Indicates an <see cref="IBranchStatement"/>.</summary>
        BranchStatement = 0xc,
        /// <summary>Indicates an <see cref="IEmptyStatement"/>.</summary>
        EmptyStatement = 0xd,
        /// <summary>Indicates an <see cref="IThrowStatement"/>.</summary>
        ThrowStatement = 0xe,
        /// <summary>Indicates an <see cref="IReturnStatement"/>.</summary>
        ReturnStatement = 0xf,
        /// <summary>Indicates an <see cref="ILockStatement"/>.</summary>
        LockStatement = 0x10,
        /// <summary>Indicates an <see cref="ITryStatement"/>.</summary>
        TryStatement = 0x11,
        /// <summary>Indicates an <see cref="ICatchClause"/>.</summary>
        CatchClause = 0x12,
        /// <summary>Indicates an <see cref="IUsingWithDeclarationStatement"/>.</summary>
        UsingWithDeclarationStatement = 0x13,
        /// <summary>Indicates an <see cref="IUsingWithExpressionStatement"/>.</summary>
        UsingWithExpressionStatement = 0x14,
        /// <summary>Indicates an <see cref="IReturnStatement"/>.</summary>
        YieldReturnStatement = 0x15,
        /// <summary>Indicates an <see cref="IFixedStatement"/>.</summary>
        FixedStatement = 0x16,
        // LocalFunctionStatement = 0x17,

        /// <summary>Indicates an <see cref="IExpressionStatement"/>.</summary>
        ExpressionStatement = 0x18,

        /// <summary>Indicates an <see cref="IInvalidExpression"/>.</summary>
        InvalidExpression = 0x19,
        /// <summary>Indicates an <see cref="ILiteralExpression"/>.</summary>
        LiteralExpression = 0x1a,
        /// <summary>Indicates an <see cref="IConversionExpression"/>.</summary>
        ConversionExpression = 0x1b,
        /// <summary>Indicates an <see cref="IInvocationExpression"/>.</summary>
        InvocationExpression = 0x1c,
        /// <summary>Indicates an <see cref="IArrayElementReferenceExpression"/>.</summary>
        ArrayElementReferenceExpression = 0x1d,
        /// <summary>Indicates an <see cref="IPointerIndirectionReferenceExpression"/>.</summary>
        PointerIndirectionReferenceExpression = 0x1e,
        /// <summary>Indicates an <see cref="ILocalReferenceExpression"/>.</summary>
        LocalReferenceExpression = 0x1f,
        /// <summary>Indicates an <see cref="IParameterReferenceExpression"/>.</summary>
        ParameterReferenceExpression = 0x20,
        /// <summary>Indicates an <see cref="ISyntheticLocalReferenceExpression"/>.</summary>
        SyntheticLocalReferenceExpression = 0x21,
        /// <summary>Indicates an <see cref="IFieldReferenceExpression"/>.</summary>
        FieldReferenceExpression = 0x22,
        /// <summary>Indicates an <see cref="IMethodBindingExpression"/>.</summary>
        MethodBindingExpression = 0x23,
        /// <summary>Indicates an <see cref="IPropertyReferenceExpression"/>.</summary>
        PropertyReferenceExpression = 0x24,
        /// <summary>Indicates an <see cref="IIndexedPropertyReferenceExpression"/>.</summary>
        IndexedPropertyReferenceExpression = 0x4f,
        /// <summary>Indicates an <see cref="IEventReferenceExpression"/>.</summary>
        EventReferenceExpression = 0x25,
        /// <summary>Indicates an <see cref="ILateBoundMemberReferenceExpression"/>.</summary>
        LateBoundMemberReferenceExpression = 0x26,
        /// <summary>Indicates an <see cref="IUnaryOperatorExpression"/>.</summary>
        UnaryOperatorExpression = 0x27,
        /// <summary>Indicates an <see cref="IBinaryOperatorExpression"/>.</summary>
        BinaryOperatorExpression = 0x28,
        /// <summary>Indicates an <see cref="IConditionalChoiceExpression"/>.</summary>
        ConditionalChoiceExpression = 0x29,
        /// <summary>Indicates an <see cref="INullCoalescingExpression"/>.</summary>
        NullCoalescingExpression = 0x2a,
        /// <summary>Indicates an <see cref="ILambdaExpression"/>.</summary>
        LambdaExpression = 0x2b,
        /// <summary>Indicates an <see cref="IObjectCreationExpression"/>.</summary>
        ObjectCreationExpression = 0x2c,
        /// <summary>Indicates an <see cref="ITypeParameterObjectCreationExpression"/>.</summary>
        TypeParameterObjectCreationExpression = 0x2d,
        /// <summary>Indicates an <see cref="IArrayCreationExpression"/>.</summary>
        ArrayCreationExpression = 0x2e,
        /// <summary>Indicates an <see cref="IDefaultValueExpression"/>.</summary>
        DefaultValueExpression = 0x2f,
        /// <summary>Indicates an <see cref="IInstanceReferenceExpression"/>.</summary>
        InstanceReferenceExpression = 0x30,
        /// <summary>Indicates an <see cref="IIsExpression"/>.</summary>
        IsExpression = 0x33,
        // TypeOperationExpression = 0x34,
        AwaitExpression = 0x35,
        /// <summary>Indicates an <see cref="IAddressOfExpression"/>.</summary>
        AddressOfExpression = 0x36,
        /// <summary>Indicates an <see cref="IAssignmentExpression"/>.</summary>
        AssignmentExpression = 0x37,
        /// <summary>Indicates an <see cref="ICompoundAssignmentExpression"/>.</summary>
        CompoundAssignmentExpression = 0x38,
        /// <summary>Indicates an <see cref="IParenthesizedExpression"/>.</summary>
        ParenthesizedExpression = 0x39,

        /// <summary>Indicates an <see cref="IUnboundLambdaExpression"/>.</summary>
        UnboundLambdaExpression = 0x3a,
        /// <summary>Indicates an <see cref="IEventAssignmentExpression"/>.</summary>
        EventAssignmentExpression = 0x3b,

        /// <summary>Indicates an <see cref="ITypeOfExpression"/>.</summary>
        TypeOfExpression = 0x34,
        /// <summary>Indicates an <see cref="ISizeOfExpression"/>.</summary>
        SizeOfExpression = 0x50,

        // VB only

        /// <summary>Indicates an <see cref="IOmittedArgumentExpression"/>.</summary>
        OmittedArgumentExpression = 0x3c,
        /// <summary>Indicates an <see cref="IStopStatement"/>.</summary>
        StopStatement = 0x3d,
        /// <summary>Indicates an <see cref="IEndStatement"/>.</summary>
        EndStatement = 0x3e,
        /// <summary>Indicates an <see cref="IWithStatement"/>.</summary>
        WithStatement = 0x3f,

        // Newly added

        /// <summary>Indicates an <see cref="IConditionalAccessExpression"/>.</summary>
        ConditionalAccessExpression = 0x40,
        /// <summary>Indicates an <see cref="IConditionalAccessInstanceExpression"/>.</summary>
        ConditionalAccessInstanceExpression = 0x4e,

        /// <summary>Indicates an <see cref="IIncrementExpression"/>.</summary>
        IncrementExpression = 0x41,

        /// <summary>Indicates an <see cref="IArgument"/>.</summary>
        Argument = 0x42,
        /// <summary>Indicates an <see cref="IFieldInitializer"/>.</summary>
        FieldInitializerInCreation = 0x43,
        /// <summary>Indicates an <see cref="IPropertyInitializer"/>.</summary>
        PropertyInitializerInCreation = 0x44,
        /// <summary>Indicates an <see cref="IArrayInitializer"/>.</summary>
        ArrayInitializer = 0x45,
        /// <summary>Indicates an <see cref="IVariableDeclaration"/>.</summary>
        VariableDeclaration = 0x46,
        /// <summary>Indicates an <see cref="ISwitchCase"/>.</summary>
        SwitchCase = 0x47,
        /// <summary>Indicates an <see cref="ISingleValueCaseClause"/>.</summary>
        SingleValueCaseClause = 0x48,
        /// <summary>Indicates an <see cref="IRelationalCaseClause"/>.</summary>
        RelationalCaseClause = 0x49,
        /// <summary>Indicates an <see cref="IRangeCaseClause"/>.</summary>
        RangeCaseClause = 0x4a,

        /// <summary>Indicates an <see cref="IParameterInitializer"/>.</summary>
        ParameterInitializerAtDeclaration = 0x4b,
        /// <summary>Indicates an <see cref="IFieldInitializer"/>.</summary>
        FieldInitializerAtDeclaration = 0x4c,
        /// <summary>Indicates an <see cref="IPropertyInitializer"/>.</summary>
        PropertyInitializerAtDeclaration = 0x4d
    }
}
