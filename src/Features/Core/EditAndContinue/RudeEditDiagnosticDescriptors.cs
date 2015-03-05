// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    // TODO (tomat): cleanup and localize

    internal static class RudeEditDiagnosticDescriptors
    {
        private static readonly ImmutableDictionary<RudeEditKind, DiagnosticDescriptor> s_descriptors = new List<KeyValuePair<RudeEditKind, DiagnosticDescriptor>>
        {
            { GetDescriptorPair(RudeEditKind.STMT_MID_DELETE,                           FeaturesResources.EditingOrDeletingBeingExecuted) },
            { GetDescriptorPair(RudeEditKind.STMT_NON_LEAF_DELETE,                      FeaturesResources.EditingOrDeletingNotAtTheTop) },
            { GetDescriptorPair(RudeEditKind.STMT_CTOR_CALL,                            FeaturesResources.EditingOrDeletingConstructorDeclaration) },
            { GetDescriptorPair(RudeEditKind.STMT_FIELD_INIT,                           FeaturesResources.EditingOrDeletingFieldInitializer) },
            { GetDescriptorPair(RudeEditKind.STMT_DELETE,                               FeaturesResources.AnActiveStatementMarkerHasBeenDeleted) },
            { GetDescriptorPair(RudeEditKind.STMT_DELETE_REMAP,                         FeaturesResources.AnActiveStatementMarkerHasBeenDeletedAnd) },
            { GetDescriptorPair(RudeEditKind.STMT_READONLY,                             FeaturesResources.EditingOrCommentingOut) },
            { GetDescriptorPair(RudeEditKind.RUDE_EDIT_INSERT_AROUND,                   FeaturesResources.AddingAAroundAnActiveStatement) },
            { GetDescriptorPair(RudeEditKind.RUDE_EDIT_DELETE_AROUND,                   FeaturesResources.DeletingAAroundAnActiveStatement) },
            { GetDescriptorPair(RudeEditKind.RUDE_NO_ACTIVE_STMT,                       FeaturesResources.ErrorLocatingAnActiveStatement) },
            { GetDescriptorPair(RudeEditKind.RUDE_ACTIVE_STMT_DELETED,                  FeaturesResources.AnActiveStatementHasBeenRemoved) },
            { GetDescriptorPair(RudeEditKind.RUDE_EDIT_AROUND_ACTIVE_STMT,              FeaturesResources.UpdatingAStatementAroundActive) },
            { GetDescriptorPair(RudeEditKind.EXC_HANDLER_ERROR,                         FeaturesResources.ModifyingACatchFinallyHandler) },
            { GetDescriptorPair(RudeEditKind.EXC_FINALLY_ERROR,                         FeaturesResources.ModifyingATryCatchFinally) },
            { GetDescriptorPair(RudeEditKind.EXC_CATCH_ERROR,                           FeaturesResources.ModifyingACatchHandlerAround) },
            { GetDescriptorPair(RudeEditKind.Update,                                    FeaturesResources.UpdatingAWillPrevent) },
            { GetDescriptorPair(RudeEditKind.ModifiersUpdate,                           FeaturesResources.UpdatingTheModifiersOf) },
            { GetDescriptorPair(RudeEditKind.VarianceUpdate,                            FeaturesResources.UpdatingTheVarianceOf) },
            { GetDescriptorPair(RudeEditKind.TypeUpdate,                                FeaturesResources.UpdatingTheTypeOf) },
            { GetDescriptorPair(RudeEditKind.InitializerUpdate,                         FeaturesResources.UpdatingTheInitializerOf) },
            { GetDescriptorPair(RudeEditKind.FixedSizeFieldUpdate,                      FeaturesResources.UpdatingTheSizeOf) },
            { GetDescriptorPair(RudeEditKind.EnumUnderlyingTypeUpdate,                  FeaturesResources.UpdatingTheUnderlyingTypeOf) },
            { GetDescriptorPair(RudeEditKind.BaseTypeOrInterfaceUpdate,                 FeaturesResources.UpdatingTheBaseClassAndOrInterfaceOf) },
            { GetDescriptorPair(RudeEditKind.TypeKindUpdate,                            FeaturesResources.UpdatingTheKindOfType) },
            { GetDescriptorPair(RudeEditKind.AccessorKindUpdate,                        FeaturesResources.UpdatingTheKindOfAccessor) },
            { GetDescriptorPair(RudeEditKind.MethodKindUpdate,                          FeaturesResources.UpdatingTheKindOfMethod) },
            { GetDescriptorPair(RudeEditKind.DeclareAliasUpdate,                        FeaturesResources.UpdatingTheAliasOfDeclareStatement) },
            { GetDescriptorPair(RudeEditKind.DeclareLibraryUpdate,                      FeaturesResources.UpdatingTheLibraryNameOfDeclareStatement) },
            { GetDescriptorPair(RudeEditKind.FieldKindUpdate,                           FeaturesResources.UpdatingTheKindOfField) },
            { GetDescriptorPair(RudeEditKind.Renamed,                                   FeaturesResources.RenamingAWillPrevent) },
            { GetDescriptorPair(RudeEditKind.Insert,                                    FeaturesResources.AddingAWillPreventTheDebugSession) },
            { GetDescriptorPair(RudeEditKind.InsertVirtual,                             FeaturesResources.AddingAbstractOrOverride) },
            { GetDescriptorPair(RudeEditKind.InsertOverridable,                         FeaturesResources.AddingMustOverrideOrOverrides) },
            { GetDescriptorPair(RudeEditKind.InsertExtern,                              FeaturesResources.AddingExternMember) },
            { GetDescriptorPair(RudeEditKind.InsertDllImport,                           FeaturesResources.AddingAnImportedMethod) },
            { GetDescriptorPair(RudeEditKind.InsertOperator,                            FeaturesResources.AddingUserDefinedOperator) },
            { GetDescriptorPair(RudeEditKind.InsertIntoStruct,                          FeaturesResources.AddingInto) },
            { GetDescriptorPair(RudeEditKind.InsertIntoClassWithLayout,                 FeaturesResources.AddingIntoClassWithExplicitOrSequential) },
            { GetDescriptorPair(RudeEditKind.InsertGenericMethod,                       FeaturesResources.AddingAGeneric) },
            { GetDescriptorPair(RudeEditKind.Move,                                      FeaturesResources.MovingAWillPreventTheDebug) },
            { GetDescriptorPair(RudeEditKind.Delete,                                    FeaturesResources.DeletingAWillPrevent) },
            { GetDescriptorPair(RudeEditKind.MethodBodyAdd,                             FeaturesResources.AddingAMethodBodyWillPrevent) },
            { GetDescriptorPair(RudeEditKind.MethodBodyDelete,                          FeaturesResources.DeletingAMethodBodyWillPrevent) },
            { GetDescriptorPair(RudeEditKind.GenericMethodUpdate,                       FeaturesResources.ModifyingAGenericMethodWillPrevent) },
            { GetDescriptorPair(RudeEditKind.GenericMethodTriviaUpdate,                 FeaturesResources.ModifyingTriviaInGenericMethodWillPrevent) },
            { GetDescriptorPair(RudeEditKind.GenericTypeUpdate,                         FeaturesResources.ModifyingAMethodInsideTheContext) },
            { GetDescriptorPair(RudeEditKind.GenericTypeTriviaUpdate,                   FeaturesResources.ModifyingTriviaInMethodInsideTheContext) },
            { GetDescriptorPair(RudeEditKind.GenericTypeInitializerUpdate,              FeaturesResources.ModifyingTheInitializerInGenericType) },
            { GetDescriptorPair(RudeEditKind.PartialTypeInitializerUpdate,              FeaturesResources.ModifyingTheInitializerInPartialType) },
            { GetDescriptorPair(RudeEditKind.StackAllocUpdate,                          FeaturesResources.ModifyingAWhichContainsStackalloc) },
            { GetDescriptorPair(RudeEditKind.ExperimentalFeaturesEnabled,               FeaturesResources.ModifyingAFileWithExperimentalFeaturesEnabled) },
            { GetDescriptorPair(RudeEditKind.AwaitStatementUpdate,                      FeaturesResources.UpdatingAStatementContainingAwaitExpression) },
            { GetDescriptorPair(RudeEditKind.ChangingConstructorVisibility,             FeaturesResources.ChangingVisibilityOfConstructor) },
            { GetDescriptorPair(RudeEditKind.CapturingVariable,                         FeaturesResources.CapturingVariable) },
            { GetDescriptorPair(RudeEditKind.NotCapturingVariable,                      FeaturesResources.NotCapturingVariable) },
            { GetDescriptorPair(RudeEditKind.DeletingCapturedVariable,                  FeaturesResources.DeletingCapturedVariable) },
            { GetDescriptorPair(RudeEditKind.ChangingCapturedVariableType,              FeaturesResources.ChangingCapturedVariableType) },            
            { GetDescriptorPair(RudeEditKind.ChangingCapturedVariableScope,             FeaturesResources.ChangingCapturedVariableScope) },
            { GetDescriptorPair(RudeEditKind.ChangingLambdaParameters,                  FeaturesResources.ChangingLambdaParameters) },
            { GetDescriptorPair(RudeEditKind.ChangingLambdaReturnType,                  FeaturesResources.ChangingLambdaReturnType) },
            { GetDescriptorPair(RudeEditKind.AccessingCapturedVariableInLambda,         FeaturesResources.AccessingCapturedVariableInLambda) },
            { GetDescriptorPair(RudeEditKind.NotAccessingCapturedVariableInLambda,      FeaturesResources.NotAccessingCapturedVariableInLambda) },
            { GetDescriptorPair(RudeEditKind.InsertLambdaWithMultiScopeCapture,         FeaturesResources.InsertLambdaWithMultiScopeCapture) },
            { GetDescriptorPair(RudeEditKind.DeleteLambdaWithMultiScopeCapture,         FeaturesResources.DeleteLambdaWithMultiScopeCapture) },
            { GetDescriptorPair(RudeEditKind.RUDE_EDIT_ANON_METHOD,                     FeaturesResources.ModifyingAWhichContainsAnonymousMethod) },
            { GetDescriptorPair(RudeEditKind.RUDE_EDIT_YIELD,                           FeaturesResources.ModifyingAWhichContainsYield) },
            { GetDescriptorPair(RudeEditKind.RUDE_EDIT_FIELD_ANON_METH,                 FeaturesResources.ModifyingAInitializerWithAnonymousMethod) },
            { GetDescriptorPair(RudeEditKind.RUDE_EDIT_NON_USER_STMT,                   FeaturesResources.ModifyingAContainingAnActiveStatement) },
            { GetDescriptorPair(RudeEditKind.FIELD_WITH_ANON_METHOD,                    FeaturesResources.ConstructorCannotBeModifiedAnonymousMethod) },
            { GetDescriptorPair(RudeEditKind.RUDE_EDIT_LAMBDA_EXPRESSION,               FeaturesResources.ModifyingAWhichContainsLambda) },
            { GetDescriptorPair(RudeEditKind.RUDE_EDIT_QUERY_EXPRESSION,                FeaturesResources.ModifyingAWhichContainsQuery) },
            { GetDescriptorPair(RudeEditKind.RUDE_EDIT_ANONYMOUS_TYPE,                  FeaturesResources.ModifyingAWhichContainsAnonymousType) },
            { GetDescriptorPair(RudeEditKind.FIELD_WITH_LAMBDA,                         FeaturesResources.ConstructorCannotBeModifiedLambda) },
            { GetDescriptorPair(RudeEditKind.FIELD_WITH_QUERY,                          FeaturesResources.ConstructorCannotBeModifiedQuery) },
            { GetDescriptorPair(RudeEditKind.RUDE_EDIT_FIELD_LAMBDA,                    FeaturesResources.ModifyingAInitializerWithLambda) },
            { GetDescriptorPair(RudeEditKind.RUDE_EDIT_FIELD_QUERY,                     FeaturesResources.ModifyingAInitializerWithQuery) },
            { GetDescriptorPair(RudeEditKind.FIELD_WITH_ANON_TYPE,                      FeaturesResources.ConstructorCannotBeModifiedAnonymousType) },
            { GetDescriptorPair(RudeEditKind.RUDE_EDIT_FIELD_ANON_TYPE,                 FeaturesResources.ModifyingAInitializerWithAnonymousType) },
            { GetDescriptorPair(RudeEditKind.RUDE_EDIT_DYNAMIC_INVOCATION,              FeaturesResources.ModifyingAWhichContainsDynamic) },
            { GetDescriptorPair(RudeEditKind.RUDE_EDIT_NOPIA_USAGE,                     FeaturesResources.ModifyingAWhichContainsEmbeddedInterop) },
            { GetDescriptorPair(RudeEditKind.RUDE_EDIT_MODIFY_ANON_METHOD,              FeaturesResources.ModifyingAStatementWhichContainsAnonymousMethod) },
            { GetDescriptorPair(RudeEditKind.RUDE_EDIT_ADD_ANON_METHOD,                 FeaturesResources.AddingAStatementWhichContainsAnAnonymousMethod) },
            { GetDescriptorPair(RudeEditKind.RUDE_EDIT_DELETE_ANON_METHOD,              FeaturesResources.DeletingAStatementWhichContainsAnonymousMethod) },
            { GetDescriptorPair(RudeEditKind.RUDE_EDIT_MOVE_ANON_METHOD,                FeaturesResources.MovingAStatementWhichContainsAnonymousMethod) },
            { GetDescriptorPair(RudeEditKind.RUDE_EDIT_LOCAL_USED_IN_ANON_METHOD,       FeaturesResources.ModifyingLocalReferencedInAnonymousMethod) },
            { GetDescriptorPair(RudeEditKind.RUDE_EDIT_MODIFY_LAMBDA_EXPRESSION,        FeaturesResources.ModifyingAStatementWhichContainsLambda) },
            { GetDescriptorPair(RudeEditKind.RUDE_EDIT_ADD_LAMBDA_EXPRESSION,           FeaturesResources.AddingAStatementWhichContainsALambda) },
            { GetDescriptorPair(RudeEditKind.RUDE_EDIT_DELETE_LAMBDA_EXPRESSION,        FeaturesResources.DeletingAStatementWhichContainsLambda) },
            { GetDescriptorPair(RudeEditKind.RUDE_EDIT_MOVE_LAMBDA_EXPRESSION,          FeaturesResources.MovingAStatementWhichContainsLambda) },
            { GetDescriptorPair(RudeEditKind.RUDE_EDIT_LOCAL_USED_IN_LAMBDA_EXPRESSION, FeaturesResources.ModifyingLocalReferencedInLambda) },
            { GetDescriptorPair(RudeEditKind.RUDE_EDIT_MODIFY_QUERY_EXPRESSION,         FeaturesResources.ModifyingAStatementWhichContainsQuery) },
            { GetDescriptorPair(RudeEditKind.RUDE_EDIT_ADD_QUERY_EXPRESSION,            FeaturesResources.AddingAStatementWhichContainsAQuery) },
            { GetDescriptorPair(RudeEditKind.RUDE_EDIT_DELETE_QUERY_EXPRESSION,         FeaturesResources.DeletingAStatementWhichContainsQuery) },
            { GetDescriptorPair(RudeEditKind.RUDE_EDIT_MOVE_QUERY_EXPRESSION,           FeaturesResources.MovingAStatementWhichContainsQuery) },
            { GetDescriptorPair(RudeEditKind.RUDE_EDIT_LOCAL_USED_IN_QUERY_EXPRESSION,  FeaturesResources.ModifyingLocalReferencedInQuery) },
            { GetDescriptorPair(RudeEditKind.RUDE_EDIT_MODIFY_ANONYMOUS_TYPE,           FeaturesResources.ModifyingAStatementWhichContainsAnonymousType) },
            { GetDescriptorPair(RudeEditKind.RUDE_EDIT_ADD_ANONYMOUS_TYPE,              FeaturesResources.AddingAStatementWhichContainsAnAnonymousType) },
            { GetDescriptorPair(RudeEditKind.RUDE_EDIT_DELETE_ANONYMOUS_TYPE,           FeaturesResources.DeletingAStatementWhichContainsAnonymousType) },
            { GetDescriptorPair(RudeEditKind.RUDE_EDIT_MOVE_ANONYMOUS_TYPE,             FeaturesResources.MovingAStatementWhichContainsAnonymousType) },
            { GetDescriptorPair(RudeEditKind.AsyncMethodUpdate,                         FeaturesResources.ModifyingAnAsynchronous) },
            { GetDescriptorPair(RudeEditKind.AsyncMethodTriviaUpdate,                   FeaturesResources.ModifyingTriviaOfAnAsynchronous) },
            { GetDescriptorPair(RudeEditKind.ActiveStatementUpdate,                     FeaturesResources.UpdatingAnActiveStatement) },
            { GetDescriptorPair(RudeEditKind.ActiveStatementLambdaRemoved,              FeaturesResources.RemovingThatContainsActiveStatement) },
            { GetDescriptorPair(RudeEditKind.RUDE_EDIT_ADD_NEW_FILE,                    FeaturesResources.AddingANewFile) },

            // VB specific,
            { GetDescriptorPair(RudeEditKind.HandlesClauseUpdate,                       FeaturesResources.UpdatingTheHandlesClause) },
            { GetDescriptorPair(RudeEditKind.ImplementsClauseUpdate,                    FeaturesResources.UpdatingTheImplementsClause) },
            { GetDescriptorPair(RudeEditKind.ConstraintKindUpdate,                      FeaturesResources.ChangingTheConstraintFromTo) },
            { GetDescriptorPair(RudeEditKind.RUDE_EDIT_ADD_HANDLES_CLAUSE,              FeaturesResources.AddingAWithTheHandlesClause) },
        }.ToImmutableDictionary();

        private static KeyValuePair<RudeEditKind, DiagnosticDescriptor> GetDescriptorPair(RudeEditKind kind, string message)
        {
            return new KeyValuePair<RudeEditKind, DiagnosticDescriptor>(kind,
                                                                        new DiagnosticDescriptor(id: "ENC" + ((int)kind).ToString("0000"),
                                                                            title: message, // TODO: come up with real titles.
                                                                            messageFormat: message,
                                                                            category: DiagnosticCategory.EditAndContinue,
                                                                            defaultSeverity: DiagnosticSeverity.Error,
                                                                            isEnabledByDefault: true,
                                                                            customTags: DiagnosticCustomTags.EditAndContinue));
        }

        internal static ImmutableArray<DiagnosticDescriptor> AllDescriptors
        {
            get
            {
                return s_descriptors.Values.ToImmutableArray();
            }
        }

        internal static DiagnosticDescriptor GetDescriptor(RudeEditKind kind)
        {
            return s_descriptors[kind];
        }
    }
}
