// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class AsyncRewriter : StateMachineRewriter
    {
        /// <summary>
        /// This rewriter rewrites an async-iterator method using runtime-async lowering.
        /// Similar to AsyncIteratorRewriter but generates simpler MoveNextAsync without a builder field.
        /// </summary>
        private sealed class RuntimeAsyncIteratorRewriter : AsyncIteratorRewriter
        {
            internal RuntimeAsyncIteratorRewriter(
                BoundStatement body,
                MethodSymbol method,
                int methodOrdinal,
                AsyncStateMachine stateMachineType,
                ArrayBuilder<StateMachineStateDebugInfo> stateMachineStateDebugInfoBuilder,
                VariableSlotAllocator slotAllocatorOpt,
                TypeCompilationState compilationState,
                BindingDiagnosticBag diagnostics)
                : base(body, method, methodOrdinal, stateMachineType, stateMachineStateDebugInfoBuilder, slotAllocatorOpt, compilationState, diagnostics)
            {
            }

            protected override void GenerateControlFields()
            {
                // For runtime-async iterators, we don't need a builder field
                // The runtime handles the async state machine
                
                // Generate state field
                stateField = F.StateMachineField(F.SpecialType(SpecialType.System_Int32), GeneratedNames.MakeStateMachineStateFieldName(), isPublic: true);
                
                NamedTypeSymbol boolType = F.SpecialType(SpecialType.System_Boolean);
                
                // Add a field: ManualResetValueTaskSourceCore<bool> promiseOfValueOrEnd
                _promiseOfValueOrEndField = F.StateMachineField(
                    F.WellKnownType(WellKnownType.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T).Construct(boolType),
                    GeneratedNames.MakeAsyncIteratorPromiseOfValueOrEndFieldName(), isPublic: true);
                
                // the element type may contain method type parameters, which are now alpha-renamed into type parameters of the generated class
                TypeSymbol elementType = ((AsyncStateMachine)stateMachineType).IteratorElementType;
                
                // Add a field: T current
                _currentField = F.StateMachineField(elementType, GeneratedNames.MakeIteratorCurrentFieldName());
                
                // Add a field: bool disposeMode
                _disposeModeField = F.StateMachineField(boolType, GeneratedNames.MakeDisposeModeFieldName());
                
                if (_isEnumerable && this.method.Parameters.Any(static p => !p.IsExtensionParameterImplementation() && p.HasEnumeratorCancellationAttribute))
                {
                    // Add a field: CancellationTokenSource combinedTokens
                    _combinedTokensField = F.StateMachineField(
                        F.WellKnownType(WellKnownType.System_Threading_CancellationTokenSource),
                        GeneratedNames.MakeAsyncIteratorCombinedTokensFieldName());
                }
                
                // Note: No _builderField for runtime-async iterators
            }

            protected override void GenerateConstructor()
            {
                // Produces:
                // .ctor(int state)
                // {
                //     this.state = state;
                //     this.initialThreadId = {managedThreadId};
                // }
                // Note: No builder initialization for runtime-async iterators
                
                Debug.Assert(stateMachineType.Constructor is IteratorConstructor);
                
                F.CurrentFunction = stateMachineType.Constructor;
                var bodyBuilder = ArrayBuilder<BoundStatement>.GetInstance();
                bodyBuilder.Add(F.BaseInitialization());
                
                // this.state = state;
                bodyBuilder.Add(F.Assignment(F.InstanceField(stateField), F.Parameter(F.CurrentFunction.Parameters[0])));
                
                var managedThreadId = MakeCurrentThreadId();
                if (managedThreadId != null && (object)initialThreadIdField != null)
                {
                    // this.initialThreadId = {managedThreadId};
                    bodyBuilder.Add(F.Assignment(F.InstanceField(initialThreadIdField), managedThreadId));
                }
                
                if (instanceIdField is not null &&
                    F.WellKnownMethod(WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__GetNewStateMachineInstanceId) is { } getId)
                {
                    // this.instanceId = LocalStoreTracker.GetNewStateMachineInstanceId();
                    bodyBuilder.Add(F.Assignment(F.InstanceField(instanceIdField), F.Call(receiver: null, getId)));
                }
                
                bodyBuilder.Add(F.Return());
                F.CloseMethod(F.Block(bodyBuilder.ToImmutableAndFree()));
            }

            protected override void GenerateResetInstance(ArrayBuilder<BoundStatement> builder, StateMachineState initialState)
            {
                // this.state = {initialState};
                // this.disposeMode = false;
                // Note: No builder reset for runtime-async iterators
                
                builder.Add(
                    // this.state = {initialState};
                    F.Assignment(F.Field(F.This(), stateField), F.Literal(initialState)));
                
                builder.Add(
                    // disposeMode = false;
                    F.Assignment(F.InstanceField(_disposeModeField), F.Literal(false)));
            }

            /// <summary>
            /// Generates the `ValueTask&lt;bool> MoveNextAsync()` method for runtime-async.
            /// This method is marked with [MethodImpl(MethodImplOptions.Async)] and contains the user code directly.
            /// </summary>
            [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "Standard naming convention for generating 'IAsyncEnumerator.MoveNextAsync'")]
            private void GenerateIAsyncEnumeratorImplementation_MoveNextAsync()
            {
                // For runtime-async, MoveNextAsync is much simpler:
                // It's marked with [MethodImpl(MethodImplOptions.Async)] and the runtime handles the async state machine
                // The method directly contains the lowered user code with yield returns and awaits
                
                NamedTypeSymbol IAsyncEnumeratorOfElementType =
                    F.WellKnownType(WellKnownType.System_Collections_Generic_IAsyncEnumerator_T)
                    .Construct(_currentField.Type);
                
                MethodSymbol IAsyncEnumerableOfElementType_MoveNextAsync = F.WellKnownMethod(WellKnownMember.System_Collections_Generic_IAsyncEnumerator_T__MoveNextAsync)
                    .AsMember(IAsyncEnumeratorOfElementType);
                
                // The implementation depends on the method body of the iterator method.
                OpenMethodImplementation(IAsyncEnumerableOfElementType_MoveNextAsync, hasMethodBodyDependency: true);
                
                // The actual method body will be generated by GenerateMoveNext
                // For now, just set up the method signature with the Async flag
                // TODO: We need to mark this method with [MethodImpl(MethodImplOptions.Async)]
                // This should be done in code generation, not here
            }

            protected override void GenerateMoveNext(SynthesizedImplementationMethod moveNextMethod)
            {
                // For runtime-async iterators, we generate a simpler MoveNext that:
                // 1. Dispatches to yield return resume points
                // 2. Contains the user code directly
                // 3. Returns bool directly (not through a builder)
                
                MethodSymbol setResultMethod = F.WellKnownMethod(WellKnownMember.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__SetResult, isOptional: true);
                if (setResultMethod is { })
                {
                    setResultMethod = (MethodSymbol)setResultMethod.SymbolAsMember((NamedTypeSymbol)_promiseOfValueOrEndField.Type);
                }
                
                MethodSymbol setExceptionMethod = F.WellKnownMethod(WellKnownMember.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__SetException, isOptional: true);
                if (setExceptionMethod is { })
                {
                    setExceptionMethod = (MethodSymbol)setExceptionMethod.SymbolAsMember((NamedTypeSymbol)_promiseOfValueOrEndField.Type);
                }
                
                var rewriter = new AsyncIteratorMethodToStateMachineRewriter(
                    method: method,
                    methodOrdinal: _methodOrdinal,
                    asyncMethodBuilderMemberCollection: _asyncMethodBuilderMemberCollection,
                    asyncIteratorInfo: new AsyncIteratorInfo(_promiseOfValueOrEndField, _combinedTokensField, _currentField, _disposeModeField, setResultMethod, setExceptionMethod),
                    F: F,
                    state: stateField,
                    builder: null, // No builder for runtime-async
                    instanceIdField: instanceIdField,
                    hoistedVariables: hoistedVariables,
                    nonReusableLocalProxies: nonReusableLocalProxies,
                    nonReusableFieldsForCleanup: nonReusableFieldsForCleanup,
                    synthesizedLocalOrdinals: synthesizedLocalOrdinals,
                    stateMachineStateDebugInfoBuilder,
                    slotAllocatorOpt: slotAllocatorOpt,
                    nextFreeHoistedLocalSlot: nextFreeHoistedLocalSlot,
                    diagnostics: diagnostics);
                
                rewriter.GenerateMoveNext(body, moveNextMethod);
            }
        }
    }
}
