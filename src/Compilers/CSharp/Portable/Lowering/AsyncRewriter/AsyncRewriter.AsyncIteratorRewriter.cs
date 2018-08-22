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
        // PROTOTYPE(async-streams): Consider making AsyncRewriter an abstract base

        /// <summary>
        /// This rewriter rewrites an async iterator method.
        /// It produces a similar state machine as the AsyncRewriter does, but additionally:
        /// - implementing IAsyncEnumerable and IAsyncEnumerator
        /// - with a current field for last currently yielded value
        /// - with a promise of a value or end (implemented as a ManualResetValueTaskSourceLogic{bool} and its supporting interfaces)
        ///
        /// The promise of a value or end is visible outside of the state machine (it is returned from WaitForNextAsync). The existing
        /// builder and awaiter are used internally, to run the state machine in the background.
        ///
        ///
        /// Compared to the state machine for a regular async method, the MoveNext for an async iterator method adds logic:
        /// - to the handling of an `await`, to reset the promise
        /// - to the handling of exceptions, to set the exception into the promise, if active, or rethrow it otherwise
        /// - to support handling a `yield return` statement, which saves the current value and fulfill the promise (if active)
        /// - to support handling a `yield break` statement, which resets the promise (if active) and fulfills it with result `false`
        ///
        /// The contract of the `MoveNext` method is that it returns either:
        /// - in completed state
        /// - leaving the promise inactive (when started with an inactive promise and a value is immediately available)
        /// - with an exception (when started with an inactive promise and an exception is thrown)
        /// - an active promise, which will later be fulfilled:
        ///     - with `true` (when a value becomes available),
        ///     - with `false` (if the end is reached)
        ///     - with an exception
        ///
        /// If the promise is active:
        /// - the builder is running the `MoveNext` logic,
        /// - a call to `WaitForNextAsync` will not move the state machine forward (ie. it won't call `MoveNext`),
        /// - a call to `TryGetNext` APIs will throw.
        /// </summary>
        private sealed class AsyncIteratorRewriter : AsyncRewriter
        {
            private FieldSymbol _currentField; // stores the current yieled value
            private FieldSymbol _promiseOfValueOrEndField; // this struct implements the IValueTaskSource logic
            private FieldSymbol _promiseIsActiveField;

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

                // PROTOTYPE(async-streams): Why does AsyncRewriter have logic to ignore accessibility?
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
                // the fields are initialized from async method, so they need to be public

                base.GenerateControlFields();

                // Add a field: T current
                _currentField = F.StateMachineField(method.IteratorElementType, GeneratedNames.MakeIteratorCurrentFieldName());

                // Add a field: ManualResetValueTaskSourceLogic<bool> promiseOfValueOrEnd
                _promiseOfValueOrEndField = F.StateMachineField(
                    F.WellKnownType(WellKnownType.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T)
                        .Construct(F.SpecialType(SpecialType.System_Boolean)),
                    GeneratedNames.MakeAsyndIteratorPromiseOfValueOrEndFieldName(), isPublic: true);

                // Add a field: bool promiseIsActive
                _promiseIsActiveField = F.StateMachineField(
                    F.SpecialType(SpecialType.System_Boolean),
                    GeneratedNames.MakeAsyndIteratorPromiseIsActiveFieldName(), isPublic: true);
            }

            /// <summary>
            /// Generates the body of the replacement method, which initializes the state machine. Unlike regular async methods, we won't start it.
            /// </summary>
            protected override BoundStatement GenerateStateMachineCreation(LocalSymbol stateMachineVariable, NamedTypeSymbol frameType)
            {
                // PROTOTYPE(async-streams): TODO review this (what is this error case at the start?)

                // If the async method's result type is a type parameter of the method, then the AsyncTaskMethodBuilder<T>
                // needs to use the method's type parameters inside the rewritten method body. All other methods generated
                // during async rewriting are members of the synthesized state machine struct, and use the type parameters
                // structs type parameters.
                AsyncMethodBuilderMemberCollection methodScopeAsyncMethodBuilderMemberCollection;
                if (!AsyncMethodBuilderMemberCollection.TryCreate(F, method, null, out methodScopeAsyncMethodBuilderMemberCollection))
                {
                    return new BoundBadStatement(F.Syntax, ImmutableArray<BoundNode>.Empty, hasErrors: true);
                }

                var bodyBuilder = ArrayBuilder<BoundStatement>.GetInstance();
                LocalSymbol builderVariable = F.SynthesizedLocal(methodScopeAsyncMethodBuilderMemberCollection.BuilderType, null);

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

                // builder = local.$stateField.builder;
                bodyBuilder.Add(
                    F.Assignment(
                        F.Local(builderVariable),
                        F.Field(F.Local(stateMachineVariable), _builderField.AsMember(frameType))));

                // local._valueOrEndPromise = new ManualResetValueTaskSourceLogic<bool>(stateMachine);
                MethodSymbol mrvtslCtor =
                    F.WellKnownMethod(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__ctor)
                    .AsMember((NamedTypeSymbol)_promiseOfValueOrEndField.Type);

                bodyBuilder.Add(
                    F.Assignment(
                        F.Field(F.Local(stateMachineVariable), _promiseOfValueOrEndField.AsMember(frameType)),
                        F.New(mrvtslCtor, F.Local(stateMachineVariable))));

                // PROTOTYPE(async-streams): Why do we need AsMember?
                // local._promiseIsActive = true;
                bodyBuilder.Add(
                    F.Assignment(
                        F.Field(F.Local(stateMachineVariable), _promiseIsActiveField.AsMember(frameType)),
                        F.Literal(true)));

                // return local.$stateField;
                bodyBuilder.Add(F.Return(F.Local(stateMachineVariable)));

                return F.Block(
                    ImmutableArray.Create(builderVariable),
                    bodyBuilder.ToImmutableAndFree());
            }

            /// <summary>
            /// Generates the WaitForNextAsync method.
            /// </summary>
            private void GenerateIAsyncEnumeratorImplementation_WaitForNextAsync()
            {
                // Produce:
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
                LocalSymbol instSymbol = F.SynthesizedLocal(this.stateMachineType);

                var ifFinished = F.If(
                    // if (State == StateMachineStates.FinishedStateMachine)
                    F.IntEqual(F.Field(F.This(), stateField), F.Literal(StateMachineStates.FinishedStateMachine)),
                    thenClause: F.Return(F.Default(IAsyncEnumerableOfElementType_WaitForNextAsync.ReturnType))); // return default(ValueTask<bool>)

                var ifNotRunningOrNotStarted = F.If(
                    // if (!this._promiseIsActive || this.State == StateMachineStates.NotStartedStateMachine)
                    F.Binary(BinaryOperatorKind.LogicalOr, promiseIsActiveField.Type,
                        F.Not(promiseIsActiveField),
                        F.IntEqual(F.Field(F.This(), stateField), F.Literal(StateMachineStates.NotStartedStateMachine))),
                    thenClause: GenerateCallStart(instSymbol)); // var inst = this; this._builder.Start(ref inst);

                MethodSymbol valueTask_ctor =
                    F.WellKnownMethod(WellKnownMember.System_Threading_Tasks_ValueTask_T__ctor)
                    .AsMember((NamedTypeSymbol)IAsyncEnumerableOfElementType_WaitForNextAsync.ReturnType);

                MethodSymbol promise_get_Version =
                    F.WellKnownMethod(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__get_Version)
                    .AsMember((NamedTypeSymbol)_promiseOfValueOrEndField.Type);

                // return new ValueTask<bool>(this, _valueOrEndPromise.Version);
                var returnStatement = F.Return(F.New(valueTask_ctor, F.This(), F.Call(F.Field(F.This(), _promiseOfValueOrEndField), promise_get_Version)));

                F.CloseMethod(F.Block(ImmutableArray.Create(instSymbol), ifFinished, ifNotRunningOrNotStarted, returnStatement));
            }

            /// <summary>
            /// Generates the TryGetNext method.
            /// </summary>
            private void GenerateIAsyncEnumeratorImplementation_TryGetNext()
            {
                // Produce:
                // if (this._promiseIsActive)
                // {
                //     if (_valueOrEndPromise.GetStatus(_valueOrEndPromise.Version) == ValueTaskSourceStatus.Pending) throw new Exception();
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

                // PROTOTYPE(async-streams): Add safeguard code

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
                LocalSymbol instSymbol = F.SynthesizedLocal(this.stateMachineType);
                BoundBlock startBlock = GenerateCallStart(instSymbol);

                // if (this._promiseIsActive)
                // {
                //     if (_valueOrEndPromise.GetStatus(_valueOrEndPromise.Version) == ValueTaskSourceStatus.Pending) throw new Exception();
                //     if (State == StateMachineStates.NotStartedStateMachine) throw new Exception("You should call WaitForNextAsync first");
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

                F.CloseMethod(F.Block(ImmutableArray.Create(instSymbol), blockBuilder.ToImmutableAndFree()));

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
                // Produce:
                // return this._valueOrEndPromise.GetResult(token);

                NamedTypeSymbol IValueTaskSourceOfBool =
                    F.WellKnownType(WellKnownType.System_Threading_Tasks_Sources_IValueTaskSource_T)
                    .Construct(F.SpecialType(SpecialType.System_Boolean));

                MethodSymbol IValueTaskSourceOfBool_GetResult =
                    F.WellKnownMethod(WellKnownMember.System_Threading_Tasks_Sources_IValueTaskSource_T__GetResult)
                    .AsMember(IValueTaskSourceOfBool);

                // PROTOTYPE(async-streams): Should we looking those members as optional?
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
                // Produce:
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
                // Produce:
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
                // Produce:
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
                // Produce:
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
            private BoundBlock GenerateCallStart(LocalSymbol instSymbol)
            {
                // Produce:
                // var inst = this;
                // this._builder.Start(ref inst);

                // PROTOTYPE(async-streams): Can we factor this code? (copied and modified from below)

                MethodSymbol startMethod = _asyncMethodBuilderMemberCollection.Start.Construct(this.stateMachineType);
                BoundLocal instLocal = F.Local(instSymbol);

                // PROTOTYPE(async-streams): Test constraints scenario
                //if (_asyncMethodBuilderMemberCollection.CheckGenericMethodConstraints)
                //{
                //    startMethod.CheckConstraints(F.Compilation.Conversions, F.Syntax, F.Compilation, diagnostics);
                //}

                // this._builder.Start(ref inst);
                BoundExpressionStatement startCall = F.ExpressionStatement(
                     F.Call(
                         F.Field(F.This(), _builderField),
                         startMethod,
                         ImmutableArray.Create<BoundExpression>(instLocal)));

                return F.Block(
                    F.Assignment(instLocal, F.This()), // var inst = this;
                    startCall); // this._builder.Start(ref inst);
            }

            /// <summary>
            /// Generates the GetEnumerator method.
            /// </summary>
            private void GenerateIAsyncEnumerableImplementation_GetAsyncEnumerator()
            {
                // PROTOTYPE(async-streams): do the threadID dance.

                NamedTypeSymbol IAsyncEnumerableOfElementType =
                    F.WellKnownType(WellKnownType.System_Collections_Generic_IAsyncEnumerable_T)
                    .Construct(_currentField.Type);

                MethodSymbol IAsyncEnumerableOfElementType_GetEnumerator =
                    F.WellKnownMethod(WellKnownMember.System_Collections_Generic_IAsyncEnumerable_T__GetAsyncEnumerator)
                    .AsMember(IAsyncEnumerableOfElementType);

                // PROTOTYPE(async-streams): TODO
                // result = this;
                // result.parameter = this.parameterProxy; // copy all of the parameter proxies // PROTOTYPE(async-streams): No sure what this is for

                // The implementation doesn't depend on the method body of the iterator method.
                // Generates IAsyncEnumerator<elementType> IAsyncEnumerable<elementType>.GetEnumerator()
                OpenMethodImplementation( IAsyncEnumerableOfElementType_GetEnumerator, hasMethodBodyDependency: false);

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

                // PROTOTYPE(async-streams): We should have checked for required members earlier
                //if (setResultMethod is null)
                //{
                //    var descriptor = WellKnownMembers.GetDescriptor(member);
                //    var diagnostic = new CSDiagnostic(
                //        new CSDiagnosticInfo(ErrorCode.ERR_MissingPredefinedMember, (customBuilder ? (object)builderType : descriptor.DeclaringTypeMetadataName), descriptor.Name),
                //        F.Syntax.Location);
                //    F.Diagnostics.Add(diagnostic);
                //}

                var rewriter = new AsyncMethodToStateMachineRewriter(
                    method: method,
                    methodOrdinal: _methodOrdinal,
                    asyncMethodBuilderMemberCollection: _asyncMethodBuilderMemberCollection,
                    asyncIteratorInfo: new AsyncIteratorInfo(_promiseOfValueOrEndField, setResultMethod, resetMethod, setExceptionMethod, _currentField, _promiseIsActiveField),
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
