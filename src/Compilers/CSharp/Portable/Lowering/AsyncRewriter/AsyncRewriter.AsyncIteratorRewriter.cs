// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            private FieldSymbol _promiseIsActiveField;
            private FieldSymbol _currentField; // stores the current/yielded value

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
            }

            protected override void VerifyPresenceOfRequiredAPIs(DiagnosticBag bag)
            {
                base.VerifyPresenceOfRequiredAPIs(bag);
                EnsureWellKnownMember(WellKnownMember.System_Collections_Generic_IAsyncEnumerable_T__GetAsyncEnumerator, bag);
                EnsureWellKnownMember(WellKnownMember.System_Collections_Generic_IAsyncEnumerator_T__WaitForNextAsync, bag);
                EnsureWellKnownMember(WellKnownMember.System_Collections_Generic_IAsyncEnumerator_T__TryGetNext, bag);
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
                // IAsyncStateMachine and constructor
                base.GenerateMethodImplementations();

                // IAsyncEnumerable
                GenerateIAsyncEnumerableImplementation_GetAsyncEnumerator();

                // IAsyncEnumerator
                GenerateIAsyncEnumeratorImplementation_WaitForNextAsync();
                GenerateIAsyncEnumeratorImplementation_TryGetNext();

                // IValueTaskSource<bool>
                GenerateIValueTaskSourceImplementation_GetResult();
                GenerateIValueTaskSourceImplementation_GetStatus();
                GenerateIValueTaskSourceImplementation_OnCompleted();

                // IStrongBox<ManualResetValueTaskSourceLogic<TResult>>
                GenerateIStrongBox_get_Value();

                // IAsyncDisposable
                GenerateIAsyncDisposable_DisposeAsync();
            }

            protected override void GenerateControlFields()
            {
                // the fields are initialized from entry-point method (which replaces the async-iterator method), so they need to be public

                base.GenerateControlFields();
                NamedTypeSymbol boolType = F.SpecialType(SpecialType.System_Boolean);

                // Add a field: ManualResetValueTaskSourceLogic<bool> promiseOfValueOrEnd
                _promiseOfValueOrEndField = F.StateMachineField(
                    F.WellKnownType(WellKnownType.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T).Construct(boolType),
                    GeneratedNames.MakeAsyncIteratorPromiseOfValueOrEndFieldName(), isPublic: true);

                // Add a field: bool promiseIsActive
                _promiseIsActiveField = F.StateMachineField(
                    boolType,
                    GeneratedNames.MakeAsyncIteratorPromiseIsActiveFieldName(), isPublic: true);

                // the element type may contain method type parameters, which are now alpha-renamed into type parameters of the generated class
                TypeSymbol elementType = ((AsyncStateMachine)stateMachineType).IteratorElementType;

                // Add a field: T current
                _currentField = F.StateMachineField(elementType, GeneratedNames.MakeIteratorCurrentFieldName());
            }

            /// <summary>
            /// Generates the body of the replacement method, which initializes the state machine. Unlike regular async methods, we won't start it.
            /// </summary>
            protected override BoundStatement GenerateStateMachineCreation(LocalSymbol stateMachineVariable, NamedTypeSymbol frameType)
            {
                // If the async method's result type is a type parameter of the method, then the AsyncTaskMethodBuilder<T>
                // needs to use the method's type parameters inside the rewritten method body. All other methods generated
                // during async rewriting are members of the synthesized state machine struct, and use the type parameters
                // from the struct.
                AsyncMethodBuilderMemberCollection methodScopeAsyncMethodBuilderMemberCollection;
                if (!AsyncMethodBuilderMemberCollection.TryCreate(F, method, null, out methodScopeAsyncMethodBuilderMemberCollection))
                {
                    return new BoundBadStatement(F.Syntax, ImmutableArray<BoundNode>.Empty, hasErrors: true);
                }

                var bodyBuilder = ArrayBuilder<BoundStatement>.GetInstance();

                // local.$builder = System.Runtime.CompilerServices.AsyncTaskMethodBuilder<typeArgs>.Create();
                bodyBuilder.Add(
                    F.Assignment(
                        F.Field(F.Local(stateMachineVariable), _builderField.AsMember(frameType)),
                        F.StaticCall(
                            null,
                            methodScopeAsyncMethodBuilderMemberCollection.CreateBuilder)));

                // local.$stateField = NotStartedStateMachine;
                bodyBuilder.Add(
                    F.Assignment(
                        F.Field(F.Local(stateMachineVariable), stateField.AsMember(frameType)),
                        F.Literal(StateMachineStates.NotStartedStateMachine)));

                // local._valueOrEndPromise = new ManualResetValueTaskSourceLogic<bool>(stateMachine);
                MethodSymbol mrvtslCtor =
                    F.WellKnownMethod(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__ctor)
                    .AsMember((NamedTypeSymbol)_promiseOfValueOrEndField.Type);

                bodyBuilder.Add(
                    F.Assignment(
                        F.Field(F.Local(stateMachineVariable), _promiseOfValueOrEndField.AsMember(frameType)),
                        F.New(mrvtslCtor, F.Local(stateMachineVariable))));

                // local._promiseIsActive = true;
                bodyBuilder.Add(
                    F.Assignment(
                        F.Field(F.Local(stateMachineVariable), _promiseIsActiveField.AsMember(frameType)),
                        F.Literal(true)));

                // return local;
                bodyBuilder.Add(F.Return(F.Local(stateMachineVariable)));

                return F.Block(
                    bodyBuilder.ToImmutableAndFree());
            }

            /// <summary>
            /// Generates the WaitForNextAsync method.
            /// </summary>
            private void GenerateIAsyncEnumeratorImplementation_WaitForNextAsync()
            {
                // Produce the implementation for `ValueTask<bool> WaitForNextAsync()`:
                // if (State == StateMachineStates.FinishedStateMachine)
                // {
                //     return default(ValueTask<bool>)
                // }
                // if (!this._promiseIsActive || this.State == StateMachineStates.NotStartedStateMachine)
                // {
                //     var inst = this;
                //     this._builder.Start(ref inst);
                // }
                // return new ValueTask<bool>(this, _valueOrEndPromise.Version);

                NamedTypeSymbol IAsyncEnumeratorOfElementType =
                    F.WellKnownType(WellKnownType.System_Collections_Generic_IAsyncEnumerator_T)
                    .Construct(_currentField.Type);

                MethodSymbol IAsyncEnumerableOfElementType_WaitForNextAsync =
                    F.WellKnownMethod(WellKnownMember.System_Collections_Generic_IAsyncEnumerator_T__WaitForNextAsync)
                    .AsMember(IAsyncEnumeratorOfElementType);

                // The implementation doesn't depend on the method body of the iterator method.
                OpenMethodImplementation( IAsyncEnumerableOfElementType_WaitForNextAsync, hasMethodBodyDependency: false);

                BoundFieldAccess promiseIsActiveField = F.Field(F.This(), _promiseIsActiveField);

                var ifFinished = F.If(
                    // if (State == StateMachineStates.FinishedStateMachine)
                    F.IntEqual(F.Field(F.This(), stateField), F.Literal(StateMachineStates.FinishedStateMachine)),
                    thenClause: F.Return(F.Default(IAsyncEnumerableOfElementType_WaitForNextAsync.ReturnType))); // return default(ValueTask<bool>)

                var ifNotRunningOrNotStarted = F.If(
                    // if (!this._promiseIsActive || this.State == StateMachineStates.NotStartedStateMachine)
                    F.Binary(BinaryOperatorKind.LogicalOr, promiseIsActiveField.Type,
                        F.Not(promiseIsActiveField),
                        F.IntEqual(F.Field(F.This(), stateField), F.Literal(StateMachineStates.NotStartedStateMachine))),
                    thenClause: GenerateCallStart()); // var inst = this; this._builder.Start(ref inst);

                MethodSymbol valueTask_ctor =
                    F.WellKnownMethod(WellKnownMember.System_Threading_Tasks_ValueTask_T__ctor)
                    .AsMember((NamedTypeSymbol)IAsyncEnumerableOfElementType_WaitForNextAsync.ReturnType);

                MethodSymbol promise_get_Version =
                    F.WellKnownMethod(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__get_Version)
                    .AsMember((NamedTypeSymbol)_promiseOfValueOrEndField.Type);

                // return new ValueTask<bool>(this, _valueOrEndPromise.Version);
                var returnStatement = F.Return(F.New(valueTask_ctor, F.This(), F.Call(F.Field(F.This(), _promiseOfValueOrEndField), promise_get_Version)));

                F.CloseMethod(F.Block(ifFinished, ifNotRunningOrNotStarted, returnStatement));
            }

            /// <summary>
            /// Generates the TryGetNext method.
            /// </summary>
            private void GenerateIAsyncEnumeratorImplementation_TryGetNext()
            {
                // Produce the implementation for `T TryGetNext(out bool success)`:
                // if (this._promiseIsActive)
                // {
                //     if (_valueOrEndPromise.GetStatus(_valueOrEndPromise.Version) == ValueTaskSourceStatus.Pending) throw new Exception(); // https://github.com/dotnet/roslyn/issues/30109 Add this safeguard code
                //     _promiseIsActive = false;
                // }
                // else
                // {
                //     var inst = this;
                //     this._builder.Start(ref inst);
                // }
                // if (_promiseIsActive || State == StateMachineStates.FinishedStateMachine)
                // {
                //     success = false;
                //     return default;
                // }
                // success = true;
                // return _current;

                NamedTypeSymbol IAsyncEnumeratorOfElementType =
                    F.WellKnownType(WellKnownType.System_Collections_Generic_IAsyncEnumerator_T)
                    .Construct(_currentField.Type);

                MethodSymbol IAsyncEnumerableOfElementType_TryGetNext =
                    F.WellKnownMethod(WellKnownMember.System_Collections_Generic_IAsyncEnumerator_T__TryGetNext)
                    .AsMember(IAsyncEnumeratorOfElementType);

                // The implementation doesn't depend on the method body of the iterator method.
                OpenMethodImplementation( IAsyncEnumerableOfElementType_TryGetNext, hasMethodBodyDependency: false);

                var blockBuilder = ArrayBuilder<BoundStatement>.GetInstance();

                // this._promiseIsActive
                BoundFieldAccess promiseIsActiveField = F.Field(F.This(), _promiseIsActiveField);

                // var inst = this;
                // this._builder.Start(ref inst);
                BoundBlock startBlock = GenerateCallStart();

                // if (this._promiseIsActive)
                // {
                //     if (_valueOrEndPromise.GetStatus(_valueOrEndPromise.Version) == ValueTaskSourceStatus.Pending) throw new Exception();
                //     if (State == StateMachineStates.NotStartedStateMachine) throw new Exception("You should call WaitForNextAsync first"); // https://github.com/dotnet/roslyn/issues/30109 Add this safeguard code
                //     _promiseIsActive = false;
                // }
                // else
                // {
                //     var inst = this;
                //     this._builder.Start(ref inst);
                // }
                blockBuilder.Add(
                    F.If(
                        // if (this._promiseIsActive)
                        promiseIsActiveField,
                    thenClause: F.Assignment(promiseIsActiveField, F.Literal(false)), // this._promiseIsActive = false;
                    elseClauseOpt: startBlock)); // var inst = this; this._builder.Start(ref inst);

                // if (_promiseIsActive || State == StateMachineStates.FinishedStateMachine)
                // {
                //     success = false;
                //     return default;
                // }
                blockBuilder.Add(
                    F.If(
                        // if (this._promiseIsActive || State == StateMachineStates.FinishedStateMachine)
                        F.Binary(BinaryOperatorKind.LogicalOr, promiseIsActiveField.Type,
                            promiseIsActiveField,
                            F.IntEqual(F.Field(F.This(), stateField), F.Literal(StateMachineStates.FinishedStateMachine))),
                    thenClause: F.Block(generateAssignToSuccessParameter(false), F.Return(F.Default(_currentField.Type))))); // success = false; return default;

                blockBuilder.Add(
                    generateAssignToSuccessParameter(true)); // success = true;

                blockBuilder.Add(
                    F.Return(F.Field(F.This(), _currentField))); // return _current;

                F.CloseMethod(F.Block(blockBuilder.ToImmutableAndFree()));

                BoundStatement generateAssignToSuccessParameter(bool value)
                {
                    // Produce:
                    // success = value;

                    BoundParameter successParameter = F.Parameter(IAsyncEnumerableOfElementType_TryGetNext.Parameters[0]);
                    return F.Assignment(successParameter, F.Literal(value)); // success = value;
                }
            }

            private void GenerateIValueTaskSourceImplementation_GetResult()
            {
                // Produce the implementation for `bool IValueTaskSource<bool>.GetResult(short token)`:
                // return this._valueOrEndPromise.GetResult(token);

                NamedTypeSymbol IValueTaskSourceOfBool =
                    F.WellKnownType(WellKnownType.System_Threading_Tasks_Sources_IValueTaskSource_T)
                    .Construct(F.SpecialType(SpecialType.System_Boolean));

                MethodSymbol IValueTaskSourceOfBool_GetResult =
                    F.WellKnownMethod(WellKnownMember.System_Threading_Tasks_Sources_IValueTaskSource_T__GetResult)
                    .AsMember(IValueTaskSourceOfBool);

                MethodSymbol promise_GetResult =
                    F.WellKnownMethod(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__GetResult)
                    .AsMember((NamedTypeSymbol)_promiseOfValueOrEndField.Type);

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
                    .AsMember((NamedTypeSymbol)_promiseOfValueOrEndField.Type);

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
                    .AsMember((NamedTypeSymbol)_promiseOfValueOrEndField.Type);

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
                    .AsMember((NamedTypeSymbol)_promiseOfValueOrEndField.Type);

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
                    F.Return(F.Default(IAsyncDisposable_DisposeAsync.ReturnType)));

                F.CloseMethod(F.Block(bodyBuilder.ToImmutableAndFree()));
            }

            /// <summary>
            /// Generate code to start the state machine via builder.
            /// </summary>
            private BoundBlock GenerateCallStart()
            {
                // Produce:
                // var inst = this;
                // this._builder.Start(ref inst);

                LocalSymbol instSymbol = F.SynthesizedLocal(this.stateMachineType);
                MethodSymbol startMethod = _asyncMethodBuilderMemberCollection.Start.Construct(this.stateMachineType);
                BoundLocal instLocal = F.Local(instSymbol);
                Debug.Assert(!_asyncMethodBuilderMemberCollection.CheckGenericMethodConstraints);

                // this._builder.Start(ref inst);
                BoundExpressionStatement startCall = F.ExpressionStatement(
                     F.Call(
                         F.Field(F.This(), _builderField),
                         startMethod,
                         ImmutableArray.Create<BoundExpression>(instLocal)));

                return F.Block(
                    ImmutableArray.Create(instSymbol),
                    F.Assignment(instLocal, F.This()), // var inst = this;
                    startCall); // this._builder.Start(ref inst);
            }

            /// <summary>
            /// Generates the GetAsyncEnumerator method.
            /// </summary>
            private void GenerateIAsyncEnumerableImplementation_GetAsyncEnumerator()
            {
                // PROTOTYPE(async-streams): do the threadID dance to decide if we can return this or should instantiate.

                NamedTypeSymbol IAsyncEnumerableOfElementType =
                    F.WellKnownType(WellKnownType.System_Collections_Generic_IAsyncEnumerable_T)
                    .Construct(_currentField.Type);

                MethodSymbol IAsyncEnumerableOfElementType_GetEnumerator =
                    F.WellKnownMethod(WellKnownMember.System_Collections_Generic_IAsyncEnumerable_T__GetAsyncEnumerator)
                    .AsMember(IAsyncEnumerableOfElementType);

                // The implementation doesn't depend on the method body of the iterator method.
                // Generates IAsyncEnumerator<elementType> IAsyncEnumerable<elementType>.GetEnumerator()
                OpenMethodImplementation(IAsyncEnumerableOfElementType_GetEnumerator, hasMethodBodyDependency: false);

                // PROTOTYPE(async-streams): 0 is not the proper state to start with
                F.CloseMethod(F.Block(
                    //F.Assignment(F.Field(F.This(), stateField), F.Literal(StateMachineStates.FirstUnusedState)), // this.state = 0;
                    F.Return(F.This()))); // return this;
            }

            protected override void GenerateMoveNext(SynthesizedImplementationMethod moveNextMethod)
            {
                MethodSymbol setResultMethod = F.WellKnownMethod(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__SetResult, isOptional: true);
                if ((object)setResultMethod != null)
                {
                    setResultMethod = (MethodSymbol)setResultMethod.SymbolAsMember((NamedTypeSymbol)_promiseOfValueOrEndField.Type);
                }

                MethodSymbol resetMethod = F.WellKnownMethod(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__Reset, isOptional: true);
                if ((object)resetMethod != null)
                {
                    resetMethod = (MethodSymbol)resetMethod.SymbolAsMember((NamedTypeSymbol)_promiseOfValueOrEndField.Type);
                }

                MethodSymbol setExceptionMethod = F.WellKnownMethod(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__SetException, isOptional: true);
                if ((object)setExceptionMethod != null)
                {
                    setExceptionMethod = (MethodSymbol)setExceptionMethod.SymbolAsMember((NamedTypeSymbol)_promiseOfValueOrEndField.Type);
                }

                var rewriter = new AsyncMethodToStateMachineRewriter(
                    method: method,
                    methodOrdinal: _methodOrdinal,
                    asyncMethodBuilderMemberCollection: _asyncMethodBuilderMemberCollection,
                    asyncIteratorInfo: new AsyncIteratorInfo(_promiseOfValueOrEndField, _promiseIsActiveField, _currentField, resetMethod, setResultMethod, setExceptionMethod),
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
