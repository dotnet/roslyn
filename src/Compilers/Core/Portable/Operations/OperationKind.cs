// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Semantics;

namespace Microsoft.CodeAnalysis
{
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
        /// <summary>Indicates an <see cref="ILabeledStatement"/>.</summary>
        LabeledStatement = 0x7,
        /// <summary>Indicates an <see cref="IBranchStatement"/>.</summary>
        BranchStatement = 0x8,
        /// <summary>Indicates an <see cref="IEmptyStatement"/>.</summary>
        EmptyStatement = 0x9,
        // 0xa open for usage, was IThrowStatement.
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

        LocalFunctionStatement = 0x31,

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
        // Unused 0x107
        /// <summary>Indicates an <see cref="IFieldReferenceExpression"/>.</summary>
        FieldReferenceExpression = 0x108,
        /// <summary>Indicates an <see cref="IMethodBindingExpression"/>.</summary>
        MethodBindingExpression = 0x109,
        /// <summary>Indicates an <see cref="IPropertyReferenceExpression"/>.</summary>
        PropertyReferenceExpression = 0x10a,
        /// <summary>Indicates an <see cref="IEventReferenceExpression"/>.</summary>
        EventReferenceExpression = 0x10c,
        /// <summary>Indicates an <see cref="IUnaryOperatorExpression"/>.</summary>
        UnaryOperatorExpression = 0x10d,
        /// <summary>Indicates an <see cref="IBinaryOperatorExpression"/>.</summary>
        BinaryOperatorExpression = 0x10e,
        /// <summary>Indicates an <see cref="IConditionalExpression"/>.</summary>
        ConditionalExpression = 0x10f,
        /// <summary>Indicates an <see cref="ICoalesceExpression"/>.</summary>
        CoalesceExpression = 0x110,
        /// <summary>Indicates an <see cref="IAnonymousFunctionExpression"/>.</summary>
        AnonymousFunctionExpression = 0x111,
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
        /// <summary>Indicates an <see cref="ISimpleAssignmentExpression"/>.</summary>
        SimpleAssignmentExpression = 0x118,
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
        /// <summary>Indicates an <see cref="IInterpolatedStringExpression"/>.</summary>
        InterpolatedStringExpression = 0x11e,
        /// <summary>Indicates an <see cref="IAnonymousObjectCreationExpression"/>.</summary>
        AnonymousObjectCreationExpression = 0x11f,
        /// <summary>Indicates an <see cref="IObjectOrCollectionInitializerExpression"/>.</summary>
        ObjectOrCollectionInitializerExpression = 0x120,
        /// <summary>Indicates an <see cref="IMemberInitializerExpression"/>.</summary>
        MemberInitializerExpression = 0x121,
        /// <summary>Indicates an <see cref="ICollectionElementInitializerExpression"/>.</summary>
        CollectionElementInitializerExpression = 0x122,
        /// <summary>Indicates an <see cref="INameOfExpression"/>.</summary>
        NameOfExpression = 0x123,
        /// <summary>Indicates an <see cref="ITupleExpression"/>.</summary>
        TupleExpression = 0x124,
        /// <summary>Indicates an <see cref="IDynamicObjectCreationExpression"/>.</summary>
        DynamicObjectCreationExpression = 0x125,
        /// <summary>Indicates an <see cref="IDynamicMemberReferenceExpression"/>.</summary>
        DynamicMemberReferenceExpression = 0x126,

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
        /// <summary>Indicates an <see cref="IIsPatternExpression"/>.</summary>
        IsPatternExpression = 0x205,
        /// <summary>Indicates an <see cref="IIncrementExpression"/>.</summary>
        IncrementExpression = 0x206,
        /// <summary>Indicates an <see cref="IThrowExpression"/>.</summary>
        ThrowExpression = 0x207,

        // Expressions that occur only in Visual Basic.

        /// <summary>Indicates an <see cref="IOmittedArgumentExpression"/>.</summary>
        OmittedArgumentExpression = 0x300,
        // 0x301 was removed, and is available for use.
        /// <summary>Indicates an <see cref="IPlaceholderExpression"/>.</summary>
        PlaceholderExpression = 0x302,

        // Operations that are constituents of statements, expressions, or declarations.


        // Unused 0x400 and 0x402

        /// <summary>Indicates an <see cref="IFieldInitializer"/>.</summary>
        FieldInitializer = 0x401,
        /// <summary>Indicates an <see cref="IPropertyInitializer"/>.</summary>
        PropertyInitializer = 0x403,
        /// <summary>Indicates an <see cref="IParameterInitializer"/>.</summary>
        ParameterInitializer = 0x404,
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

        /// <summary>Indicates an <see cref="IInterpolatedStringText"/>.</summary>
        InterpolatedStringText = 0x40d,
        /// <summary>Indicates an <see cref="IInterpolation"/>.</summary>
        Interpolation = 0x40e,

        /// <summary>Indicates an <see cref="IConstantPattern"/>.</summary>
        ConstantPattern = 0x40f,
        /// <summary>Indicates an <see cref="IDeclarationPattern"/>.</summary>
        DeclarationPattern = 0x410,
        /// <summary>Indicates an <see cref="IPatternCaseClause"/>.</summary>
        PatternCaseClause = 0x411,

        /// <summary>Indicates an <see cref="IDefaultCaseClause"/>.</summary>
        DefaultCaseClause = 0x412,
    }
}
