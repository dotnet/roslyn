// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    // TELEMETRY: DO NOT MODIFY ANY ENUM VALUES OF THIS ENUM.
    // IT WILL BREAK OUR SQM VARIABLE MAPPINGS.

    internal enum RudeEditKind : ushort
    {
        None = 0,

        ActiveStatementUpdate = 1,
        ActiveStatementLambdaRemoved = 2,

        Update = 3,
        ModifiersUpdate = 4,
        HandlesClauseUpdate = 5,
        ImplementsClauseUpdate = 6,
        VarianceUpdate = 7,
        FieldKindUpdate = 8,
        TypeUpdate = 9,
        //ConstraintKindUpdate = 10,
        InitializerUpdate = 11,
        FixedSizeFieldUpdate = 12,
        EnumUnderlyingTypeUpdate = 13,
        BaseTypeOrInterfaceUpdate = 14,
        TypeKindUpdate = 15,
        AccessorKindUpdate = 16,
        //MethodKindUpdate = 17,
        DeclareLibraryUpdate = 18,
        DeclareAliasUpdate = 19,
        Renamed = 20,
        Insert = 21,
        // InsertNonPrivate = 22,
        InsertVirtual = 23,
        InsertOverridable = 24,
        InsertExtern = 25,
        InsertOperator = 26,
        // InsertNonPublicConstructor = 27,
        InsertGenericMethod = 28,
        InsertDllImport = 29,
        InsertIntoStruct = 30,
        InsertIntoClassWithLayout = 31,
        Move = 32,
        Delete = 33,
        // MethodBodyAdd = 34,
        // MethodBodyDelete = 35,
        GenericMethodUpdate = 36,
        GenericMethodTriviaUpdate = 37,
        GenericTypeUpdate = 38,
        GenericTypeTriviaUpdate = 39,
        GenericTypeInitializerUpdate = 40,
        // PartialTypeInitializerUpdate = 41,
        // AsyncMethodUpdate = 42,
        // AsyncMethodTriviaUpdate = 43,
        StackAllocUpdate = 44,

        ExperimentalFeaturesEnabled = 45,

        AwaitStatementUpdate = 46,
        ChangingAccessibility = 47,

        CapturingVariable = 48,
        NotCapturingVariable = 49,
        DeletingCapturedVariable = 50,
        ChangingCapturedVariableType = 51,
        ChangingCapturedVariableScope = 52,
        ChangingLambdaParameters = 53,
        ChangingLambdaReturnType = 54,
        AccessingCapturedVariableInLambda = 55,
        NotAccessingCapturedVariableInLambda = 56,
        InsertLambdaWithMultiScopeCapture = 57,
        DeleteLambdaWithMultiScopeCapture = 58,
        ChangingQueryLambdaType = 59,

        InsertAroundActiveStatement = 60,
        DeleteAroundActiveStatement = 61,
        DeleteActiveStatement = 62,
        UpdateAroundActiveStatement = 63,
        UpdateExceptionHandlerOfActiveTry = 64,
        UpdateTryOrCatchWithActiveFinally = 65,
        UpdateCatchHandlerAroundActiveStatement = 66,
        UpdateStaticLocal = 67,

        InsertConstructorToTypeWithInitializersWithLambdas = 68,
        RenamingCapturedVariable = 69,

        InsertHandlesClause = 70,
        InsertFile = 71,
        PartiallyExecutedActiveStatementUpdate = 72,
        PartiallyExecutedActiveStatementDelete = 73,
        UpdatingStateMachineMethodAroundActiveStatement = 74,
        UpdatingStateMachineMethodMissingAttribute = 75,

        SwitchBetweenLambdaAndLocalFunction = 76,
        //RefStruct = 77,
        //ReadOnlyStruct = 78,
        //ReadOnlyReferences = 79,

        InternalError = 80,

        InsertMethodWithExplicitInterfaceSpecifier = 81,
        InsertIntoInterface = 82,
        InsertLocalFunctionIntoInterfaceMethod = 83,
        //SwitchExpressionUpdate = 84,
        ChangingFromAsynchronousToSynchronous = 85,
        ChangingStateMachineShape = 86,

        // Chagned from 0x103 in 16.1 and from 82 to 87 in 16.8
        ComplexQueryExpression = 87,

        MemberBodyInternalError = 88,
        SourceFileTooBig = 89,
        MemberBodyTooBig = 90,
        InsertIntoGenericType = 91,

        ImplementRecordParameterAsReadOnly = 92,
        ImplementRecordParameterWithSet = 93,
        //AddRecordPositionalParameter = 94,
        //DeleteRecordPositionalParameter = 95,
        ExplicitRecordMethodParameterNamesMustMatch = 96,
        NotSupportedByRuntime = 97,
        MakeMethodAsyncNotSupportedByRuntime = 98,
        MakeMethodIteratorNotSupportedByRuntime = 99,
        InsertNotSupportedByRuntime = 100,
        ChangingAttributesNotSupportedByRuntime = 101,
        ChangeImplicitMainReturnType = 102,
        ChangingParameterTypes = 103,
        ChangingTypeParameters = 104,
        ChangingConstraints = 105,
        ChangingReloadableTypeNotSupportedByRuntime = 106,
        RenamingNotSupportedByRuntime = 107,
        ChangingNonCustomAttribute = 108
    }
}
