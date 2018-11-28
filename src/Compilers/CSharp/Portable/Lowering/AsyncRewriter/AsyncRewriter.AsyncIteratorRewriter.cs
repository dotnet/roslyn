﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class AsyncRewriter : StateMachineRewriter
    {
        /// <summary>
        /// This rewriter rewrites an async-iterator method. See async-streams.md for design overview.
        /// </summary>
        private sealed class AsyncIteratorRewriter : AsyncRewriter
        {
            private FieldSymbol _promiseOfValueOrEndField; // this struct implements the IValueTaskSource logic
            private FieldSymbol _currentField; // stores the current/yielded value

            // true if the iterator implements IAsyncEnumerable<T>,
            // false if it implements IAsyncEnumerator<T>
            private readonly bool _isEnumerable;

            internal AsyncIteratorRewriter(
                BoundStatement body,
                MethodSymbol method,
                int methodOrdinal,
                AsyncStateMachine stateMachineType,
                VariableSlotAllocator slotAllocatorOpt,
                TypeCompilationState compilationState,
                DiagnosticBag diagnostics)
                : base(body, method, methodOrdinal, stateMachineType, slotAllocatorOpt, compilationState, diagnostics)
            {
                Debug.Assert(method.IteratorElementType != null);

                _isEnumerable = method.IsIAsyncEnumerableReturningAsync(method.DeclaringCompilation);
            }

            protected override void VerifyPresenceOfRequiredAPIs(DiagnosticBag bag)
            {
                base.VerifyPresenceOfRequiredAPIs(bag);

                if (_isEnumerable)
                {
                    EnsureWellKnownMember(WellKnownMember.System_Collections_Generic_IAsyncEnumerable_T__GetAsyncEnumerator, bag);
                }
                EnsureWellKnownMember(WellKnownMember.System_Collections_Generic_IAsyncEnumerator_T__MoveNextAsync, bag);
                EnsureWellKnownMember(WellKnownMember.System_Collections_Generic_IAsyncEnumerator_T__get_Current, bag);

                EnsureWellKnownMember(WellKnownMember.System_IAsyncDisposable__DisposeAsync, bag);
                EnsureWellKnownMember(WellKnownMember.System_Threading_Tasks_ValueTask_T__ctor, bag);

                EnsureWellKnownMember(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__ctor, bag);
                EnsureWellKnownMember(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__GetResult, bag);
                EnsureWellKnownMember(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__GetStatus, bag);
                EnsureWellKnownMember(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__get_Version, bag);
                EnsureWellKnownMember(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__OnCompleted, bag);
                EnsureWellKnownMember(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__Reset, bag);
                EnsureWellKnownMember(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__SetException, bag);
                EnsureWellKnownMember(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__SetResult, bag);

                EnsureWellKnownMember(WellKnownMember.System_Threading_Tasks_Sources_IValueTaskSource_T__GetResult, bag);
                EnsureWellKnownMember(WellKnownMember.System_Threading_Tasks_Sources_IValueTaskSource_T__GetStatus, bag);
                EnsureWellKnownMember(WellKnownMember.System_Threading_Tasks_Sources_IValueTaskSource_T__OnCompleted, bag);

                EnsureWellKnownMember(WellKnownMember.System_Runtime_CompilerServices_IStrongBox_T__get_Value, bag);
                EnsureWellKnownMember(WellKnownMember.System_Runtime_CompilerServices_IStrongBox_T__Value, bag);
            }

            protected override void GenerateMethodImplementations()
            {
                // IAsyncStateMachine methods and constructor
                base.GenerateMethodImplementations();

                if (_isEnumerable)
                {
                    // IAsyncEnumerable
                    GenerateIAsyncEnumerableImplementation_GetAsyncEnumerator();
                }

                // IAsyncEnumerator
                GenerateIAsyncEnumeratorImplementation_MoveNextAsync();
                GenerateIAsyncEnumeratorImplementation_Current();

                // IValueTaskSource<bool>
                GenerateIValueTaskSourceImplementation_GetResult();
                GenerateIValueTaskSourceImplementation_GetStatus();
                GenerateIValueTaskSourceImplementation_OnCompleted();

                // IStrongBox<ManualResetValueTaskSourceLogic<TResult>>
                GenerateIStrongBox_get_Value();

                // IAsyncDisposable
                GenerateIAsyncDisposable_DisposeAsync();
            }

            protected override bool PreserveInitialParameterValuesAndThreadId
                => _isEnumerable;

            protected override void GenerateControlFields()
            {
                // the fields are initialized from entry-point method (which replaces the async-iterator method), so they need to be public

                base.GenerateControlFields();
                NamedTypeSymbol boolType = F.SpecialType(SpecialType.System_Boolean);

                // Add a field: ManualResetValueTaskSourceLogic<bool> promiseOfValueOrEnd
                _promiseOfValueOrEndField = F.StateMachineField(
                    F.WellKnownType(WellKnownType.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T).Construct(boolType),
                    GeneratedNames.MakeAsyncIteratorPromiseOfValueOrEndFieldName(), isPublic: true);

                // the element type may contain method type parameters, which are now alpha-renamed into type parameters of the generated class
                TypeSymbol elementType = ((AsyncStateMachine)stateMachineType).IteratorElementType;

                // Add a field: T current
                _currentField = F.StateMachineField(elementType, GeneratedNames.MakeIteratorCurrentFieldName());
            }

            protected override void GenerateConstructor()
            {
                // Produces:
                // .ctor(int state)
                // {
                //     this.state = state;
                //     this.initialThreadId = {managedThreadId};
                //     this.builder = System.Runtime.CompilerServices.AsyncVoidMethodBuilder.Create();
                //     this.valueOrEndPromise = new ManualResetValueTaskSourceLogic<bool>(this);
                // }
                Debug.Assert(stateMachineType.Constructor is IteratorConstructor);

                F.CurrentFunction = stateMachineType.Constructor;
                var bodyBuilder = ArrayBuilder<BoundStatement>.GetInstance();
                bodyBuilder.Add(F.BaseInitialization());
                bodyBuilder.Add(F.Assignment(F.Field(F.This(), stateField), F.Parameter(F.CurrentFunction.Parameters[0]))); // this.state = state;

                var managedThreadId = MakeCurrentThreadId();
                if (managedThreadId != null && (object)initialThreadIdField != null)
                {
                    // this.initialThreadId = {managedThreadId};
                    bodyBuilder.Add(F.Assignment(F.Field(F.This(), initialThreadIdField), managedThreadId));
                }

                // this.builder = System.Runtime.CompilerServices.AsyncVoidMethodBuilder.Create();
                AsyncMethodBuilderMemberCollection methodScopeAsyncMethodBuilderMemberCollection;
                bool found = AsyncMethodBuilderMemberCollection.TryCreate(F, method, typeMap: null, out methodScopeAsyncMethodBuilderMemberCollection);
                Debug.Assert(found);

                bodyBuilder.Add(
                    F.Assignment(
                        F.Field(F.This(), _builderField),
                        F.StaticCall(
                            null,
                            methodScopeAsyncMethodBuilderMemberCollection.CreateBuilder)));

                // this._valueOrEndPromise = new ManualResetValueTaskSourceLogic<bool>(this);
                MethodSymbol mrvtslCtor =
                    F.WellKnownMethod(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__ctor)
                    .AsMember((NamedTypeSymbol)_promiseOfValueOrEndField.Type.TypeSymbol);

                bodyBuilder.Add(
                    F.Assignment(
                        F.Field(F.This(), _promiseOfValueOrEndField),
                        F.New(mrvtslCtor, F.This())));

                bodyBuilder.Add(F.Return());
                F.CloseMethod(F.Block(bodyBuilder.ToImmutableAndFree()));
                bodyBuilder = null;
            }

            protected override void InitializeStateMachine(ArrayBuilder<BoundStatement> bodyBuilder, NamedTypeSymbol frameType, LocalSymbol stateMachineLocal)
            {
                // var stateMachineLocal = new {StateMachineType}({initialState})
                int initialState = _isEnumerable ? StateMachineStates.FinishedStateMachine : StateMachineStates.NotStartedStateMachine;
                bodyBuilder.Add(
                    F.Assignment(
                        F.Local(stateMachineLocal),
                        F.New(stateMachineType.Constructor.AsMember(frameType), F.Literal(initialState))));
            }

            protected override BoundStatement GenerateStateMachineCreation(LocalSymbol stateMachineVariable, NamedTypeSymbol frameType)
            {
                // return local;
                return F.Block(F.Return(F.Local(stateMachineVariable)));
            }

            /// <summary>
            /// Generates the `ValueTask&lt;bool> MoveNextAsync()` method.
            /// </summary>
            private void GenerateIAsyncEnumeratorImplementation_MoveNextAsync()
            {
                // Produce:
                //  if (State == StateMachineStates.FinishedStateMachine)
                //  {
                //      return default(ValueTask<bool>)
                //  }
                //  _valueOrEndPromise.Reset();
                //  var inst = this;
                //  _builder.Start(ref inst);
                //  return new ValueTask<bool>(this, _valueOrEndPromise.Version);

                NamedTypeSymbol IAsyncEnumeratorOfElementType =
                    F.WellKnownType(WellKnownType.System_Collections_Generic_IAsyncEnumerator_T)
                    .Construct(_currentField.Type.TypeSymbol);

                MethodSymbol IAsyncEnumerableOfElementType_MoveNextAsync =
                    F.WellKnownMethod(WellKnownMember.System_Collections_Generic_IAsyncEnumerator_T__MoveNextAsync)
                    .AsMember(IAsyncEnumeratorOfElementType);

                // The implementation doesn't depend on the method body of the iterator method.
                OpenMethodImplementation(IAsyncEnumerableOfElementType_MoveNextAsync, hasMethodBodyDependency: false);

                var ifFinished = F.If(
                    // if (State == StateMachineStates.FinishedStateMachine)
                    F.IntEqual(F.Field(F.This(), stateField), F.Literal(StateMachineStates.FinishedStateMachine)),
                    // return default(ValueTask<bool>)
                    thenClause: F.Return(F.Default(IAsyncEnumerableOfElementType_MoveNextAsync.ReturnType.TypeSymbol)));

                // _promiseOfValueOrEnd.Reset();
                BoundFieldAccess promiseField = F.Field(F.This(), _promiseOfValueOrEndField);
                var resetMethod = (MethodSymbol)F.WellKnownMethod(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__Reset, isOptional: true)
                    .SymbolAsMember((NamedTypeSymbol)_promiseOfValueOrEndField.Type.TypeSymbol);

                var callReset = F.ExpressionStatement(F.Call(promiseField, resetMethod));

                // _builder.Start(ref inst);
                Debug.Assert(!_asyncMethodBuilderMemberCollection.CheckGenericMethodConstraints);
                MethodSymbol startMethod = _asyncMethodBuilderMemberCollection.Start.Construct(this.stateMachineType);
                LocalSymbol instSymbol = F.SynthesizedLocal(this.stateMachineType);
                BoundLocal instLocal = F.Local(instSymbol);
                BoundExpressionStatement startCall = F.ExpressionStatement(
                     F.Call(
                         F.Field(F.This(), _builderField),
                         startMethod,
                         ImmutableArray.Create<BoundExpression>(instLocal)));

                MethodSymbol valueTask_ctor =
                    F.WellKnownMethod(WellKnownMember.System_Threading_Tasks_ValueTask_T__ctor)
                    .AsMember((NamedTypeSymbol)IAsyncEnumerableOfElementType_MoveNextAsync.ReturnType.TypeSymbol);

                MethodSymbol promise_get_Version =
                    F.WellKnownMethod(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__get_Version)
                    .AsMember((NamedTypeSymbol)_promiseOfValueOrEndField.Type.TypeSymbol);

                // return new ValueTask<bool>(this, _valueOrEndPromise.Version);
                var returnStatement = F.Return(F.New(valueTask_ctor, F.This(), F.Call(F.Field(F.This(), _promiseOfValueOrEndField), promise_get_Version)));

                F.CloseMethod(F.Block(
                    ImmutableArray.Create(instSymbol),
                    ifFinished,
                    callReset, // _promiseOfValueOrEnd.Reset();
                    F.Assignment(instLocal, F.This()), // var inst = this;
                    startCall, // _builder.Start(ref inst);
                    returnStatement));
            }

            /// <summary>
            /// Generates the Current property.
            /// </summary>
            private void GenerateIAsyncEnumeratorImplementation_Current()
            {
                // Produce the implementation for `T Current { get; }`:
                // return _current;

                NamedTypeSymbol IAsyncEnumeratorOfElementType =
                    F.WellKnownType(WellKnownType.System_Collections_Generic_IAsyncEnumerator_T)
                    .Construct(_currentField.Type.TypeSymbol);

                MethodSymbol IAsyncEnumerableOfElementType_get_Current =
                    F.WellKnownMethod(WellKnownMember.System_Collections_Generic_IAsyncEnumerator_T__get_Current)
                    .AsMember(IAsyncEnumeratorOfElementType);

                OpenPropertyImplementation(IAsyncEnumerableOfElementType_get_Current);

                F.CloseMethod(F.Block(F.Return(F.Field(F.This(), _currentField))));
            }

            private void GenerateIValueTaskSourceImplementation_GetResult()
            {
                // Produce the implementation for `bool IValueTaskSource<bool>.GetResult(short token)`:
                // return _valueOrEndPromise.GetResult(token);

                NamedTypeSymbol IValueTaskSourceOfBool =
                    F.WellKnownType(WellKnownType.System_Threading_Tasks_Sources_IValueTaskSource_T)
                    .Construct(F.SpecialType(SpecialType.System_Boolean));

                MethodSymbol IValueTaskSourceOfBool_GetResult =
                    F.WellKnownMethod(WellKnownMember.System_Threading_Tasks_Sources_IValueTaskSource_T__GetResult)
                    .AsMember(IValueTaskSourceOfBool);

                MethodSymbol promise_GetResult =
                    F.WellKnownMethod(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__GetResult)
                    .AsMember((NamedTypeSymbol)_promiseOfValueOrEndField.Type.TypeSymbol);

                // The implementation doesn't depend on the method body of the iterator method.
                OpenMethodImplementation(IValueTaskSourceOfBool_GetResult, hasMethodBodyDependency: false);

                // return this._valueOrEndPromise.GetResult(token);
                F.CloseMethod(F.Return(
                    F.Call(F.Field(F.This(), _promiseOfValueOrEndField), promise_GetResult, F.Parameter(IValueTaskSourceOfBool_GetResult.Parameters[0]))));
            }

            private void GenerateIValueTaskSourceImplementation_GetStatus()
            {
                // Produce the implementation for `ValueTaskSourceStatus IValueTaskSource<bool>.GetStatus(short token)`:
                // return this._valueOrEndPromise.GetStatus(token);

                NamedTypeSymbol IValueTaskSourceOfBool =
                    F.WellKnownType(WellKnownType.System_Threading_Tasks_Sources_IValueTaskSource_T)
                    .Construct(F.SpecialType(SpecialType.System_Boolean));

                MethodSymbol IValueTaskSourceOfBool_GetStatus =
                    F.WellKnownMethod(WellKnownMember.System_Threading_Tasks_Sources_IValueTaskSource_T__GetStatus)
                    .AsMember(IValueTaskSourceOfBool);

                MethodSymbol promise_GetStatus =
                    F.WellKnownMethod(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__GetStatus)
                    .AsMember((NamedTypeSymbol)_promiseOfValueOrEndField.Type.TypeSymbol);

                // The implementation doesn't depend on the method body of the iterator method.
                OpenMethodImplementation(IValueTaskSourceOfBool_GetStatus, hasMethodBodyDependency: false);

                // return this._valueOrEndPromise.GetStatus(token);
                F.CloseMethod(F.Return(
                    F.Call(F.Field(F.This(), _promiseOfValueOrEndField), promise_GetStatus, F.Parameter(IValueTaskSourceOfBool_GetStatus.Parameters[0]))));
            }

            private void GenerateIValueTaskSourceImplementation_OnCompleted()
            {
                // Produce the implementation for `void IValueTaskSource<bool>.OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)`:
                // this._valueOrEndPromise.OnCompleted(continuation, state, token, flags);
                // return;

                NamedTypeSymbol IValueTaskSourceOfBool =
                    F.WellKnownType(WellKnownType.System_Threading_Tasks_Sources_IValueTaskSource_T)
                    .Construct(F.SpecialType(SpecialType.System_Boolean));

                MethodSymbol IValueTaskSourceOfBool_OnCompleted =
                    F.WellKnownMethod(WellKnownMember.System_Threading_Tasks_Sources_IValueTaskSource_T__OnCompleted)
                    .AsMember(IValueTaskSourceOfBool);

                MethodSymbol promise_OnCompleted =
                    F.WellKnownMethod(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__OnCompleted)
                    .AsMember((NamedTypeSymbol)_promiseOfValueOrEndField.Type.TypeSymbol);

                // The implementation doesn't depend on the method body of the iterator method.
                OpenMethodImplementation(IValueTaskSourceOfBool_OnCompleted, hasMethodBodyDependency: false);

                F.CloseMethod(F.Block(
                    // this._valueOrEndPromise.OnCompleted(continuation, state, token, flags);
                    F.ExpressionStatement(
                        F.Call(F.Field(F.This(), _promiseOfValueOrEndField), promise_OnCompleted,
                        F.Parameter(IValueTaskSourceOfBool_OnCompleted.Parameters[0]),
                        F.Parameter(IValueTaskSourceOfBool_OnCompleted.Parameters[1]),
                        F.Parameter(IValueTaskSourceOfBool_OnCompleted.Parameters[2]),
                        F.Parameter(IValueTaskSourceOfBool_OnCompleted.Parameters[3]))),
                    F.Return())); // return;
            }

            private void GenerateIStrongBox_get_Value()
            {
                // Produce the implementation for `ref ManualResetValueTaskSourceLogic<bool> IStrongBox<ManualResetValueTaskSourceLogic<bool>>.Value { get; }`:
                // return ref _valueOrEndPromise;

                NamedTypeSymbol MrvtslOfBool =
                    F.WellKnownType(WellKnownType.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T)
                    .Construct(F.SpecialType(SpecialType.System_Boolean));

                NamedTypeSymbol IStrongBoxOfMrvtslOfBool =
                    F.WellKnownType(WellKnownType.System_Runtime_CompilerServices_IStrongBox_T)
                    .Construct(MrvtslOfBool);

                MethodSymbol IStrongBoxOfMrvtslOfBool_get_Value =
                    F.WellKnownMethod(WellKnownMember.System_Runtime_CompilerServices_IStrongBox_T__get_Value)
                    .AsMember(IStrongBoxOfMrvtslOfBool);

                OpenPropertyImplementation(IStrongBoxOfMrvtslOfBool_get_Value);

                // return ref _valueOrEndPromise;
                F.CloseMethod(F.Return(F.Field(F.This(), _promiseOfValueOrEndField)));
            }

            private void GenerateIAsyncDisposable_DisposeAsync()
            {
                // Produce the implementation of `ValueTask IAsyncDisposable.DisposeAsync()`:
                // this.builder.SetResult();
                // this._valueOrEndPromise.Reset();
                // this._state = StateNotStarted;
                // return default;

                NamedTypeSymbol IAsyncDisposable =
                    F.WellKnownType(WellKnownType.System_IAsyncDisposable);

                MethodSymbol IAsyncDisposable_DisposeAsync =
                    F.WellKnownMethod(WellKnownMember.System_IAsyncDisposable__DisposeAsync)
                    .AsMember(IAsyncDisposable);

                // The implementation doesn't depend on the method body of the iterator method.
                OpenMethodImplementation(IAsyncDisposable_DisposeAsync, hasMethodBodyDependency: false);

                var bodyBuilder = ArrayBuilder<BoundStatement>.GetInstance();

                MethodSymbol promise_Reset =
                    F.WellKnownMethod(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__Reset)
                    .AsMember((NamedTypeSymbol)_promiseOfValueOrEndField.Type.TypeSymbol);

                bodyBuilder.Add(
                    // this.builder.SetResult();
                    F.ExpressionStatement(
                        F.Call(
                            F.Field(F.This(), _builderField),
                            _asyncMethodBuilderMemberCollection.SetResult)));

                bodyBuilder.Add(
                    // this._valueOrEndPromise.Reset();
                    F.ExpressionStatement(
                        F.Call(F.Field(F.This(), _promiseOfValueOrEndField), promise_Reset)));

                bodyBuilder.Add(
                    //_state = StateDisposed;
                    F.Assignment(F.Field(F.This(), stateField), F.Literal(StateMachineStates.NotStartedStateMachine)));

                bodyBuilder.Add(
                    // return default;
                    F.Return(F.Default(IAsyncDisposable_DisposeAsync.ReturnType.TypeSymbol)));

                F.CloseMethod(F.Block(bodyBuilder.ToImmutableAndFree()));
            }

            /// <summary>
            /// Generates the GetAsyncEnumerator method.
            /// </summary>
            private void GenerateIAsyncEnumerableImplementation_GetAsyncEnumerator()
            {
                NamedTypeSymbol IAsyncEnumerableOfElementType =
                    F.WellKnownType(WellKnownType.System_Collections_Generic_IAsyncEnumerable_T)
                    .Construct(_currentField.Type.TypeSymbol);

                MethodSymbol IAsyncEnumerableOfElementType_GetEnumerator =
                    F.WellKnownMethod(WellKnownMember.System_Collections_Generic_IAsyncEnumerable_T__GetAsyncEnumerator)
                    .AsMember(IAsyncEnumerableOfElementType);

                BoundExpression managedThreadId = null;
                GenerateIteratorGetEnumerator(IAsyncEnumerableOfElementType_GetEnumerator, ref managedThreadId, StateMachineStates.NotStartedStateMachine);
            }

            protected override void GenerateMoveNext(SynthesizedImplementationMethod moveNextMethod)
            {
                MethodSymbol setResultMethod = F.WellKnownMethod(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__SetResult, isOptional: true);
                if ((object)setResultMethod != null)
                {
                    setResultMethod = (MethodSymbol)setResultMethod.SymbolAsMember((NamedTypeSymbol)_promiseOfValueOrEndField.Type.TypeSymbol);
                }

                MethodSymbol setExceptionMethod = F.WellKnownMethod(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__SetException, isOptional: true);
                if ((object)setExceptionMethod != null)
                {
                    setExceptionMethod = (MethodSymbol)setExceptionMethod.SymbolAsMember((NamedTypeSymbol)_promiseOfValueOrEndField.Type.TypeSymbol);
                }

                var rewriter = new AsyncMethodToStateMachineRewriter(
                    method: method,
                    methodOrdinal: _methodOrdinal,
                    asyncMethodBuilderMemberCollection: _asyncMethodBuilderMemberCollection,
                    asyncIteratorInfo: new AsyncIteratorInfo(_promiseOfValueOrEndField, _currentField, setResultMethod, setExceptionMethod),
                    F: F,
                    state: stateField,
                    builder: _builderField,
                    hoistedVariables: hoistedVariables,
                    nonReusableLocalProxies: nonReusableLocalProxies,
                    synthesizedLocalOrdinals: synthesizedLocalOrdinals,
                    slotAllocatorOpt: slotAllocatorOpt,
                    nextFreeHoistedLocalSlot: nextFreeHoistedLocalSlot,
                    diagnostics: diagnostics);

                rewriter.GenerateMoveNext(body, moveNextMethod);
            }
        }
    }
}
