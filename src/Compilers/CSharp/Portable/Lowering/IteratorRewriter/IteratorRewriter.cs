// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class IteratorRewriter : StateMachineRewriter
    {
        private readonly TypeWithAnnotations _elementType;

        // true if the iterator implements IEnumerable and IEnumerable<T>,
        // false if it implements IEnumerator and IEnumerator<T>
        private readonly bool _isEnumerable;

        private FieldSymbol _currentField;

        private IteratorRewriter(
            BoundStatement body,
            MethodSymbol method,
            bool isEnumerable,
            IteratorStateMachine stateMachineType,
            ArrayBuilder<StateMachineStateDebugInfo> stateMachineStateDebugInfoBuilder,
            VariableSlotAllocator slotAllocatorOpt,
            TypeCompilationState compilationState,
            BindingDiagnosticBag diagnostics)
            : base(body, method, stateMachineType, stateMachineStateDebugInfoBuilder, slotAllocatorOpt, compilationState, diagnostics)
        {
            // the element type may contain method type parameters, which are now alpha-renamed into type parameters of the generated class
            _elementType = stateMachineType.ElementType;

            _isEnumerable = isEnumerable;
        }

        /// <summary>
        /// Rewrite an iterator method into a state machine class.
        /// </summary>
        internal static BoundStatement Rewrite(
            BoundStatement body,
            MethodSymbol method,
            int methodOrdinal,
            ArrayBuilder<StateMachineStateDebugInfo> stateMachineStateDebugInfoBuilder,
            VariableSlotAllocator slotAllocatorOpt,
            TypeCompilationState compilationState,
            BindingDiagnosticBag diagnostics,
            out IteratorStateMachine stateMachineType)
        {
            TypeWithAnnotations elementType = method.IteratorElementTypeWithAnnotations;
            if (elementType.IsDefault || method.IsAsync)
            {
                stateMachineType = null;
                return body;
            }

            // Figure out what kind of iterator we are generating.
            bool isEnumerable;
            switch (method.ReturnType.OriginalDefinition.SpecialType)
            {
                case SpecialType.System_Collections_IEnumerable:
                case SpecialType.System_Collections_Generic_IEnumerable_T:
                    isEnumerable = true;
                    break;

                case SpecialType.System_Collections_IEnumerator:
                case SpecialType.System_Collections_Generic_IEnumerator_T:
                    isEnumerable = false;
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(method.ReturnType.OriginalDefinition.SpecialType);
            }

            stateMachineType = new IteratorStateMachine(slotAllocatorOpt, compilationState, method, methodOrdinal, isEnumerable, elementType);
            compilationState.ModuleBuilderOpt.CompilationState.SetStateMachineType(method, stateMachineType);
            var rewriter = new IteratorRewriter(body, method, isEnumerable, stateMachineType, stateMachineStateDebugInfoBuilder, slotAllocatorOpt, compilationState, diagnostics);
            if (!rewriter.VerifyPresenceOfRequiredAPIs())
            {
                return body;
            }

            return rewriter.Rewrite();
        }

        /// <returns>
        /// Returns true if all types and members we need are present and good
        /// </returns>
        protected bool VerifyPresenceOfRequiredAPIs()
        {
            var bag = BindingDiagnosticBag.GetInstance(withDiagnostics: true, diagnostics.AccumulatesDependencies);

            EnsureSpecialType(SpecialType.System_Int32, bag);
            EnsureSpecialType(SpecialType.System_IDisposable, bag);
            EnsureSpecialMember(SpecialMember.System_IDisposable__Dispose, bag);

            // IEnumerator
            EnsureSpecialType(SpecialType.System_Collections_IEnumerator, bag);
            EnsureSpecialPropertyGetter(SpecialMember.System_Collections_IEnumerator__Current, bag);
            EnsureSpecialMember(SpecialMember.System_Collections_IEnumerator__MoveNext, bag);
            EnsureSpecialMember(SpecialMember.System_Collections_IEnumerator__Reset, bag);

            // IEnumerator<T>
            EnsureSpecialType(SpecialType.System_Collections_Generic_IEnumerator_T, bag);
            EnsureSpecialPropertyGetter(SpecialMember.System_Collections_Generic_IEnumerator_T__Current, bag);

            if (_isEnumerable)
            {
                // IEnumerable and IEnumerable<T>
                EnsureSpecialType(SpecialType.System_Collections_IEnumerable, bag);
                EnsureSpecialMember(SpecialMember.System_Collections_IEnumerable__GetEnumerator, bag);
                EnsureSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T, bag);
                EnsureSpecialMember(SpecialMember.System_Collections_Generic_IEnumerable_T__GetEnumerator, bag);
            }

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
        }

        private Symbol EnsureSpecialMember(SpecialMember member, BindingDiagnosticBag bag)
        {
            Symbol symbol;
            Binder.TryGetSpecialTypeMember(F.Compilation, member, body.Syntax, bag, out symbol);
            return symbol;
        }

        private void EnsureSpecialType(SpecialType type, BindingDiagnosticBag bag)
        {
            Binder.GetSpecialType(F.Compilation, type, body.Syntax, bag);
        }

        /// <summary>
        /// Check that the property and its getter exist and collect any use-site errors.
        /// </summary>
        private void EnsureSpecialPropertyGetter(SpecialMember member, BindingDiagnosticBag bag)
        {
            PropertySymbol symbol = (PropertySymbol)EnsureSpecialMember(member, bag);
            if ((object)symbol != null)
            {
                var getter = symbol.GetMethod;
                if ((object)getter == null)
                {
                    Binder.Error(bag, ErrorCode.ERR_PropertyLacksGet, body.Syntax, symbol);
                    return;
                }

                bag.ReportUseSite(getter, body.Syntax.Location);
            }
        }

        protected override bool PreserveInitialParameterValuesAndThreadId
            => _isEnumerable;

        protected override void GenerateControlFields()
        {
            Debug.Assert(F.ModuleBuilderOpt is not null);

            stateField = F.StateMachineField(F.SpecialType(SpecialType.System_Int32), GeneratedNames.MakeStateMachineStateFieldName());

            var instrumentations = F.ModuleBuilderOpt.GetMethodBodyInstrumentations(method);
            if (instrumentations.Kinds.Contains(InstrumentationKindExtensions.LocalStateTracing))
            {
                instanceIdField = F.StateMachineField(F.SpecialType(SpecialType.System_UInt64), GeneratedNames.MakeStateMachineStateIdFieldName());
            }

            // Add a field: T current
            _currentField = F.StateMachineField(_elementType, GeneratedNames.MakeIteratorCurrentFieldName());
        }

        protected override void GenerateMethodImplementations()
        {
            try
            {
                BoundExpression managedThreadId = null; // Thread.CurrentThread.ManagedThreadId

                GenerateEnumeratorImplementation();

                if (_isEnumerable)
                {
                    GenerateEnumerableImplementation(ref managedThreadId);
                }

                GenerateConstructor(managedThreadId);
            }
            catch (SyntheticBoundNodeFactory.MissingPredefinedMember ex)
            {
                diagnostics.Add(ex.Diagnostic);
            }
        }

        private void GenerateEnumeratorImplementation()
        {
            var IDisposable_Dispose = F.SpecialMethod(SpecialMember.System_IDisposable__Dispose);

            var IEnumerator_MoveNext = F.SpecialMethod(SpecialMember.System_Collections_IEnumerator__MoveNext);
            var IEnumerator_Reset = F.SpecialMethod(SpecialMember.System_Collections_IEnumerator__Reset);
            var IEnumerator_get_Current = F.SpecialProperty(SpecialMember.System_Collections_IEnumerator__Current).GetMethod;

            var IEnumeratorOfElementType = F.SpecialType(SpecialType.System_Collections_Generic_IEnumerator_T).Construct(ImmutableArray.Create(_elementType));
            var IEnumeratorOfElementType_get_Current = F.SpecialProperty(SpecialMember.System_Collections_Generic_IEnumerator_T__Current).GetMethod.AsMember(IEnumeratorOfElementType);

            // Add bool IEnumerator.MoveNext() and void IDisposable.Dispose()
            {
                var disposeMethod = OpenMethodImplementation(
                    IDisposable_Dispose,
                    hasMethodBodyDependency: true);

                var moveNextMethod = OpenMoveNextMethodImplementation(IEnumerator_MoveNext);

                GenerateMoveNextAndDispose(moveNextMethod, disposeMethod);
            }

            // Add T IEnumerator<T>.Current
            {
                OpenPropertyImplementation(IEnumeratorOfElementType_get_Current);
                F.CloseMethod(F.Return(F.Field(F.This(), _currentField)));
            }

            // Add void IEnumerator.Reset()
            {
                OpenMethodImplementation(IEnumerator_Reset, hasMethodBodyDependency: false);
                F.CloseMethod(F.Throw(F.New(F.WellKnownType(WellKnownType.System_NotSupportedException))));
            }

            // Add object IEnumerator.Current
            {
                OpenPropertyImplementation(IEnumerator_get_Current);
                F.CloseMethod(F.Return(F.Field(F.This(), _currentField)));
            }
        }

        /// <summary>
        /// Add IEnumerator&lt;elementType> IEnumerable&lt;elementType>.GetEnumerator()
        /// </summary>
        private void GenerateEnumerableImplementation(ref BoundExpression managedThreadId)
        {
            var IEnumerable_GetEnumerator = F.SpecialMethod(SpecialMember.System_Collections_IEnumerable__GetEnumerator);

            var IEnumerableOfElementType = F.SpecialType(SpecialType.System_Collections_Generic_IEnumerable_T).Construct(_elementType.Type);
            var IEnumerableOfElementType_GetEnumerator = F.SpecialMethod(SpecialMember.System_Collections_Generic_IEnumerable_T__GetEnumerator).AsMember(IEnumerableOfElementType);

            // generate GetEnumerator()
            var getEnumeratorGeneric = GenerateIteratorGetEnumerator(IEnumerableOfElementType_GetEnumerator, ref managedThreadId, StateMachineState.InitialIteratorState);

            // Generate IEnumerable.GetEnumerator
            var getEnumerator = OpenMethodImplementation(IEnumerable_GetEnumerator);
            F.CloseMethod(F.Return(F.Call(F.This(), getEnumeratorGeneric)));
        }

        private void GenerateConstructor(BoundExpression managedThreadId)
        {
            // Produces:
            // .ctor(int state)
            // {
            //     this.state = state;
            //     this.initialThreadId = <managedThreadId>;
            // }
            Debug.Assert(stateMachineType.Constructor is IteratorConstructor);

            F.CurrentFunction = stateMachineType.Constructor;
            var bodyBuilder = ArrayBuilder<BoundStatement>.GetInstance();
            bodyBuilder.Add(F.BaseInitialization());
            bodyBuilder.Add(F.Assignment(F.Field(F.This(), stateField), F.Parameter(F.CurrentFunction.Parameters[0]))); // this.state = state;

            if (managedThreadId != null)
            {
                // this.initialThreadId = Thread.CurrentThread.ManagedThreadId;
                bodyBuilder.Add(F.Assignment(F.Field(F.This(), initialThreadIdField), managedThreadId));
            }

            if (instanceIdField is not null &&
                F.WellKnownMethod(WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__GetNewStateMachineInstanceId) is { } getId)
            {
                bodyBuilder.Add(F.Assignment(F.InstanceField(instanceIdField), F.Call(receiver: null, getId)));
            }

            bodyBuilder.Add(F.Return());
            F.CloseMethod(F.Block(bodyBuilder.ToImmutableAndFree()));
            bodyBuilder = null;
        }

        protected override void InitializeStateMachine(ArrayBuilder<BoundStatement> bodyBuilder, NamedTypeSymbol frameType, LocalSymbol stateMachineLocal)
        {
            // var stateMachineLocal = new IteratorImplementationClass(N)
            // where N is either 0 (if we're producing an enumerator) or -2 (if we're producing an enumerable)
            var initialState = _isEnumerable ? StateMachineState.FinishedState : StateMachineState.InitialIteratorState;
            bodyBuilder.Add(
                F.Assignment(
                    F.Local(stateMachineLocal),
                    F.New(stateMachineType.Constructor.AsMember(frameType), F.Literal(initialState))));
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

        private void GenerateMoveNextAndDispose(
            SynthesizedImplementationMethod moveNextMethod,
            SynthesizedImplementationMethod disposeMethod)
        {
            var rewriter = new IteratorMethodToStateMachineRewriter(
                F,
                method,
                stateField,
                _currentField,
                instanceIdField,
                hoistedVariables,
                nonReusableLocalProxies,
                nonReusableFieldsForCleanup,
                synthesizedLocalOrdinals,
                stateMachineStateDebugInfoBuilder,
                slotAllocatorOpt,
                nextFreeHoistedLocalSlot,
                diagnostics);

            rewriter.GenerateMoveNextAndDispose(body, moveNextMethod, disposeMethod);
        }
    }
}
