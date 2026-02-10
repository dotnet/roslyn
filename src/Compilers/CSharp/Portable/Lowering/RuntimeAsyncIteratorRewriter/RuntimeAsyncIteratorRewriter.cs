// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp;

/// <summary>
/// The entrypoint for this rewriter is <see cref="Rewrite"/>.
/// It delegates to <see cref="StateMachineRewriter.Rewrite"/> which drives the process relying on various overrides
/// to produce a nested type <see cref="RuntimeAsyncIteratorStateMachine"/> with:
/// - fields:
///   - <see cref="GenerateControlFields"/> for `current`, `disposeMode`, `initialThreadId` fields
///   - proxies for `this`, parameters and hoisted locals
/// - members including interface implementations:
///   - <see cref="GenerateConstructor"/>
///   - <see cref="GenerateIAsyncEnumerableImplementation_GetAsyncEnumerator"/> (for enumerable case)
///   - <see cref="GenerateIAsyncEnumeratorImplementation_MoveNextAsync"/> which relies heavily on <see cref="MoveNextAsyncRewriter"/>
///   - <see cref="GenerateIAsyncEnumeratorImplementation_Current"/>
///   - <see cref="GenerateIAsyncDisposable_DisposeAsync"/>
/// before returning the body for the kickoff method (<see cref="StateMachineRewriter.GenerateKickoffMethodBody"/>).
///
/// The state machine uses the same states as regular async-iterator methods, with the exception of `await` states
/// which are no longer needed (handled by the runtime directly).
/// </summary>
internal sealed partial class RuntimeAsyncIteratorRewriter : StateMachineRewriter
{
    // true if the iterator implements IAsyncEnumerable<T>,
    // false if it only implements IAsyncEnumerator<T>
    private readonly bool _isEnumerable;

    private FieldSymbol? _currentField; // stores the current/yielded value
    private FieldSymbol? _disposeModeField; // whether the state machine is in dispose mode (ie. skipping all logic except that in `catch` and `finally`, yielding no new elements)
    private FieldSymbol? _combinedTokensField; // CancellationTokenSource for combining tokens (only set for enumerable async-iterators with [EnumeratorCancellation] parameter)

    public RuntimeAsyncIteratorRewriter(
        BoundStatement body,
        MethodSymbol method,
        SynthesizedContainer stateMachineType,
        ArrayBuilder<StateMachineStateDebugInfo> stateMachineStateDebugInfoBuilder,
        VariableSlotAllocator? slotAllocatorOpt,
        TypeCompilationState compilationState,
        BindingDiagnosticBag diagnostics)
        : base(body, method, stateMachineType, stateMachineStateDebugInfoBuilder, slotAllocatorOpt, compilationState, diagnostics)
    {
        Debug.Assert(method.IteratorElementTypeWithAnnotations.Type is not null);

        _isEnumerable = method.IsAsyncReturningIAsyncEnumerable(method.DeclaringCompilation);
        Debug.Assert(_isEnumerable != method.IsAsyncReturningIAsyncEnumerator(method.DeclaringCompilation));
    }

    public static BoundStatement Rewrite(
        BoundStatement bodyWithAwaitLifted,
        MethodSymbol method,
        int methodOrdinal,
        ArrayBuilder<StateMachineStateDebugInfo> stateMachineStateDebugInfoBuilder,
        VariableSlotAllocator? slotAllocatorOpt,
        TypeCompilationState compilationState,
        BindingDiagnosticBag diagnostics,
        out RuntimeAsyncIteratorStateMachine? stateMachineType)
    {
        Debug.Assert(compilationState.ModuleBuilderOpt != null);
        Debug.Assert(method.DeclaringCompilation.IsValidRuntimeAsyncIteratorReturnType(method.ReturnType));
        Debug.Assert(method.IsAsync);

        TypeWithAnnotations elementType = method.IteratorElementTypeWithAnnotations;
        Debug.Assert(!elementType.IsDefault);

        bool isEnumerable = method.IsAsyncReturningIAsyncEnumerable(method.DeclaringCompilation);
        stateMachineType = new RuntimeAsyncIteratorStateMachine(slotAllocatorOpt, compilationState, method, methodOrdinal, isEnumerable: isEnumerable, elementType);
        compilationState.ModuleBuilderOpt.CompilationState.SetStateMachineType(method, stateMachineType);

        var rewriter = new RuntimeAsyncIteratorRewriter(bodyWithAwaitLifted, method, stateMachineType, stateMachineStateDebugInfoBuilder, slotAllocatorOpt, compilationState, diagnostics);

        if (!rewriter.VerifyPresenceOfRequiredAPIs())
        {
            return bodyWithAwaitLifted;
        }

        try
        {
            return rewriter.Rewrite();
        }
        catch (SyntheticBoundNodeFactory.MissingPredefinedMember ex)
        {
            diagnostics.Add(ex.Diagnostic);
            return new BoundBadStatement(bodyWithAwaitLifted.Syntax, [bodyWithAwaitLifted], hasErrors: true);
        }
    }

    // For async-enumerables, it is possible for an instance to be re-used as enumerator for multiple iterations
    // so we need to preserve initial parameter values and thread ID across iterations.
    protected override bool PreserveInitialParameterValuesAndThreadId
        => _isEnumerable;

    protected override void GenerateControlFields()
    {
        // the fields are initialized from async method, so they need to be public:
        stateField = F.StateMachineField(F.SpecialType(SpecialType.System_Int32), GeneratedNames.MakeStateMachineStateFieldName(), isPublic: true);

        // the element type may contain method type parameters, which are now alpha-renamed into type parameters of the generated class
        TypeSymbol elementType = ((RuntimeAsyncIteratorStateMachine)stateMachineType).ElementType.Type;

        // Add a field: T current
        _currentField = F.StateMachineField(elementType, GeneratedNames.MakeIteratorCurrentFieldName());

        // Add a field: bool disposeMode
        NamedTypeSymbol boolType = F.SpecialType(SpecialType.System_Boolean);
        _disposeModeField = F.StateMachineField(boolType, GeneratedNames.MakeDisposeModeFieldName());

        if (_isEnumerable && this.method.Parameters.Any(static p => !p.IsExtensionParameterImplementation() && p.HasEnumeratorCancellationAttribute))
        {
            // Add a field: CancellationTokenSource combinedTokens
            _combinedTokensField = F.StateMachineField(
                F.WellKnownType(WellKnownType.System_Threading_CancellationTokenSource),
                GeneratedNames.MakeAsyncIteratorCombinedTokensFieldName());
        }

        Debug.Assert(F.ModuleBuilderOpt is not null);
        var instrumentations = F.ModuleBuilderOpt.GetMethodBodyInstrumentations(method);
        if (instrumentations.Kinds.Contains(InstrumentationKindExtensions.LocalStateTracing))
        {
            instanceIdField = F.StateMachineField(F.SpecialType(SpecialType.System_UInt64), GeneratedNames.MakeStateMachineStateIdFieldName(), isPublic: true);
        }
    }

    /// <returns>
    /// Returns true if all types and members we need are present and good
    /// </returns>
    private bool VerifyPresenceOfRequiredAPIs()
    {
        var bag = BindingDiagnosticBag.GetInstance(withDiagnostics: true, diagnostics.AccumulatesDependencies);

        verifyPresenceOfRequiredAPIs(bag);

        bool hasErrors = bag.HasAnyErrors();
        if (!hasErrors)
        {
            diagnostics.AddDependencies(bag);
        }
        else
        {
            diagnostics.AddRange(bag);
        }

        bag.Free();
        return !hasErrors;

        void verifyPresenceOfRequiredAPIs(BindingDiagnosticBag bag)
        {
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

            ensureSpecialMember(SpecialMember.System_Runtime_CompilerServices_AsyncHelpers__Await_T, bag);

            Symbol ensureSpecialMember(SpecialMember member, BindingDiagnosticBag bag)
            {
                return Binder.GetSpecialTypeMember(F.Compilation, member, bag, body.Syntax);
            }
        }
    }

    protected override void GenerateMethodImplementations()
    {
        GenerateConstructor();

        if (_isEnumerable)
        {
            // IAsyncEnumerable
            GenerateIAsyncEnumerableImplementation_GetAsyncEnumerator();
        }

        // IAsyncEnumerator
        GenerateIAsyncEnumeratorImplementation_MoveNextAsync();
        GenerateIAsyncEnumeratorImplementation_Current();

        // IAsyncDisposable
        GenerateIAsyncDisposable_DisposeAsync();
    }

    private void GenerateConstructor()
    {
        Debug.Assert(stateMachineType.Constructor is IteratorConstructor);
        Debug.Assert(stateField is not null);

        // Produces:
        // .ctor(int state)
        // {
        //     this.state = state;
        //     this.initialThreadId = {managedThreadId};
        //     this.instanceId = LocalStoreTracker.GetNewStateMachineInstanceId();
        // }
        Debug.Assert(stateMachineType.Constructor is IteratorConstructor);

        F.CurrentFunction = stateMachineType.Constructor;
        var blockBuilder = ArrayBuilder<BoundStatement>.GetInstance();
        blockBuilder.Add(F.BaseInitialization());

        blockBuilder.Add(F.Assignment(F.InstanceField(stateField), F.Parameter(F.CurrentFunction.Parameters[0]))); // this.state = state;

        BoundExpression managedThreadId = MakeCurrentThreadId();
        if (initialThreadIdField is not null)
        {
            // this.initialThreadId = {managedThreadId};
            blockBuilder.Add(F.Assignment(F.InstanceField(initialThreadIdField), managedThreadId));
        }

        if (instanceIdField is not null &&
            F.WellKnownMethod(WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__GetNewStateMachineInstanceId) is { } getId)
        {
            // this.instanceId = LocalStoreTracker.GetNewStateMachineInstanceId();
            blockBuilder.Add(F.Assignment(F.InstanceField(instanceIdField), F.Call(receiver: null, getId)));
        }

        blockBuilder.Add(F.Return());

        var block = F.Block(blockBuilder.ToImmutableAndFree());
        F.CloseMethod(block);
    }

    /// <summary>
    /// Generates the GetAsyncEnumerator method.
    /// </summary>
    private void GenerateIAsyncEnumerableImplementation_GetAsyncEnumerator()
    {
        Debug.Assert(_currentField is not null);

        NamedTypeSymbol IAsyncEnumerableOfElementType =
            F.WellKnownType(WellKnownType.System_Collections_Generic_IAsyncEnumerable_T)
            .Construct(_currentField.Type);

        MethodSymbol IAsyncEnumerableOfElementType_GetEnumerator =
            F.WellKnownMethod(WellKnownMember.System_Collections_Generic_IAsyncEnumerable_T__GetAsyncEnumerator)
            .AsMember(IAsyncEnumerableOfElementType);

        BoundExpression? managedThreadId = null;
        GenerateIteratorGetEnumerator(IAsyncEnumerableOfElementType_GetEnumerator, ref managedThreadId, initialState: StateMachineState.InitialAsyncIteratorState);
    }

    protected override void GenerateResetInstance(ArrayBuilder<BoundStatement> builder, StateMachineState initialState)
    {
        Debug.Assert(stateField is not null);
        Debug.Assert(_disposeModeField is not null);

        // this.state = {initialState};
        // this.disposeMode = false;

        builder.Add(
            // this.state = {initialState};
            F.Assignment(F.Field(F.This(), stateField), F.Literal(initialState)));

        builder.Add(
            // disposeMode = false;
            F.Assignment(F.InstanceField(_disposeModeField), F.Literal(false)));
    }

    protected override BoundStatement InitializeParameterField(MethodSymbol getEnumeratorMethod, ParameterSymbol parameter, BoundExpression resultParameter, BoundExpression parameterProxy)
    {
        return AsyncRewriter.AsyncIteratorRewriter.InitializeParameterField(getEnumeratorMethod, parameter, resultParameter, parameterProxy, _combinedTokensField, F);
    }

    /// <summary>
    /// Generates the `ValueTask&gt;bool> IAsyncEnumerator&gt;ElementType>.MoveNextAsync()` method as a runtime-async method.
    /// </summary>
    [SuppressMessage("Style", """VSTHRD200:Use "Async" suffix for async methods""", Justification = "Standard naming convention for generating 'IAsyncEnumerator.MoveNextAsync'")]
    private void GenerateIAsyncEnumeratorImplementation_MoveNextAsync()
    {
        Debug.Assert(stateField is not null);
        Debug.Assert(_currentField is not null);
        Debug.Assert(_disposeModeField is not null);
        Debug.Assert(hoistedVariables is not null);
        Debug.Assert(nonReusableLocalProxies is not null);

        // Add IAsyncEnumerator<...>.MoveNextAsync() as a runtime-async method.
        MethodSymbol IAsyncEnumeratorOfElementType_MoveNextAsync = GetMoveNextAsyncMethod();
        OpenMoveNextMethodImplementation(IAsyncEnumeratorOfElementType_MoveNextAsync, runtimeAsync: true);

        var rewriter = new MoveNextAsyncRewriter(
             F,
             method,
             stateField,
             instanceIdField,
             hoistedVariables,
             nonReusableLocalProxies,
             nonReusableFieldsForCleanup,
             synthesizedLocalOrdinals,
             stateMachineStateDebugInfoBuilder,
             slotAllocatorOpt,
             nextFreeHoistedLocalSlot,
             diagnostics,
             _currentField,
             _disposeModeField,
             _combinedTokensField);

        BoundStatement runtimeAsyncMoveNextAsyncBody = rewriter.GenerateMoveNextAsync(body);

        Debug.Assert(F.CurrentFunction is not null);
        BoundStatement rewrittenBody = RuntimeAsyncRewriter.RewriteWithoutHoisting(runtimeAsyncMoveNextAsyncBody, F.CurrentFunction, F.CompilationState, F.Diagnostics);

        F.CloseMethod(rewrittenBody);
    }

    private MethodSymbol GetMoveNextAsyncMethod()
    {
        Debug.Assert(_currentField is not null);

        MethodSymbol IAsyncEnumerator_MoveNextAsync = F.WellKnownMethod(WellKnownMember.System_Collections_Generic_IAsyncEnumerator_T__MoveNextAsync);
        var IAsyncEnumerator = IAsyncEnumerator_MoveNextAsync.ContainingType;
        var IAsyncEnumeratorOfElementType = IAsyncEnumerator.Construct(_currentField.Type);
        MethodSymbol IAsyncEnumeratorOfElementType_MoveNextAsync = IAsyncEnumerator_MoveNextAsync.AsMember(IAsyncEnumeratorOfElementType);

        Debug.Assert(IAsyncEnumeratorOfElementType_MoveNextAsync.ReturnType.OriginalDefinition.ExtendedSpecialType == InternalSpecialType.System_Threading_Tasks_ValueTask_T);
        return IAsyncEnumeratorOfElementType_MoveNextAsync;
    }

    /// <summary>
    /// Generates the Current property.
    /// </summary>
    private void GenerateIAsyncEnumeratorImplementation_Current()
    {
        AsyncRewriter.AsyncIteratorRewriter.GenerateIAsyncEnumeratorImplementation_Current(_currentField, this, F);
    }

    /// <summary>
    /// Generates the `ValueTask IAsyncDisposable.DisposeAsync()` method as a runtime-async method.
    /// </summary>
    [SuppressMessage("Style", """VSTHRD200:Use "Async" suffix for async methods""", Justification = "Standard naming convention for generating 'IAsyncDisposable.DisposeAsync'")]
    private void GenerateIAsyncDisposable_DisposeAsync()
    {
        // Produce:
        //  if (state >= StateMachineStates.NotStartedStateMachine /* -1 */)
        //  {
        //      throw new NotSupportedException();
        //  }
        //  if (state == StateMachineStates.FinishedStateMachine /* -2 */)
        //  {
        //      return;
        //  }
        //  disposeMode = true;
        //  AsyncHelpers.Await(MoveNextAsync());
        //  return;

        Debug.Assert(_currentField is not null);
        Debug.Assert(stateField is not null);
        Debug.Assert(_disposeModeField is not null);
        MethodSymbol IAsyncDisposable_DisposeAsync = F.WellKnownMethod(WellKnownMember.System_IAsyncDisposable__DisposeAsync);
        Debug.Assert(IAsyncDisposable_DisposeAsync.ReturnType.OriginalDefinition.ExtendedSpecialType == InternalSpecialType.System_Threading_Tasks_ValueTask);

        // The implementation doesn't depend on the method body of the iterator method.
        OpenMethodImplementation(IAsyncDisposable_DisposeAsync, hasMethodBodyDependency: false, isRuntimeAsync: true);
        var blockBuilder = ArrayBuilder<BoundStatement>.GetInstance();

        blockBuilder.Add(F.If(
            // if (state >= StateMachineStates.NotStartedStateMachine /* -1 */)
            F.IntGreaterThanOrEqual(F.InstanceField(stateField), F.Literal(StateMachineState.NotStartedOrRunningState)),
            //   throw new NotSupportedException();
            thenClause: F.Throw(F.New(F.WellKnownType(WellKnownType.System_NotSupportedException)))));

        blockBuilder.Add(F.If(
            // if (state == StateMachineStates.FinishedStateMachine)
            F.IntEqual(F.InstanceField(stateField), F.Literal(StateMachineState.FinishedState)),
            //   return;
            thenClause: F.Return()));

        // disposeMode = true;
        blockBuilder.Add(F.Assignment(F.InstanceField(_disposeModeField), F.Literal(true)));

        // AsyncHelpers.Await(MoveNextAsync());
        MethodSymbol IAsyncEnumeratorOfElementType_MoveNextAsync = GetMoveNextAsyncMethod();
        NamedTypeSymbol boolType = F.SpecialType(SpecialType.System_Boolean);
        var awaitMethod = ((MethodSymbol)F.Compilation.GetSpecialTypeMember(SpecialMember.System_Runtime_CompilerServices_AsyncHelpers__Await_T))
            .Construct(boolType);

        BoundCall awaitMoveNextAsync = F.Call(
            receiver: null,
            awaitMethod,
            F.Call(F.This(), IAsyncEnumeratorOfElementType_MoveNextAsync));

        blockBuilder.Add(F.ExpressionStatement(awaitMoveNextAsync));

        blockBuilder.Add(F.Return());

        BoundBlock block = F.Block(blockBuilder.ToImmutableAndFree());
        F.CloseMethod(block);
    }

    protected override BoundStatement GenerateStateMachineCreation(LocalSymbol stateMachineVariable, NamedTypeSymbol frameType, IReadOnlyDictionary<Symbol, CapturedSymbolReplacement> proxies)
    {
        var blockBuilder = ArrayBuilder<BoundStatement>.GetInstance();

        // result.parameter = this.parameterProxy; // OR more complex initialization for async-iterator parameter marked with [EnumeratorCancellation]
        blockBuilder.Add(GenerateParameterStorage(stateMachineVariable, proxies));

        // return local;
        blockBuilder.Add(F.Return(F.Local(stateMachineVariable)));

        return F.Block(blockBuilder.ToImmutableAndFree());
    }

    protected override void InitializeStateMachine(ArrayBuilder<BoundStatement> blockBuilder, NamedTypeSymbol frameType, LocalSymbol stateMachineLocal)
    {
        // stateMachineLocal = new {StateMachineType}(initialState);
        var initialState = _isEnumerable ? StateMachineState.FinishedState : StateMachineState.InitialAsyncIteratorState;
        blockBuilder.Add(F.Assignment(F.Local(stateMachineLocal), F.New(frameType.InstanceConstructors[0], F.Literal(initialState))));
    }
}
