// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
// < auto-generated />
using System;
using System.ComponentModel;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;
namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// All of the kinds of operations, including statements and expressions.
    /// </summary>
    public enum OperationKind
    {
        /// <summary>Indicates an <see cref="IOperation"/> for a construct that is not implemented yet.</summary>
        None = 0x0,
        /// <summary>Indicates an <see cref="IInvalidOperation"/>.</summary>
        Invalid = 0x1,
        /// <summary>Indicates an <see cref="IBlockOperation"/>.</summary>
        Block = 0x2,
        /// <summary>Indicates an <see cref="IVariableDeclarationGroupOperation"/>.</summary>
        VariableDeclarationGroup = 0x3,
        /// <summary>Indicates an <see cref="ISwitchOperation"/>.</summary>
        Switch = 0x4,
        /// <summary>Indicates an <see cref="ILoopOperation"/>. This is further differentiated by <see cref="ILoopOperation.LoopKind"/>.</summary>
        Loop = 0x5,
        /// <summary>Indicates an <see cref="ILabeledOperation"/>.</summary>
        Labeled = 0x6,
        /// <summary>Indicates an <see cref="IBranchOperation"/>.</summary>
        Branch = 0x7,
        /// <summary>Indicates an <see cref="IEmptyOperation"/>.</summary>
        Empty = 0x8,
        /// <summary>Indicates an <see cref="IReturnOperation"/>.</summary>
        Return = 0x9,
        /// <summary>Indicates an <see cref="IReturnOperation"/>. This has yield break semantics.</summary>
        YieldBreak = 0xa,
        /// <summary>Indicates an <see cref="ILockOperation"/>.</summary>
        Lock = 0xb,
        /// <summary>Indicates an <see cref="ITryOperation"/>.</summary>
        Try = 0xc,
        /// <summary>Indicates an <see cref="IUsingOperation"/>.</summary>
        Using = 0xd,
        /// <summary>Indicates an <see cref="IReturnOperation"/>. This has yield return semantics.</summary>
        YieldReturn = 0xe,
        /// <summary>Indicates an <see cref="IExpressionStatementOperation"/>.</summary>
        ExpressionStatement = 0xf,
        /// <summary>Indicates an <see cref="ILocalFunctionOperation"/>.</summary>
        LocalFunction = 0x10,
        /// <summary>Indicates an <see cref="IStopOperation"/>.</summary>
        Stop = 0x11,
        /// <summary>Indicates an <see cref="IEndOperation"/>.</summary>
        End = 0x12,
        /// <summary>Indicates an <see cref="IRaiseEventOperation"/>.</summary>
        RaiseEvent = 0x13,
        /// <summary>Indicates an <see cref="ILiteralOperation"/>.</summary>
        Literal = 0x14,
        /// <summary>Indicates an <see cref="IConversionOperation"/>.</summary>
        Conversion = 0x15,
        /// <summary>Indicates an <see cref="IInvocationOperation"/>.</summary>
        Invocation = 0x16,
        /// <summary>Indicates an <see cref="IArrayElementReferenceOperation"/>.</summary>
        ArrayElementReference = 0x17,
        /// <summary>Indicates an <see cref="ILocalReferenceOperation"/>.</summary>
        LocalReference = 0x18,
        /// <summary>Indicates an <see cref="IParameterReferenceOperation"/>.</summary>
        ParameterReference = 0x19,
        /// <summary>Indicates an <see cref="IFieldReferenceOperation"/>.</summary>
        FieldReference = 0x1a,
        /// <summary>Indicates an <see cref="IMethodReferenceOperation"/>.</summary>
        MethodReference = 0x1b,
        /// <summary>Indicates an <see cref="IPropertyReferenceOperation"/>.</summary>
        PropertyReference = 0x1c,
        // Unused: 1d
        /// <summary>Indicates an <see cref="IEventReferenceOperation"/>.</summary>
        EventReference = 0x1e,
        /// <summary>Indicates an <see cref="IUnaryOperation"/>.</summary>
        Unary = 0x1f,
        /// <summary>Indicates an <see cref="IUnaryOperation"/>. Use <see cref="Unary"/> instead.</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        UnaryOperator = 0x1f,
        /// <summary>Indicates an <see cref="IBinaryOperation"/>.</summary>
        Binary = 0x20,
        /// <summary>Indicates an <see cref="IBinaryOperation"/>. Use <see cref="Binary"/> instead.</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        BinaryOperator = 0x20,
        /// <summary>Indicates an <see cref="IConditionalOperation"/>.</summary>
        Conditional = 0x21,
        /// <summary>Indicates an <see cref="ICoalesceOperation"/>.</summary>
        Coalesce = 0x22,
        /// <summary>Indicates an <see cref="IAnonymousFunctionOperation"/>.</summary>
        AnonymousFunction = 0x23,
        /// <summary>Indicates an <see cref="IObjectCreationOperation"/>.</summary>
        ObjectCreation = 0x24,
        /// <summary>Indicates an <see cref="ITypeParameterObjectCreationOperation"/>.</summary>
        TypeParameterObjectCreation = 0x25,
        /// <summary>Indicates an <see cref="IArrayCreationOperation"/>.</summary>
        ArrayCreation = 0x26,
        /// <summary>Indicates an <see cref="IInstanceReferenceOperation"/>.</summary>
        InstanceReference = 0x27,
        /// <summary>Indicates an <see cref="IIsTypeOperation"/>.</summary>
        IsType = 0x28,
        /// <summary>Indicates an <see cref="IAwaitOperation"/>.</summary>
        Await = 0x29,
        /// <summary>Indicates an <see cref="ISimpleAssignmentOperation"/>.</summary>
        SimpleAssignment = 0x2a,
        /// <summary>Indicates an <see cref="ICompoundAssignmentOperation"/>.</summary>
        CompoundAssignment = 0x2b,
        /// <summary>Indicates an <see cref="IParenthesizedOperation"/>.</summary>
        Parenthesized = 0x2c,
        /// <summary>Indicates an <see cref="IEventAssignmentOperation"/>.</summary>
        EventAssignment = 0x2d,
        /// <summary>Indicates an <see cref="IConditionalAccessOperation"/>.</summary>
        ConditionalAccess = 0x2e,
        /// <summary>Indicates an <see cref="IConditionalAccessInstanceOperation"/>.</summary>
        ConditionalAccessInstance = 0x2f,
        /// <summary>Indicates an <see cref="IInterpolatedStringOperation"/>.</summary>
        InterpolatedString = 0x30,
        /// <summary>Indicates an <see cref="IAnonymousObjectCreationOperation"/>.</summary>
        AnonymousObjectCreation = 0x31,
        /// <summary>Indicates an <see cref="IObjectOrCollectionInitializerOperation"/>.</summary>
        ObjectOrCollectionInitializer = 0x32,
        /// <summary>Indicates an <see cref="IMemberInitializerOperation"/>.</summary>
        MemberInitializer = 0x33,
        /// <summary>Indicates an <see cref="ICollectionElementInitializerOperation"/>.</summary>
        [Obsolete("ICollectionElementInitializerOperation has been replaced with " + nameof(IInvocationOperation) + " and " + nameof(IDynamicInvocationOperation), error: true)]
        CollectionElementInitializer = 0x34,
        /// <summary>Indicates an <see cref="INameOfOperation"/>.</summary>
        NameOf = 0x35,
        /// <summary>Indicates an <see cref="ITupleOperation"/>.</summary>
        Tuple = 0x36,
        /// <summary>Indicates an <see cref="IDynamicObjectCreationOperation"/>.</summary>
        DynamicObjectCreation = 0x37,
        /// <summary>Indicates an <see cref="IDynamicMemberReferenceOperation"/>.</summary>
        DynamicMemberReference = 0x38,
        /// <summary>Indicates an <see cref="IDynamicInvocationOperation"/>.</summary>
        DynamicInvocation = 0x39,
        /// <summary>Indicates an <see cref="IDynamicIndexerAccessOperation"/>.</summary>
        DynamicIndexerAccess = 0x3a,
        /// <summary>Indicates an <see cref="ITranslatedQueryOperation"/>.</summary>
        TranslatedQuery = 0x3b,
        /// <summary>Indicates an <see cref="IDelegateCreationOperation"/>.</summary>
        DelegateCreation = 0x3c,
        /// <summary>Indicates an <see cref="IDefaultValueOperation"/>.</summary>
        DefaultValue = 0x3d,
        /// <summary>Indicates an <see cref="ITypeOfOperation"/>.</summary>
        TypeOf = 0x3e,
        /// <summary>Indicates an <see cref="ISizeOfOperation"/>.</summary>
        SizeOf = 0x3f,
        /// <summary>Indicates an <see cref="IAddressOfOperation"/>.</summary>
        AddressOf = 0x40,
        /// <summary>Indicates an <see cref="IIsPatternOperation"/>.</summary>
        IsPattern = 0x41,
        /// <summary>Indicates an <see cref="IIncrementOrDecrementOperation"/>. This is used as an increment operator</summary>
        Increment = 0x42,
        /// <summary>Indicates an <see cref="IThrowOperation"/>.</summary>
        Throw = 0x43,
        /// <summary>Indicates an <see cref="IIncrementOrDecrementOperation"/>. This is used as an decrement operator</summary>
        Decrement = 0x44,
        /// <summary>Indicates an <see cref="IDeconstructionAssignmentOperation"/>.</summary>
        DeconstructionAssignment = 0x45,
        /// <summary>Indicates an <see cref="IDeclarationExpressionOperation"/>.</summary>
        DeclarationExpression = 0x46,
        /// <summary>Indicates an <see cref="IOmittedArgumentOperation"/>.</summary>
        OmittedArgument = 0x47,
        /// <summary>Indicates an <see cref="IFieldInitializerOperation"/>.</summary>
        FieldInitializer = 0x48,
        /// <summary>Indicates an <see cref="IVariableInitializerOperation"/>.</summary>
        VariableInitializer = 0x49,
        /// <summary>Indicates an <see cref="IPropertyInitializerOperation"/>.</summary>
        PropertyInitializer = 0x4a,
        /// <summary>Indicates an <see cref="IParameterInitializerOperation"/>.</summary>
        ParameterInitializer = 0x4b,
        /// <summary>Indicates an <see cref="IArrayInitializerOperation"/>.</summary>
        ArrayInitializer = 0x4c,
        /// <summary>Indicates an <see cref="IVariableDeclaratorOperation"/>.</summary>
        VariableDeclarator = 0x4d,
        /// <summary>Indicates an <see cref="IVariableDeclarationOperation"/>.</summary>
        VariableDeclaration = 0x4e,
        /// <summary>Indicates an <see cref="IArgumentOperation"/>.</summary>
        Argument = 0x4f,
        /// <summary>Indicates an <see cref="ICatchClauseOperation"/>.</summary>
        CatchClause = 0x50,
        /// <summary>Indicates an <see cref="ISwitchCaseOperation"/>.</summary>
        SwitchCase = 0x51,
        /// <summary>Indicates an <see cref="ICaseClauseOperation"/>. This is further differentiated by <see cref="ICaseClauseOperation.CaseKind"/>.</summary>
        CaseClause = 0x52,
        /// <summary>Indicates an <see cref="IInterpolatedStringTextOperation"/>.</summary>
        InterpolatedStringText = 0x53,
        /// <summary>Indicates an <see cref="IInterpolationOperation"/>.</summary>
        Interpolation = 0x54,
        /// <summary>Indicates an <see cref="IConstantPatternOperation"/>.</summary>
        ConstantPattern = 0x55,
        /// <summary>Indicates an <see cref="IDeclarationPatternOperation"/>.</summary>
        DeclarationPattern = 0x56,
        /// <summary>Indicates an <see cref="ITupleBinaryOperation"/>.</summary>
        TupleBinary = 0x57,
        /// <summary>Indicates an <see cref="ITupleBinaryOperation"/>. Use <see cref="TupleBinary"/> instead.</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        TupleBinaryOperator = 0x57,
        /// <summary>Indicates an <see cref="IMethodBodyOperation"/>.</summary>
        MethodBody = 0x58,
        /// <summary>Indicates an <see cref="IMethodBodyOperation"/>. Use <see cref="MethodBody"/> instead.</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        MethodBodyOperation = 0x58,
        /// <summary>Indicates an <see cref="IConstructorBodyOperation"/>.</summary>
        ConstructorBody = 0x59,
        /// <summary>Indicates an <see cref="IConstructorBodyOperation"/>. Use <see cref="ConstructorBody"/> instead.</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        ConstructorBodyOperation = 0x59,
        /// <summary>Indicates an <see cref="IDiscardOperation"/>.</summary>
        Discard = 0x5a,
        /// <summary>Indicates an <see cref="IFlowCaptureOperation"/>.</summary>
        FlowCapture = 0x5b,
        /// <summary>Indicates an <see cref="IFlowCaptureReferenceOperation"/>.</summary>
        FlowCaptureReference = 0x5c,
        /// <summary>Indicates an <see cref="IIsNullOperation"/>.</summary>
        IsNull = 0x5d,
        /// <summary>Indicates an <see cref="ICaughtExceptionOperation"/>.</summary>
        CaughtException = 0x5e,
        /// <summary>Indicates an <see cref="IStaticLocalInitializationSemaphoreOperation"/>.</summary>
        StaticLocalInitializationSemaphore = 0x5f,
        /// <summary>Indicates an <see cref="IFlowAnonymousFunctionOperation"/>.</summary>
        FlowAnonymousFunction = 0x60,
        /// <summary>Indicates an <see cref="ICoalesceAssignmentOperation"/>.</summary>
        CoalesceAssignment = 0x61,
        // Unused: 62
        /// <summary>Indicates an <see cref="IRangeOperation"/>.</summary>
        Range = 0x63,
        // Unused: 64
        /// <summary>Indicates an <see cref="IReDimOperation"/>.</summary>
        ReDim = 0x65,
        /// <summary>Indicates an <see cref="IReDimClauseOperation"/>.</summary>
        ReDimClause = 0x66,
        /// <summary>Indicates an <see cref="IRecursivePatternOperation"/>.</summary>
        RecursivePattern = 0x67,
        /// <summary>Indicates an <see cref="IDiscardPatternOperation"/>.</summary>
        DiscardPattern = 0x68,
        /// <summary>Indicates an <see cref="ISwitchExpressionOperation"/>.</summary>
        SwitchExpression = 0x69,
        /// <summary>Indicates an <see cref="ISwitchExpressionArmOperation"/>.</summary>
        SwitchExpressionArm = 0x6a,
        /// <summary>Indicates an <see cref="IPropertySubpatternOperation"/>.</summary>
        PropertySubpattern = 0x6b,
        /// <summary>Indicates an <see cref="IUsingVariableDeclarationOperation"/>.</summary>
        UsingVariableDeclaration = 0x6c,
    }
}
