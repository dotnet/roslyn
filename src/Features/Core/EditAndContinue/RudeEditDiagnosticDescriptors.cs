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
            { GetDescriptorPair(RudeEditKind.InsertAroundActiveStatement,               FeaturesResources.AddingAAroundAnActiveStatement) },
            { GetDescriptorPair(RudeEditKind.DeleteAroundActiveStatement,               FeaturesResources.DeletingAAroundAnActiveStatement) },
            { GetDescriptorPair(RudeEditKind.DeleteActiveStatement,                     FeaturesResources.AnActiveStatementHasBeenRemoved) },
            { GetDescriptorPair(RudeEditKind.UpdateAroundActiveStatement,               FeaturesResources.UpdatingAStatementAroundActive) },
            { GetDescriptorPair(RudeEditKind.UpdateExceptionHandlerOfActiveTry,         FeaturesResources.ModifyingACatchFinallyHandler) },
            { GetDescriptorPair(RudeEditKind.UpdateTryOrCatchWithActiveFinally,         FeaturesResources.ModifyingATryCatchFinally) },
            { GetDescriptorPair(RudeEditKind.UpdateCatchHandlerAroundActiveStatement,   FeaturesResources.ModifyingACatchHandlerAround) },
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
            { GetDescriptorPair(RudeEditKind.InsertConstructorToTypeWithInitializersWithLambdas,  FeaturesResources.InsertConstructorToTypeWithInitializersWithLambdas) },
            { GetDescriptorPair(RudeEditKind.RenamingCapturedVariable,                  FeaturesResources.RenamingCapturedVariable) },
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
            { GetDescriptorPair(RudeEditKind.ChangingQueryLambdaType,                   FeaturesResources.ChangingQueryLambdaType) },
            { GetDescriptorPair(RudeEditKind.AccessingCapturedVariableInLambda,         FeaturesResources.AccessingCapturedVariableInLambda) },
            { GetDescriptorPair(RudeEditKind.NotAccessingCapturedVariableInLambda,      FeaturesResources.NotAccessingCapturedVariableInLambda) },
            { GetDescriptorPair(RudeEditKind.InsertLambdaWithMultiScopeCapture,         FeaturesResources.InsertLambdaWithMultiScopeCapture) },
            { GetDescriptorPair(RudeEditKind.DeleteLambdaWithMultiScopeCapture,         FeaturesResources.DeleteLambdaWithMultiScopeCapture) },
            { GetDescriptorPair(RudeEditKind.ActiveStatementUpdate,                     FeaturesResources.UpdatingAnActiveStatement) },
            { GetDescriptorPair(RudeEditKind.ActiveStatementLambdaRemoved,              FeaturesResources.RemovingThatContainsActiveStatement) },
            // TODO: change the error message to better explain what's going on
            { GetDescriptorPair(RudeEditKind.PartiallyExecutedActiveStatementUpdate,    FeaturesResources.UpdatingAnActiveStatement) },
            { GetDescriptorPair(RudeEditKind.PartiallyExecutedActiveStatementDelete,    FeaturesResources.AnActiveStatementHasBeenRemoved) },
            { GetDescriptorPair(RudeEditKind.InsertFile,                                FeaturesResources.AddingANewFile) },
            { GetDescriptorPair(RudeEditKind.UpdatingStateMachineMethodAroundActiveStatement, FeaturesResources.UpdatingStateMachineMethodAroundActive) },

            { GetDescriptorPair(RudeEditKind.RUDE_EDIT_COMPLEX_QUERY_EXPRESSION,        FeaturesResources.ModifyingAWhichContainsComplexQuery) },

            // VB specific,
            { GetDescriptorPair(RudeEditKind.HandlesClauseUpdate,                       FeaturesResources.UpdatingTheHandlesClause) },
            { GetDescriptorPair(RudeEditKind.ImplementsClauseUpdate,                    FeaturesResources.UpdatingTheImplementsClause) },
            { GetDescriptorPair(RudeEditKind.ConstraintKindUpdate,                      FeaturesResources.ChangingTheConstraintFromTo) },
            { GetDescriptorPair(RudeEditKind.InsertHandlesClause,                       FeaturesResources.AddingAWithTheHandlesClause) },
            { GetDescriptorPair(RudeEditKind.UpdateStaticLocal,                         FeaturesResources.ModifyingAWhichContainsStaticLocal) },
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
