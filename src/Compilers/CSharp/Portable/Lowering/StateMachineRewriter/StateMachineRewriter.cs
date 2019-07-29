// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract class StateMachineRewriter
    {
        protected readonly BoundStatement body;
        protected readonly MethodSymbol method;
        protected readonly DiagnosticBag diagnostics;
        protected readonly SyntheticBoundNodeFactory F;
        protected readonly SynthesizedContainer stateMachineType;
        protected readonly VariableSlotAllocator slotAllocatorOpt;
        protected readonly SynthesizedLocalOrdinalsDispenser synthesizedLocalOrdinals;

        protected FieldSymbol stateField;
        protected IReadOnlyDictionary<Symbol, CapturedSymbolReplacement> nonReusableLocalProxies;
        protected int nextFreeHoistedLocalSlot;
        protected IOrderedReadOnlySet<Symbol> hoistedVariables;
        protected Dictionary<Symbol, CapturedSymbolReplacement> initialParameters;
        protected FieldSymbol initialThreadIdField;

        protected StateMachineRewriter(
            BoundStatement body,
            MethodSymbol method,
            SynthesizedContainer stateMachineType,
            VariableSlotAllocator slotAllocatorOpt,
            TypeCompilationState compilationState,
            DiagnosticBag diagnostics)
        {
            Debug.Assert(body != null);
            Debug.Assert(method != null);
            Debug.Assert((object)stateMachineType != null);
            Debug.Assert(compilationState != null);
            Debug.Assert(diagnostics != null);

            this.body = body;
            this.method = method;
            this.stateMachineType = stateMachineType;
            this.slotAllocatorOpt = slotAllocatorOpt;
            this.synthesizedLocalOrdinals = new SynthesizedLocalOrdinalsDispenser();
            this.diagnostics = diagnostics;

            this.F = new SyntheticBoundNodeFactory(method, body.Syntax, compilationState, diagnostics);
            Debug.Assert(TypeSymbol.Equals(F.CurrentType, method.ContainingType, TypeCompareKind.ConsiderEverything2));
            Debug.Assert(F.Syntax == body.Syntax);
        }

        /// <summary>
        /// True if the initial values of locals in the rewritten method and the initial thread ID need to be preserved. (e.g. enumerable iterator methods and async-enumerable iterator methods)
        /// </summary>
        protected abstract bool PreserveInitialParameterValuesAndThreadId { get; }

        /// <summary>
        /// Add fields to the state machine class that control the state machine.
        /// </summary>
        protected abstract void GenerateControlFields();

        /// <summary>
        /// Initialize the state machine class.
        /// </summary>
        protected abstract void InitializeStateMachine(ArrayBuilder<BoundStatement> bodyBuilder, NamedTypeSymbol frameType, LocalSymbol stateMachineLocal);

        /// <summary>
        /// Generate implementation-specific state machine initialization for the kickoff method body.
        /// </summary>
        protected abstract BoundStatement GenerateStateMachineCreation(LocalSymbol stateMachineVariable, NamedTypeSymbol frameType);

        /// <summary>
        /// Generate implementation-specific state machine member method implementations.
        /// </summary>
        protected abstract void GenerateMethodImplementations();

        protected BoundStatement Rewrite()
        {
            if (this.body.HasErrors)
            {
                return this.body;
            }

            F.OpenNestedType(stateMachineType);

            GenerateControlFields();

            if (PreserveInitialParameterValuesAndThreadId && CanGetThreadId())
            {
                // if it is an enumerable or async-enumerable, and either Environment.CurrentManagedThreadId or Thread.ManagedThreadId are available
                // add a field: int initialThreadId
                initialThreadIdField = F.StateMachineField(F.SpecialType(SpecialType.System_Int32), GeneratedNames.MakeIteratorCurrentThreadIdFieldName());
            }

            // fields for the initial values of all the parameters of the method
            if (PreserveInitialParameterValuesAndThreadId)
            {
                initialParameters = new Dictionary<Symbol, CapturedSymbolReplacement>();
            }

            // fields for the captured variables of the method
            var variablesToHoist = IteratorAndAsyncCaptureWalker.Analyze(F.Compilation, method, body, diagnostics);

            CreateNonReusableLocalProxies(variablesToHoist, out this.nonReusableLocalProxies, out this.nextFreeHoistedLocalSlot);

            this.hoistedVariables = variablesToHoist;

            GenerateMethodImplementations();

            // Return a replacement body for the kickoff method
            return GenerateKickoffMethodBody();
        }

        private void CreateNonReusableLocalProxies(
            IEnumerable<Symbol> variablesToHoist,
            out IReadOnlyDictionary<Symbol, CapturedSymbolReplacement> proxies,
            out int nextFreeHoistedLocalSlot)
        {
            var proxiesBuilder = new Dictionary<Symbol, CapturedSymbolReplacement>();

            var typeMap = stateMachineType.TypeMap;
            bool isDebugBuild = F.Compilation.Options.OptimizationLevel == OptimizationLevel.Debug;
            bool mapToPreviousFields = isDebugBuild && slotAllocatorOpt != null;

            nextFreeHoistedLocalSlot = mapToPreviousFields ? slotAllocatorOpt.PreviousHoistedLocalSlotCount : 0;

            foreach (var variable in variablesToHoist)
            {
                Debug.Assert(variable.Kind == SymbolKind.Local || variable.Kind == SymbolKind.Parameter);

                if (variable.Kind == SymbolKind.Local)
                {
                    var local = (LocalSymbol)variable;
                    var synthesizedKind = local.SynthesizedKind;

                    if (!synthesizedKind.MustSurviveStateMachineSuspension())
                    {
                        continue;
                    }

                    // no need to hoist constants
                    if (local.IsConst)
                    {
                        continue;
                    }

                    if (local.RefKind != RefKind.None)
                    {
                        // we'll create proxies for these variables later:
                        Debug.Assert(synthesizedKind == SynthesizedLocalKind.Spill);
                        continue;
                    }

                    Debug.Assert(local.RefKind == RefKind.None);
                    StateMachineFieldSymbol field = null;

                    if (ShouldPreallocateNonReusableProxy(local))
                    {
                        // variable needs to be hoisted
                        var fieldType = typeMap.SubstituteType(local.Type).Type;

                        LocalDebugId id;
                        int slotIndex = -1;

                        if (isDebugBuild)
                        {
                            // Calculate local debug id.
                            //
                            // EnC: When emitting the baseline (gen 0) the id is stored in a custom debug information attached to the kickoff method.
                            //      When emitting a delta the id is only used to map to the existing field in the previous generation.
                            SyntaxNode declaratorSyntax = local.GetDeclaratorSyntax();
                            int syntaxOffset = method.CalculateLocalSyntaxOffset(LambdaUtilities.GetDeclaratorPosition(declaratorSyntax), declaratorSyntax.SyntaxTree);
                            int ordinal = synthesizedLocalOrdinals.AssignLocalOrdinal(synthesizedKind, syntaxOffset);
                            id = new LocalDebugId(syntaxOffset, ordinal);

                            // map local id to the previous id, if available:
                            int previousSlotIndex;
                            if (mapToPreviousFields && slotAllocatorOpt.TryGetPreviousHoistedLocalSlotIndex(
                                declaratorSyntax,
                                F.ModuleBuilderOpt.Translate(fieldType, declaratorSyntax, diagnostics),
                                synthesizedKind,
                                id,
                                diagnostics,
                                out previousSlotIndex))
                            {
                                slotIndex = previousSlotIndex;
                            }
                        }
                        else
                        {
                            id = LocalDebugId.None;
                        }

                        if (slotIndex == -1)
                        {
                            slotIndex = nextFreeHoistedLocalSlot++;
                        }

                        string fieldName = GeneratedNames.MakeHoistedLocalFieldName(synthesizedKind, slotIndex, local.Name);
                        field = F.StateMachineField(fieldType, fieldName, new LocalSlotDebugInfo(synthesizedKind, id), slotIndex);
                    }

                    if (field != null)
                    {
                        proxiesBuilder.Add(local, new CapturedToStateMachineFieldReplacement(field, isReusable: false));
                    }
                }
                else
                {
                    var parameter = (ParameterSymbol)variable;
                    if (parameter.IsThis)
                    {
                        var containingType = method.ContainingType;
                        var proxyField = F.StateMachineField(containingType, GeneratedNames.ThisProxyFieldName(), isPublic: true, isThis: true);
                        proxiesBuilder.Add(parameter, new CapturedToStateMachineFieldReplacement(proxyField, isReusable: false));

                        if (PreserveInitialParameterValuesAndThreadId)
                        {
                            var initialThis = containingType.IsStructType() ?
                                F.StateMachineField(containingType, GeneratedNames.StateMachineThisParameterProxyName(), isPublic: true, isThis: true) : proxyField;

                            initialParameters.Add(parameter, new CapturedToStateMachineFieldReplacement(initialThis, isReusable: false));
                        }
                    }
                    else
                    {
                        // The field needs to be public iff it is initialized directly from the kickoff method
                        // (i.e. not for IEnumerable which loads the values from parameter proxies).
                        var proxyField = F.StateMachineField(typeMap.SubstituteType(parameter.Type).Type, parameter.Name, isPublic: !PreserveInitialParameterValuesAndThreadId);
                        proxiesBuilder.Add(parameter, new CapturedToStateMachineFieldReplacement(proxyField, isReusable: false));

                        if (PreserveInitialParameterValuesAndThreadId)
                        {
                            var field = F.StateMachineField(typeMap.SubstituteType(parameter.Type).Type, GeneratedNames.StateMachineParameterProxyFieldName(parameter.Name), isPublic: true);
                            initialParameters.Add(parameter, new CapturedToStateMachineFieldReplacement(field, isReusable: false));
                        }
                    }
                }
            }

            proxies = proxiesBuilder;
        }

        private bool ShouldPreallocateNonReusableProxy(LocalSymbol local)
        {
            var synthesizedKind = local.SynthesizedKind;
            var optimizationLevel = F.Compilation.Options.OptimizationLevel;

            // do not preallocate proxy fields for user defined locals in release
            // otherwise we will be allocating fields for all locals even when fields can be reused
            // see https://github.com/dotnet/roslyn/issues/15290
            if (optimizationLevel == OptimizationLevel.Release && synthesizedKind == SynthesizedLocalKind.UserDefined)
            {
                return false;
            }

            return !synthesizedKind.IsSlotReusable(optimizationLevel);
        }

        private BoundStatement GenerateKickoffMethodBody()
        {
            F.CurrentFunction = method;
            var bodyBuilder = ArrayBuilder<BoundStatement>.GetInstance();

            var frameType = method.IsGenericMethod ? stateMachineType.Construct(method.TypeArgumentsWithAnnotations, unbound: false) : stateMachineType;
            LocalSymbol stateMachineVariable = F.SynthesizedLocal(frameType, null);
            InitializeStateMachine(bodyBuilder, frameType, stateMachineVariable);

            // plus code to initialize all of the parameter proxies result.proxy
            var proxies = PreserveInitialParameterValuesAndThreadId ? initialParameters : nonReusableLocalProxies;

            // starting with the "this" proxy
            if (!method.IsStatic)
            {
                Debug.Assert((object)method.ThisParameter != null);

                CapturedSymbolReplacement proxy;
                if (proxies.TryGetValue(method.ThisParameter, out proxy))
                {
                    bodyBuilder.Add(F.Assignment(proxy.Replacement(F.Syntax, frameType1 => F.Local(stateMachineVariable)), F.This()));
                }
            }

            foreach (var parameter in method.Parameters)
            {
                CapturedSymbolReplacement proxy;
                if (proxies.TryGetValue(parameter, out proxy))
                {
                    bodyBuilder.Add(F.Assignment(proxy.Replacement(F.Syntax, frameType1 => F.Local(stateMachineVariable)),
                                                 F.Parameter(parameter)));
                }
            }

            bodyBuilder.Add(GenerateStateMachineCreation(stateMachineVariable, frameType));
            return F.Block(
                ImmutableArray.Create(stateMachineVariable),
                bodyBuilder.ToImmutableAndFree());
        }

        protected SynthesizedImplementationMethod OpenMethodImplementation(
            MethodSymbol methodToImplement,
            string methodName = null,
            bool hasMethodBodyDependency = false)
        {
            var result = new SynthesizedStateMachineDebuggerHiddenMethod(methodName, methodToImplement, (StateMachineTypeSymbol)F.CurrentType, null, hasMethodBodyDependency);
            F.ModuleBuilderOpt.AddSynthesizedDefinition(F.CurrentType, result);
            F.CurrentFunction = result;
            return result;
        }

        protected MethodSymbol OpenPropertyImplementation(MethodSymbol getterToImplement)
        {
            var prop = new SynthesizedStateMachineProperty(getterToImplement, (StateMachineTypeSymbol)F.CurrentType);
            F.ModuleBuilderOpt.AddSynthesizedDefinition(F.CurrentType, prop);

            var getter = prop.GetMethod;
            F.ModuleBuilderOpt.AddSynthesizedDefinition(F.CurrentType, getter);

            F.CurrentFunction = getter;
            return getter;
        }

        protected SynthesizedImplementationMethod OpenMoveNextMethodImplementation(MethodSymbol methodToImplement)
        {
            var result = new SynthesizedStateMachineMoveNextMethod(methodToImplement, (StateMachineTypeSymbol)F.CurrentType);
            F.ModuleBuilderOpt.AddSynthesizedDefinition(F.CurrentType, result);
            F.CurrentFunction = result;
            return result;
        }

        /// <summary>
        /// Produce Environment.CurrentManagedThreadId if available, otherwise CurrentThread.ManagedThreadId
        /// </summary>
        protected BoundExpression MakeCurrentThreadId()
        {
            Debug.Assert(CanGetThreadId());

            // .NET Core has removed the Thread class. We can get the managed thread id by making a call to
            // Environment.CurrentManagedThreadId. If that method is not present (pre 4.5) fall back to the old behavior.

            var currentManagedThreadIdProperty = (PropertySymbol)F.WellKnownMember(WellKnownMember.System_Environment__CurrentManagedThreadId, isOptional: true);
            if ((object)currentManagedThreadIdProperty != null)
            {
                MethodSymbol currentManagedThreadIdMethod = currentManagedThreadIdProperty.GetMethod;
                if ((object)currentManagedThreadIdMethod != null)
                {
                    return F.Call(null, currentManagedThreadIdMethod);
                }
            }

            return F.Property(F.Property(WellKnownMember.System_Threading_Thread__CurrentThread), WellKnownMember.System_Threading_Thread__ManagedThreadId);
        }

        /// <summary>
        /// Generate the GetEnumerator() method for iterators and GetAsyncEnumerator() for async-iterators.
        /// </summary>
        protected SynthesizedImplementationMethod GenerateIteratorGetEnumerator(MethodSymbol getEnumeratorMethod, ref BoundExpression managedThreadId, int initialState)
        {
            // Produces:
            //    {StateMachineType} result;
            //    if (this.initialThreadId == {managedThreadId} && this.state == -2)
            //    {
            //        this.state = {initialState};
            //        extraReset
            //        result = this;
            //    }
            //    else
            //    {
            //        result = new {StateMachineType}({initialState});
            //    }
            //
            //    result.parameter = this.parameterProxy; // OR more complex initialization for async-iterator parameter marked with [EnumeratorCancellation]

            // The implementation doesn't depend on the method body of the iterator method.
            // Only on its parameters and staticness.
            var getEnumerator = OpenMethodImplementation(
                getEnumeratorMethod,
                hasMethodBodyDependency: false);

            var bodyBuilder = ArrayBuilder<BoundStatement>.GetInstance();
            // {StateMachineType} result;
            var resultVariable = F.SynthesizedLocal(stateMachineType, null);
            // result = new {StateMachineType}({initialState})
            BoundStatement makeIterator = F.Assignment(F.Local(resultVariable), F.New(stateMachineType.Constructor, F.Literal(initialState)));

            var thisInitialized = F.GenerateLabel("thisInitialized");

            if ((object)initialThreadIdField != null)
            {
                managedThreadId = MakeCurrentThreadId();

                var thenBuilder = ArrayBuilder<BoundStatement>.GetInstance(4);
                thenBuilder.Add(
                    // this.state = {initialState};
                    F.Assignment(F.Field(F.This(), stateField), F.Literal(initialState)));

                thenBuilder.Add(
                    // result = this;
                    F.Assignment(F.Local(resultVariable), F.This()));

                var extraReset = GetExtraResetForIteratorGetEnumerator();
                if (extraReset != null)
                {
                    thenBuilder.Add(extraReset);
                }

                if (method.IsStatic || method.ThisParameter.Type.IsReferenceType)
                {
                    // if this is a reference type, no need to copy it since it is not assignable
                    thenBuilder.Add(
                        // goto thisInitialized;
                        F.Goto(thisInitialized));
                }

                makeIterator = F.If(
                    // if (this.state == -2 && this.initialThreadId == Thread.CurrentThread.ManagedThreadId)
                    condition: F.LogicalAnd(
                        F.IntEqual(F.Field(F.This(), stateField), F.Literal(StateMachineStates.FinishedStateMachine)),
                        F.IntEqual(F.Field(F.This(), initialThreadIdField), managedThreadId)),
                    thenClause: F.Block(thenBuilder.ToImmutableAndFree()),
                    elseClauseOpt: makeIterator);
            }

            bodyBuilder.Add(makeIterator);

            // Initialize all the parameter copies
            var copySrc = initialParameters;
            var copyDest = nonReusableLocalProxies;
            if (!method.IsStatic)
            {
                // starting with "this"
                CapturedSymbolReplacement proxy;
                if (copyDest.TryGetValue(method.ThisParameter, out proxy))
                {
                    bodyBuilder.Add(
                        F.Assignment(
                            proxy.Replacement(F.Syntax, stateMachineType => F.Local(resultVariable)),
                            copySrc[method.ThisParameter].Replacement(F.Syntax, stateMachineType => F.This())));
                }
            }

            bodyBuilder.Add(F.Label(thisInitialized));

            foreach (var parameter in method.Parameters)
            {
                CapturedSymbolReplacement proxy;
                if (copyDest.TryGetValue(parameter, out proxy))
                {
                    // result.parameter
                    BoundExpression resultParameter = proxy.Replacement(F.Syntax, stateMachineType => F.Local(resultVariable));
                    // this.parameterProxy
                    BoundExpression parameterProxy = copySrc[parameter].Replacement(F.Syntax, stateMachineType => F.This());
                    BoundStatement copy = InitializeParameterField(getEnumeratorMethod, parameter, resultParameter, parameterProxy);

                    bodyBuilder.Add(copy);
                }
            }

            bodyBuilder.Add(F.Return(F.Local(resultVariable)));
            F.CloseMethod(F.Block(ImmutableArray.Create(resultVariable), bodyBuilder.ToImmutableAndFree()));
            return getEnumerator;
        }

        protected virtual BoundStatement InitializeParameterField(MethodSymbol getEnumeratorMethod, ParameterSymbol parameter, BoundExpression resultParameter, BoundExpression parameterProxy)
        {
            Debug.Assert(!method.IsIterator || !method.IsAsync); // an override handles async-iterators

            // result.parameter = this.parameterProxy;
            return F.Assignment(resultParameter, parameterProxy);
        }

        /// <summary>
        /// Async-iterator methods use a GetAsyncEnumerator method just like the GetEnumerator of iterator methods.
        /// But they need to do a bit more work (to reset the dispose mode).
        /// </summary>
        protected virtual BoundStatement GetExtraResetForIteratorGetEnumerator() => null;

        /// <summary>
        /// Returns true if either Thread.ManagedThreadId or Environment.CurrentManagedThreadId are available
        /// </summary>
        protected bool CanGetThreadId()
        {
            return (object)F.WellKnownMember(WellKnownMember.System_Threading_Thread__ManagedThreadId, isOptional: true) != null ||
                (object)F.WellKnownMember(WellKnownMember.System_Environment__CurrentManagedThreadId, isOptional: true) != null;
        }
    }
}
