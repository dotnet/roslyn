// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class AsyncRewriter : StateMachineRewriter
    {
        private readonly AsyncMethodBuilderMemberCollection _asyncMethodBuilderMemberCollection;
        private readonly bool _constructedSuccessfully;
        private readonly int _methodOrdinal;

        private FieldSymbol? _builderField;

        private AsyncRewriter(
            BoundStatement body,
            MethodSymbol method,
            int methodOrdinal,
            AsyncStateMachine stateMachineType,
            ArrayBuilder<StateMachineStateDebugInfo> stateMachineStateDebugInfoBuilder,
            VariableSlotAllocator? slotAllocatorOpt,
            TypeCompilationState compilationState,
            BindingDiagnosticBag diagnostics)
            : base(body, method, stateMachineType, stateMachineStateDebugInfoBuilder, slotAllocatorOpt, compilationState, diagnostics)
        {
            _constructedSuccessfully = AsyncMethodBuilderMemberCollection.TryCreate(F, method, this.stateMachineType.TypeMap, out _asyncMethodBuilderMemberCollection);
            _methodOrdinal = methodOrdinal;
        }

        /// <summary>
        /// Rewrite an async method into a state machine type.
        /// </summary>
        internal static BoundStatement Rewrite(
            BoundStatement bodyWithAwaitLifted,
            MethodSymbol method,
            int methodOrdinal,
            ArrayBuilder<StateMachineStateDebugInfo> stateMachineStateDebugInfoBuilder,
            VariableSlotAllocator? slotAllocatorOpt,
            TypeCompilationState compilationState,
            BindingDiagnosticBag diagnostics,
            out AsyncStateMachine? stateMachineType)
        {
            Debug.Assert(compilationState.ModuleBuilderOpt != null);

            if (!method.IsAsync)
            {
                stateMachineType = null;
                return bodyWithAwaitLifted;
            }

            CSharpCompilation compilation = method.DeclaringCompilation;
            bool isAsyncEnumerableOrEnumerator = method.IsAsyncReturningIAsyncEnumerable(compilation) ||
                method.IsAsyncReturningIAsyncEnumerator(compilation);
            if (isAsyncEnumerableOrEnumerator && !method.IsIterator)
            {
                bool containsAwait = AwaitDetector.ContainsAwait(bodyWithAwaitLifted);
                diagnostics.Add(containsAwait ? ErrorCode.ERR_PossibleAsyncIteratorWithoutYield : ErrorCode.ERR_PossibleAsyncIteratorWithoutYieldOrAwait,
                    method.GetFirstLocation());

                stateMachineType = null;
                return bodyWithAwaitLifted;
            }

            // The CLR doesn't support adding fields to structs, so in order to enable EnC in an async method we need to generate a class.
            // For async-iterators, we also need to generate a class.
            var typeKind = (compilationState.Compilation.Options.EnableEditAndContinue || method.IsIterator) ? TypeKind.Class : TypeKind.Struct;

            stateMachineType = new AsyncStateMachine(slotAllocatorOpt, compilationState, method, methodOrdinal, typeKind);
            compilationState.ModuleBuilderOpt.CompilationState.SetStateMachineType(method, stateMachineType);

            AsyncRewriter rewriter = isAsyncEnumerableOrEnumerator
                ? new AsyncIteratorRewriter(bodyWithAwaitLifted, method, methodOrdinal, stateMachineType, stateMachineStateDebugInfoBuilder, slotAllocatorOpt, compilationState, diagnostics)
                : new AsyncRewriter(bodyWithAwaitLifted, method, methodOrdinal, stateMachineType, stateMachineStateDebugInfoBuilder, slotAllocatorOpt, compilationState, diagnostics);

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
                return new BoundBadStatement(bodyWithAwaitLifted.Syntax, ImmutableArray.Create<BoundNode>(bodyWithAwaitLifted), hasErrors: true);
            }
        }

#nullable disable

        /// <returns>
        /// Returns true if all types and members we need are present and good
        /// </returns>
        protected bool VerifyPresenceOfRequiredAPIs()
        {
            var bag = BindingDiagnosticBag.GetInstance(withDiagnostics: true, diagnostics.AccumulatesDependencies);

            VerifyPresenceOfRequiredAPIs(bag);

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
            return !hasErrors && _constructedSuccessfully;
        }

        protected virtual void VerifyPresenceOfRequiredAPIs(BindingDiagnosticBag bag)
        {
            EnsureWellKnownMember(WellKnownMember.System_Runtime_CompilerServices_IAsyncStateMachine_MoveNext, bag);
            EnsureWellKnownMember(WellKnownMember.System_Runtime_CompilerServices_IAsyncStateMachine_SetStateMachine, bag);
        }

        private Symbol EnsureWellKnownMember(WellKnownMember member, BindingDiagnosticBag bag)
        {
            return Binder.GetWellKnownTypeMember(F.Compilation, member, bag, body.Syntax.Location);
        }

        protected override bool PreserveInitialParameterValuesAndThreadId
            => false;

        protected override void GenerateControlFields()
        {
            // the fields are initialized from async method, so they need to be public:
            stateField = F.StateMachineField(F.SpecialType(SpecialType.System_Int32), GeneratedNames.MakeStateMachineStateFieldName(), isPublic: true);
            _builderField = F.StateMachineField(_asyncMethodBuilderMemberCollection.BuilderType, GeneratedNames.AsyncBuilderFieldName(), isPublic: true);

            var instrumentations = F.ModuleBuilderOpt.GetMethodBodyInstrumentations(method);
            if (instrumentations.Kinds.Contains(InstrumentationKindExtensions.LocalStateTracing))
            {
                instanceIdField = F.StateMachineField(F.SpecialType(SpecialType.System_UInt64), GeneratedNames.MakeStateMachineStateIdFieldName(), isPublic: true);
            }
        }

        protected override void GenerateMethodImplementations()
        {
            var IAsyncStateMachine_MoveNext = F.WellKnownMethod(WellKnownMember.System_Runtime_CompilerServices_IAsyncStateMachine_MoveNext);
            var IAsyncStateMachine_SetStateMachine = F.WellKnownMethod(WellKnownMember.System_Runtime_CompilerServices_IAsyncStateMachine_SetStateMachine);

            // Add IAsyncStateMachine.MoveNext()

            var moveNextMethod = OpenMoveNextMethodImplementation(IAsyncStateMachine_MoveNext);

            GenerateMoveNext(moveNextMethod);

            // Add IAsyncStateMachine.SetStateMachine()

            OpenMethodImplementation(
                IAsyncStateMachine_SetStateMachine,
                "SetStateMachine",
                hasMethodBodyDependency: false);

            // SetStateMachine is used to initialize the underlying AsyncMethodBuilder's reference to the boxed copy of the state machine.
            // If the state machine is a class there is no copy made and thus the initialization is not necessary.
            // In fact it is an error to reinitialize the builder since it already is initialized.
            if (F.CurrentType.TypeKind == TypeKind.Class)
            {
                F.CloseMethod(F.Return());
            }
            else
            {
                F.CloseMethod(
                    // this.builderField.SetStateMachine(sm)
                    F.Block(
                        F.ExpressionStatement(
                            F.Call(
                                F.Field(F.This(), _builderField),
                                _asyncMethodBuilderMemberCollection.SetStateMachine,
                                new BoundExpression[] { F.Parameter(F.CurrentFunction.Parameters[0]) })),
                        F.Return()));
            }

            // Constructor
            GenerateConstructor();
        }

        protected virtual void GenerateConstructor()
        {
            if (stateMachineType.TypeKind == TypeKind.Class)
            {
                F.CurrentFunction = stateMachineType.Constructor;
                F.CloseMethod(F.Block(ImmutableArray.Create(F.BaseInitialization(), F.Return())));
            }
        }

        protected override void InitializeStateMachine(ArrayBuilder<BoundStatement> bodyBuilder, NamedTypeSymbol frameType, LocalSymbol stateMachineLocal)
        {
            if (frameType.TypeKind == TypeKind.Class)
            {
                // local = new {state machine type}();
                bodyBuilder.Add(
                    F.Assignment(
                        F.Local(stateMachineLocal),
                        F.New(frameType.InstanceConstructors[0])));
            }
        }

        protected override BoundStatement GenerateStateMachineCreation(LocalSymbol stateMachineVariable, NamedTypeSymbol frameType, IReadOnlyDictionary<Symbol, CapturedSymbolReplacement> proxies)
        {
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

            // local.$builder = System.Runtime.CompilerServices.AsyncTaskMethodBuilder<typeArgs>.Create();
            bodyBuilder.Add(
                F.Assignment(
                    F.Field(F.Local(stateMachineVariable), _builderField.AsMember(frameType)),
                    F.StaticCall(
                        null,
                        methodScopeAsyncMethodBuilderMemberCollection.CreateBuilder)));

            bodyBuilder.Add(GenerateParameterStorage(stateMachineVariable, proxies));

            // local.$stateField = NotStartedStateMachine
            bodyBuilder.Add(
                F.Assignment(
                    F.Field(F.Local(stateMachineVariable), stateField.AsMember(frameType)),
                    F.Literal(StateMachineState.NotStartedOrRunningState)));

            // local.$instanceIdField = LocalStoreTracker.GetNewStateMachineInstanceId()
            if (instanceIdField is not null &&
                F.WellKnownMethod(WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__GetNewStateMachineInstanceId) is { } getId)
            {
                bodyBuilder.Add(
                    F.Assignment(
                        F.Field(F.Local(stateMachineVariable), instanceIdField.AsMember(frameType)),
                        F.Call(receiver: null, getId)));
            }

            // local.$builder.Start(ref local) -- binding to the method AsyncTaskMethodBuilder<typeArgs>.Start()
            var startMethod = methodScopeAsyncMethodBuilderMemberCollection.Start.Construct(frameType);
            if (methodScopeAsyncMethodBuilderMemberCollection.CheckGenericMethodConstraints)
            {
                startMethod.CheckConstraints(new ConstraintsHelper.CheckConstraintsArgs(F.Compilation, F.Compilation.Conversions, includeNullability: false, F.Syntax.Location, diagnostics));
            }
            bodyBuilder.Add(
                F.ExpressionStatement(
                    F.Call(
                        F.Field(F.Local(stateMachineVariable), _builderField.AsMember(frameType)),
                        startMethod,
                        ImmutableArray.Create<BoundExpression>(F.Local(stateMachineVariable)))));

            bodyBuilder.Add(method.IsAsyncReturningVoid()
                ? F.Return()
                : F.Return(
                    F.Property(
                        F.Field(F.Local(stateMachineVariable), _builderField.AsMember(frameType)),
                        methodScopeAsyncMethodBuilderMemberCollection.Task)));

            return F.Block(bodyBuilder.ToImmutableAndFree());
        }

        protected virtual void GenerateMoveNext(SynthesizedImplementationMethod moveNextMethod)
        {
            var rewriter = new AsyncMethodToStateMachineRewriter(
                method: method,
                methodOrdinal: _methodOrdinal,
                asyncMethodBuilderMemberCollection: _asyncMethodBuilderMemberCollection,
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

        /// <summary>
        /// Note: do not use a static/singleton instance of this type, as it holds state.
        /// </summary>
        private class AwaitDetector : BoundTreeWalkerWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator
        {
            private bool _sawAwait;

            public static bool ContainsAwait(BoundNode node)
            {
                var detector = new AwaitDetector();
                detector.Visit(node);
                return detector._sawAwait;
            }

            public override BoundNode VisitAwaitExpression(BoundAwaitExpression node)
            {
                _sawAwait = true;
                return null;
            }
        }
    }
}
