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
        /// This rewriter rewrites an async-iterator method. See async-streams.md for design overview.
        /// </summary>
        internal sealed class AsyncIteratorRewriter : AsyncRewriter
        {
            private FieldSymbol _promiseOfValueOrEndField; // this struct implements the IValueTaskSource logic
            private FieldSymbol _currentField; // stores the current/yielded value
            private FieldSymbol _disposeModeField; // whether the state machine is in dispose mode (ie. skipping all logic except that in `catch` and `finally`, yielding no new elements)
            private FieldSymbol _combinedTokensField; // CancellationTokenSource for combining tokens

            // true if the iterator implements IAsyncEnumerable<T>,
            // false if it implements IAsyncEnumerator<T>
            private readonly bool _isEnumerable;

            internal AsyncIteratorRewriter(
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
                Debug.Assert(!TypeSymbol.Equals(method.IteratorElementTypeWithAnnotations.Type, null, TypeCompareKind.ConsiderEverything2));

                _isEnumerable = method.IsAsyncReturningIAsyncEnumerable(method.DeclaringCompilation);
                Debug.Assert(_isEnumerable != method.IsAsyncReturningIAsyncEnumerator(method.DeclaringCompilation));
            }

            protected override void VerifyPresenceOfRequiredAPIs(BindingDiagnosticBag bag)
            {
                base.VerifyPresenceOfRequiredAPIs(bag);

                if (_isEnumerable)
                {
                    EnsureWellKnownMember(WellKnownMember.System_Collections_Generic_IAsyncEnumerable_T__GetAsyncEnumerator, bag);
                    EnsureWellKnownMember(WellKnownMember.System_Threading_CancellationToken__Equals, bag);
                    EnsureWellKnownMember(WellKnownMember.System_Threading_CancellationTokenSource__CreateLinkedTokenSource, bag);
                    EnsureWellKnownMember(WellKnownMember.System_Threading_CancellationTokenSource__Token, bag);
                    EnsureWellKnownMember(WellKnownMember.System_Threading_CancellationTokenSource__Dispose, bag);
                }

                EnsureWellKnownMember(WellKnownMember.System_Collections_Generic_IAsyncEnumerator_T__MoveNextAsync, bag);
                EnsureWellKnownMember(WellKnownMember.System_Collections_Generic_IAsyncEnumerator_T__get_Current, bag);

                EnsureWellKnownMember(WellKnownMember.System_IAsyncDisposable__DisposeAsync, bag);
                EnsureWellKnownMember(WellKnownMember.System_Threading_Tasks_ValueTask_T__ctorSourceAndToken, bag);
                EnsureWellKnownMember(WellKnownMember.System_Threading_Tasks_ValueTask_T__ctorValue, bag);
                EnsureWellKnownMember(WellKnownMember.System_Threading_Tasks_ValueTask__ctor, bag);

                EnsureWellKnownMember(WellKnownMember.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__GetResult, bag);
                EnsureWellKnownMember(WellKnownMember.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__GetStatus, bag);
                EnsureWellKnownMember(WellKnownMember.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__get_Version, bag);
                EnsureWellKnownMember(WellKnownMember.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__OnCompleted, bag);
                EnsureWellKnownMember(WellKnownMember.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__Reset, bag);
                EnsureWellKnownMember(WellKnownMember.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__SetException, bag);
                EnsureWellKnownMember(WellKnownMember.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__SetResult, bag);

                EnsureWellKnownMember(WellKnownMember.System_Threading_Tasks_Sources_IValueTaskSource_T__GetResult, bag);
                EnsureWellKnownMember(WellKnownMember.System_Threading_Tasks_Sources_IValueTaskSource_T__GetStatus, bag);
                EnsureWellKnownMember(WellKnownMember.System_Threading_Tasks_Sources_IValueTaskSource_T__OnCompleted, bag);

                EnsureWellKnownMember(WellKnownMember.System_Threading_Tasks_Sources_IValueTaskSource__GetResult, bag);
                EnsureWellKnownMember(WellKnownMember.System_Threading_Tasks_Sources_IValueTaskSource__GetStatus, bag);
                EnsureWellKnownMember(WellKnownMember.System_Threading_Tasks_Sources_IValueTaskSource__OnCompleted, bag);
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
                GenerateIValueTaskSourceBoolImplementation_GetResult();
                GenerateIValueTaskSourceBoolImplementation_GetStatus();
                GenerateIValueTaskSourceBoolImplementation_OnCompleted();

                // IValueTaskSource
                GenerateIValueTaskSourceImplementation_GetResult();
                GenerateIValueTaskSourceImplementation_GetStatus();
                GenerateIValueTaskSourceImplementation_OnCompleted();

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
            }

            protected override void GenerateConstructor()
            {
                // Produces:
                // .ctor(int state)
                // {
                //     this.builder = System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Create();
                //     this.state = state;
                //     this.initialThreadId = {managedThreadId};
                // }
                Debug.Assert(stateMachineType.Constructor is IteratorConstructor);

                F.CurrentFunction = stateMachineType.Constructor;
                var bodyBuilder = ArrayBuilder<BoundStatement>.GetInstance();
                bodyBuilder.Add(F.BaseInitialization());

                // this.builder = System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Create();
                bodyBuilder.Add(GenerateCreateAndAssignBuilder());

                bodyBuilder.Add(F.Assignment(F.InstanceField(stateField), F.Parameter(F.CurrentFunction.Parameters[0]))); // this.state = state;

                var managedThreadId = MakeCurrentThreadId();
                if ((object)initialThreadIdField != null)
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

            private BoundExpressionStatement GenerateCreateAndAssignBuilder()
            {
                // Produce:
                // this.builder = System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Create();
                return F.Assignment(
                    F.InstanceField(_builderField),
                    F.StaticCall(
                        null,
                        _asyncMethodBuilderMemberCollection.CreateBuilder));
            }

            protected override void InitializeStateMachine(ArrayBuilder<BoundStatement> bodyBuilder, NamedTypeSymbol frameType, LocalSymbol stateMachineLocal)
            {
                // var stateMachineLocal = new {StateMachineType}({initialState})
                var initialState = _isEnumerable ? StateMachineState.FinishedState : StateMachineState.InitialAsyncIteratorState;
                bodyBuilder.Add(
                    F.Assignment(
                        F.Local(stateMachineLocal),
                        F.New(stateMachineType.Constructor.AsMember(frameType), F.Literal(initialState))));
            }

            protected override BoundStatement InitializeParameterField(MethodSymbol getEnumeratorMethod, ParameterSymbol parameter, BoundExpression resultParameter, BoundExpression parameterProxy)
            {
                return InitializeParameterField(getEnumeratorMethod, parameter, resultParameter, parameterProxy, _combinedTokensField, F);
            }

            internal static BoundStatement InitializeParameterField(MethodSymbol getEnumeratorMethod, ParameterSymbol parameter, BoundExpression resultParameter, BoundExpression parameterProxy,
                FieldSymbol combinedTokensField, SyntheticBoundNodeFactory f)
            {
                Debug.Assert(parameterProxy.Type is not null);

                BoundStatement result;
                if (combinedTokensField is object &&
                    !parameter.IsExtensionParameterImplementation() &&
                    parameter.HasEnumeratorCancellationAttribute &&
                    parameter.Type.Equals(f.Compilation.GetWellKnownType(WellKnownType.System_Threading_CancellationToken), TypeCompareKind.ConsiderEverything))
                {
                    // For a parameter of type CancellationToken with [EnumeratorCancellation]
                    // if (this.parameterProxy.Equals(default))
                    // {
                    //     result.parameter = token;
                    // }
                    // else if (token.Equals(this.parameterProxy) || token.Equals(default))
                    // {
                    //     result.parameter = this.parameterProxy;
                    // }
                    // else
                    // {
                    //     result.combinedTokens = CancellationTokenSource.CreateLinkedTokenSource(this.parameterProxy, token);
                    //     result.parameter = combinedTokens.Token;
                    // }

                    BoundParameter tokenParameter = f.Parameter(getEnumeratorMethod.Parameters[0]);
                    BoundFieldAccess combinedTokens = f.Field(f.This(), combinedTokensField);
                    result = f.If(
                        // if (this.parameterProxy.Equals(default))
                        f.Call(parameterProxy, WellKnownMember.System_Threading_CancellationToken__Equals, f.Default(parameterProxy.Type)),
                        // result.parameter = token;
                        thenClause: f.Assignment(resultParameter, tokenParameter),
                        elseClauseOpt: f.If(
                            // else if (token.Equals(this.parameterProxy) || token.Equals(default))
                            f.LogicalOr(
                                f.Call(tokenParameter, WellKnownMember.System_Threading_CancellationToken__Equals, parameterProxy),
                                f.Call(tokenParameter, WellKnownMember.System_Threading_CancellationToken__Equals, f.Default(tokenParameter.Type))),
                            // result.parameter = this.parameterProxy;
                            thenClause: f.Assignment(resultParameter, parameterProxy),
                            elseClauseOpt: f.Block(
                                // result.combinedTokens = CancellationTokenSource.CreateLinkedTokenSource(this.parameterProxy, token);
                                f.Assignment(combinedTokens, f.StaticCall(WellKnownMember.System_Threading_CancellationTokenSource__CreateLinkedTokenSource, parameterProxy, tokenParameter)),
                                // result.parameter = result.combinedTokens.Token;
                                f.Assignment(resultParameter, f.Property(combinedTokens, WellKnownMember.System_Threading_CancellationTokenSource__Token)))));
                }
                else
                {
                    // For parameters that don't have [EnumeratorCancellation], initialize their parameter fields
                    // result.parameter = this.parameterProxy;
                    result = f.Assignment(resultParameter, parameterProxy);
                }

                return result;
            }

            protected override BoundStatement GenerateStateMachineCreation(LocalSymbol stateMachineVariable, NamedTypeSymbol frameType, IReadOnlyDictionary<Symbol, CapturedSymbolReplacement> proxies)
            {
                var bodyBuilder = ArrayBuilder<BoundStatement>.GetInstance();

                bodyBuilder.Add(GenerateParameterStorage(stateMachineVariable, proxies));

                // return local;
                bodyBuilder.Add(
                    F.Return(
                        F.Local(stateMachineVariable)));

                return F.Block(bodyBuilder.ToImmutableAndFree());
            }

            /// <summary>
            /// Generates the `ValueTask&lt;bool> MoveNextAsync()` method.
            /// </summary>
            [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "Standard naming convention for generating 'IAsyncEnumerator.MoveNextAsync'")]
            private void GenerateIAsyncEnumeratorImplementation_MoveNextAsync()
            {
                // Produce:
                //  if (state == StateMachineStates.FinishedStateMachine)
                //  {
                //      return default;
                //  }
                //  _valueOrEndPromise.Reset();
                //  var inst = this;
                //  _builder.Start(ref inst);
                //  var version = _valueOrEndPromise.Version;
                //  if (_valueOrEndPromise.GetStatus(version) == ValueTaskSourceStatus.Succeeded)
                //  {
                //      return new ValueTask<bool>(_valueOrEndPromise.GetResult(version));
                //  }
                //  return new ValueTask<bool>(this, version);

                NamedTypeSymbol IAsyncEnumeratorOfElementType =
                    F.WellKnownType(WellKnownType.System_Collections_Generic_IAsyncEnumerator_T)
                    .Construct(_currentField.Type);

                MethodSymbol IAsyncEnumerableOfElementType_MoveNextAsync = F.WellKnownMethod(WellKnownMember.System_Collections_Generic_IAsyncEnumerator_T__MoveNextAsync)
                    .AsMember(IAsyncEnumeratorOfElementType);

                var promiseType = (NamedTypeSymbol)_promiseOfValueOrEndField.Type;

                MethodSymbol promise_GetStatus = F.WellKnownMethod(WellKnownMember.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__GetStatus)
                    .AsMember(promiseType);

                MethodSymbol promise_GetResult = F.WellKnownMethod(WellKnownMember.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__GetResult)
                    .AsMember(promiseType);

                var moveNextAsyncReturnType = (NamedTypeSymbol)IAsyncEnumerableOfElementType_MoveNextAsync.ReturnType;

                MethodSymbol valueTaskT_ctorValue = F.WellKnownMethod(WellKnownMember.System_Threading_Tasks_ValueTask_T__ctorValue)
                    .AsMember(moveNextAsyncReturnType);

                MethodSymbol valueTaskT_ctor = F.WellKnownMethod(WellKnownMember.System_Threading_Tasks_ValueTask_T__ctorSourceAndToken)
                    .AsMember(moveNextAsyncReturnType);

                // The implementation doesn't depend on the method body of the iterator method.
                OpenMethodImplementation(IAsyncEnumerableOfElementType_MoveNextAsync, hasMethodBodyDependency: false);

                GetPartsForStartingMachine(out BoundExpressionStatement callReset,
                    out LocalSymbol instSymbol,
                    out BoundStatement instAssignment,
                    out BoundExpressionStatement startCall,
                    out MethodSymbol promise_get_Version);

                BoundStatement ifFinished = F.If(
                    // if (state == StateMachineStates.FinishedStateMachine)
                    F.IntEqual(F.InstanceField(stateField), F.Literal(StateMachineState.FinishedState)),
                    // return default;
                    thenClause: F.Return(F.Default(moveNextAsyncReturnType)));

                // var version = _valueOrEndPromise.Version;
                var versionSymbol = F.SynthesizedLocal(F.SpecialType(SpecialType.System_Int16));
                var versionLocal = F.Local(versionSymbol);
                var versionInit = F.Assignment(versionLocal, F.Call(F.Field(F.This(), _promiseOfValueOrEndField), promise_get_Version));

                var ifPromiseReady = F.If(
                    // if (_valueOrEndPromise.GetStatus(version) == ValueTaskSourceStatus.Succeeded)
                    F.IntEqual(
                        F.Call(F.Field(F.This(), _promiseOfValueOrEndField), promise_GetStatus, versionLocal),
                        F.Literal(1)),
                    // return new ValueTask<bool>(_valueOrEndPromise.GetResult(version));
                    thenClause: F.Return(F.New(valueTaskT_ctorValue, F.Call(F.Field(F.This(), _promiseOfValueOrEndField), promise_GetResult, versionLocal))));

                // return new ValueTask<bool>(this, version);
                // Note: we fall back to this slower method of returning when the promise doesn't yet have a value.
                // This method of returning relies on two interface calls (`IValueTaskSource<bool>.GetStatus(version)` and `IValueTaskSource<bool>.GetResult(version)`).
                var returnStatement = F.Return(F.New(valueTaskT_ctor, F.This(), versionLocal));

                F.CloseMethod(F.Block(
                    ImmutableArray.Create(instSymbol, versionSymbol),
                    ifFinished,
                    callReset, // _promiseOfValueOrEnd.Reset();
                    instAssignment, // var inst = this;
                    startCall, // _builder.Start(ref inst);
                    versionInit,
                    ifPromiseReady,
                    returnStatement));
            }

            /// <summary>
            /// Prepares most of the parts for MoveNextAsync() and DisposeAsync() methods.
            /// </summary>
            private void GetPartsForStartingMachine(out BoundExpressionStatement callReset, out LocalSymbol instSymbol, out BoundStatement instAssignment,
                out BoundExpressionStatement startCall, out MethodSymbol promise_get_Version)
            {
                // Produce the following parts:
                // - _promiseOfValueOrEnd.Reset();
                // - var inst = this;
                // - _builder.Start(ref inst);
                // - _valueOrEndPromise.Version

                // _promiseOfValueOrEnd.Reset();
                BoundFieldAccess promiseField = F.InstanceField(_promiseOfValueOrEndField);
                var resetMethod = (MethodSymbol)F.WellKnownMethod(WellKnownMember.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__Reset, isOptional: true)
                    .SymbolAsMember((NamedTypeSymbol)_promiseOfValueOrEndField.Type);

                callReset = F.ExpressionStatement(F.Call(promiseField, resetMethod));

                // _builder.Start(ref inst);
                Debug.Assert(!_asyncMethodBuilderMemberCollection.CheckGenericMethodConstraints);
                MethodSymbol startMethod = _asyncMethodBuilderMemberCollection.Start.Construct(this.stateMachineType);
                instSymbol = F.SynthesizedLocal(this.stateMachineType);

                // var inst = this;
                var instLocal = F.Local(instSymbol);
                instAssignment = F.Assignment(instLocal, F.This());

                // _builder.Start(ref inst);
                startCall = F.ExpressionStatement(
                    F.Call(
                        F.InstanceField(_builderField),
                        startMethod,
                        ImmutableArray.Create<BoundExpression>(instLocal)));

                //  _valueOrEndPromise.Version
                promise_get_Version = F.WellKnownMethod(WellKnownMember.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__get_Version)
                    .AsMember((NamedTypeSymbol)_promiseOfValueOrEndField.Type);
            }

            /// <summary>
            /// Generates the `ValueTask IAsyncDisposable.DisposeAsync()` method.
            /// The DisposeAsync method should not be called from states -1 (running) or 0-and-up (awaits).
            /// </summary>
            [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "Standard naming convention for generating 'IAsyncDisposable.DisposeAsync'")]
            private void GenerateIAsyncDisposable_DisposeAsync()
            {
                // Produce:
                //  if (state >= StateMachineStates.NotStartedStateMachine /* -3 */)
                //  {
                //      throw new NotSupportedException();
                //  }
                //  if (state == StateMachineStates.FinishedStateMachine /* -2 */)
                //  {
                //      return default;
                //  }
                //  disposeMode = true;
                //  _valueOrEndPromise.Reset();
                //  var inst = this;
                //  _builder.Start(ref inst);
                //  return new ValueTask(this, _valueOrEndPromise.Version);

                MethodSymbol IAsyncDisposable_DisposeAsync = F.WellKnownMethod(WellKnownMember.System_IAsyncDisposable__DisposeAsync);

                // The implementation doesn't depend on the method body of the iterator method.
                OpenMethodImplementation(IAsyncDisposable_DisposeAsync, hasMethodBodyDependency: false);

                TypeSymbol returnType = IAsyncDisposable_DisposeAsync.ReturnType;

                GetPartsForStartingMachine(out BoundExpressionStatement callReset,
                    out LocalSymbol instSymbol,
                    out BoundStatement instAssignment,
                    out BoundExpressionStatement startCall,
                    out MethodSymbol promise_get_Version);

                BoundStatement ifInvalidState = F.If(
                    // if (state >= StateMachineStates.NotStartedStateMachine /* -1 */)
                    F.IntGreaterThanOrEqual(F.InstanceField(stateField), F.Literal(StateMachineState.NotStartedOrRunningState)),
                    // throw new NotSupportedException();
                    thenClause: F.Throw(F.New(F.WellKnownType(WellKnownType.System_NotSupportedException))));

                BoundStatement ifFinished = F.If(
                    // if (state == StateMachineStates.FinishedStateMachine)
                    F.IntEqual(F.InstanceField(stateField), F.Literal(StateMachineState.FinishedState)),
                    // return default;
                    thenClause: F.Return(F.Default(returnType)));

                MethodSymbol valueTask_ctor =
                    F.WellKnownMethod(WellKnownMember.System_Threading_Tasks_ValueTask__ctor)
                    .AsMember((NamedTypeSymbol)IAsyncDisposable_DisposeAsync.ReturnType);

                // return new ValueTask(this, _valueOrEndPromise.Version);
                var returnStatement = F.Return(F.New(valueTask_ctor, F.This(), F.Call(F.InstanceField(_promiseOfValueOrEndField), promise_get_Version)));

                F.CloseMethod(F.Block(
                    ImmutableArray.Create(instSymbol),
                    ifInvalidState,
                    ifFinished,
                    F.Assignment(F.InstanceField(_disposeModeField), F.Literal(true)), // disposeMode = true;
                    callReset, // _promiseOfValueOrEnd.Reset();
                    instAssignment, // var inst = this;
                    startCall, // _builder.Start(ref inst);
                    returnStatement));
            }

            /// <summary>
            /// Generates the Current property.
            /// </summary>
            private void GenerateIAsyncEnumeratorImplementation_Current()
            {
                GenerateIAsyncEnumeratorImplementation_Current(_currentField, this, F);
            }

            internal static void GenerateIAsyncEnumeratorImplementation_Current(FieldSymbol currentField, StateMachineRewriter stateMachineRewriter, SyntheticBoundNodeFactory f)
            {
                Debug.Assert(currentField is not null);
                // Produce the implementation for `T Current { get; }`:
                // return _current;

                NamedTypeSymbol IAsyncEnumeratorOfElementType =
                    f.WellKnownType(WellKnownType.System_Collections_Generic_IAsyncEnumerator_T)
                    .Construct(currentField.Type);

                MethodSymbol IAsyncEnumerableOfElementType_get_Current =
                    f.WellKnownMethod(WellKnownMember.System_Collections_Generic_IAsyncEnumerator_T__get_Current)
                    .AsMember(IAsyncEnumeratorOfElementType);

                stateMachineRewriter.OpenPropertyImplementation(IAsyncEnumerableOfElementType_get_Current);

                f.CloseMethod(f.Block(f.Return(f.InstanceField(currentField))));
            }

            private void GenerateIValueTaskSourceBoolImplementation_GetResult()
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
                    F.WellKnownMethod(WellKnownMember.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__GetResult)
                    .AsMember((NamedTypeSymbol)_promiseOfValueOrEndField.Type);

                // The implementation doesn't depend on the method body of the iterator method.
                OpenMethodImplementation(IValueTaskSourceOfBool_GetResult, hasMethodBodyDependency: false);

                // return this._valueOrEndPromise.GetResult(token);
                F.CloseMethod(F.Return(
                    F.Call(F.InstanceField(_promiseOfValueOrEndField), promise_GetResult, F.Parameter(IValueTaskSourceOfBool_GetResult.Parameters[0]))));
            }

            private void GenerateIValueTaskSourceBoolImplementation_GetStatus()
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
                    F.WellKnownMethod(WellKnownMember.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__GetStatus)
                    .AsMember((NamedTypeSymbol)_promiseOfValueOrEndField.Type);

                // The implementation doesn't depend on the method body of the iterator method.
                OpenMethodImplementation(IValueTaskSourceOfBool_GetStatus, hasMethodBodyDependency: false);

                // return this._valueOrEndPromise.GetStatus(token);
                F.CloseMethod(F.Return(
                    F.Call(F.InstanceField(_promiseOfValueOrEndField), promise_GetStatus, F.Parameter(IValueTaskSourceOfBool_GetStatus.Parameters[0]))));
            }

            private void GenerateIValueTaskSourceBoolImplementation_OnCompleted()
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
                    F.WellKnownMethod(WellKnownMember.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__OnCompleted)
                    .AsMember((NamedTypeSymbol)_promiseOfValueOrEndField.Type);

                // The implementation doesn't depend on the method body of the iterator method.
                OpenMethodImplementation(IValueTaskSourceOfBool_OnCompleted, hasMethodBodyDependency: false);

                F.CloseMethod(F.Block(
                    // this._valueOrEndPromise.OnCompleted(continuation, state, token, flags);
                    F.ExpressionStatement(
                        F.Call(F.InstanceField(_promiseOfValueOrEndField), promise_OnCompleted,
                            F.Parameter(IValueTaskSourceOfBool_OnCompleted.Parameters[0]),
                            F.Parameter(IValueTaskSourceOfBool_OnCompleted.Parameters[1]),
                            F.Parameter(IValueTaskSourceOfBool_OnCompleted.Parameters[2]),
                            F.Parameter(IValueTaskSourceOfBool_OnCompleted.Parameters[3]))),
                    F.Return())); // return;
            }

            private void GenerateIValueTaskSourceImplementation_GetResult()
            {
                // Produce an implementation for `void IValueTaskSource.GetResult(short token)`:
                //  this._valueOrEndPromise.GetResult(token);
                //  return;

                MethodSymbol IValueTaskSource_GetResult =
                    F.WellKnownMethod(WellKnownMember.System_Threading_Tasks_Sources_IValueTaskSource__GetResult);

                MethodSymbol promise_GetResult =
                    F.WellKnownMethod(WellKnownMember.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__GetResult)
                    .AsMember((NamedTypeSymbol)_promiseOfValueOrEndField.Type);

                // The implementation doesn't depend on the method body of the iterator method.
                OpenMethodImplementation(IValueTaskSource_GetResult, hasMethodBodyDependency: false);

                F.CloseMethod(F.Block(
                    // this._valueOrEndPromise.GetResult(token);
                    F.ExpressionStatement(F.Call(F.InstanceField(_promiseOfValueOrEndField), promise_GetResult, F.Parameter(IValueTaskSource_GetResult.Parameters[0]))),
                    // return;
                    F.Return()));
            }

            // Consider factoring with IValueTaskSource<bool> implementation
            // See https://github.com/dotnet/roslyn/issues/31517
            private void GenerateIValueTaskSourceImplementation_GetStatus()
            {
                // Produce the implementation for `ValueTaskSourceStatus IValueTaskSource.GetStatus(short token)`:
                // return this._valueOrEndPromise.GetStatus(token);

                MethodSymbol IValueTaskSource_GetStatus =
                    F.WellKnownMethod(WellKnownMember.System_Threading_Tasks_Sources_IValueTaskSource__GetStatus);

                MethodSymbol promise_GetStatus =
                    F.WellKnownMethod(WellKnownMember.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__GetStatus)
                    .AsMember((NamedTypeSymbol)_promiseOfValueOrEndField.Type);

                // The implementation doesn't depend on the method body of the iterator method.
                OpenMethodImplementation(IValueTaskSource_GetStatus, hasMethodBodyDependency: false);

                // return this._valueOrEndPromise.GetStatus(token);
                F.CloseMethod(F.Return(
                    F.Call(F.InstanceField(_promiseOfValueOrEndField), promise_GetStatus, F.Parameter(IValueTaskSource_GetStatus.Parameters[0]))));
            }

            // Consider factoring with IValueTaskSource<bool> implementation
            // See https://github.com/dotnet/roslyn/issues/31517
            private void GenerateIValueTaskSourceImplementation_OnCompleted()
            {
                // Produce the implementation for `void IValueTaskSource.OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)`:
                // this._valueOrEndPromise.OnCompleted(continuation, state, token, flags);
                // return;

                MethodSymbol IValueTaskSource_OnCompleted = F.WellKnownMethod(WellKnownMember.System_Threading_Tasks_Sources_IValueTaskSource__OnCompleted);

                MethodSymbol promise_OnCompleted =
                    F.WellKnownMethod(WellKnownMember.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T__OnCompleted)
                    .AsMember((NamedTypeSymbol)_promiseOfValueOrEndField.Type);

                // The implementation doesn't depend on the method body of the iterator method.
                OpenMethodImplementation(IValueTaskSource_OnCompleted, hasMethodBodyDependency: false);

                F.CloseMethod(F.Block(
                    // this._valueOrEndPromise.OnCompleted(continuation, state, token, flags);
                    F.ExpressionStatement(
                        F.Call(F.InstanceField(_promiseOfValueOrEndField), promise_OnCompleted,
                            F.Parameter(IValueTaskSource_OnCompleted.Parameters[0]),
                            F.Parameter(IValueTaskSource_OnCompleted.Parameters[1]),
                            F.Parameter(IValueTaskSource_OnCompleted.Parameters[2]),
                            F.Parameter(IValueTaskSource_OnCompleted.Parameters[3]))),
                    F.Return())); // return;
            }

            /// <summary>
            /// Generates the GetAsyncEnumerator method.
            /// </summary>
            private void GenerateIAsyncEnumerableImplementation_GetAsyncEnumerator()
            {
                NamedTypeSymbol IAsyncEnumerableOfElementType =
                    F.WellKnownType(WellKnownType.System_Collections_Generic_IAsyncEnumerable_T)
                    .Construct(_currentField.Type);

                MethodSymbol IAsyncEnumerableOfElementType_GetEnumerator =
                    F.WellKnownMethod(WellKnownMember.System_Collections_Generic_IAsyncEnumerable_T__GetAsyncEnumerator)
                    .AsMember(IAsyncEnumerableOfElementType);

                BoundExpression managedThreadId = null;
                GenerateIteratorGetEnumerator(IAsyncEnumerableOfElementType_GetEnumerator, ref managedThreadId, initialState: StateMachineState.InitialAsyncIteratorState);
            }

            protected override void GenerateResetInstance(ArrayBuilder<BoundStatement> builder, StateMachineState initialState)
            {
                // this.state = {initialState};
                // this.builder = System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Create();
                // this.disposeMode = false;

                builder.Add(
                    // this.state = {initialState};
                    F.Assignment(F.Field(F.This(), stateField), F.Literal(initialState)));

                builder.Add(
                    // this.builder = System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Create();
                    GenerateCreateAndAssignBuilder());

                builder.Add(
                    // disposeMode = false;
                    F.Assignment(F.InstanceField(_disposeModeField), F.Literal(false)));
            }

            protected override void GenerateMoveNext(SynthesizedImplementationMethod moveNextMethod)
            {
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
                    builder: _builderField,
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
