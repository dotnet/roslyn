// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
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
        protected IReadOnlySet<Symbol> hoistedVariables;
        protected Dictionary<Symbol, CapturedSymbolReplacement> initialParameters;

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
            Debug.Assert(stateMachineType != null);
            Debug.Assert(compilationState != null);
            Debug.Assert(diagnostics != null);

            this.body = body;
            this.method = method;
            this.stateMachineType = stateMachineType;
            this.slotAllocatorOpt = slotAllocatorOpt;
            this.synthesizedLocalOrdinals = new SynthesizedLocalOrdinalsDispenser();
            this.diagnostics = diagnostics;

            this.F = new SyntheticBoundNodeFactory(method, body.Syntax, compilationState, diagnostics);
            Debug.Assert(F.CurrentType == method.ContainingType);
            Debug.Assert(F.Syntax == body.Syntax);
        }

        /// <summary>
        /// True if the initial values of locals in the rewritten method need to be preserved. (e.g. enumerable iterator methods)
        /// </summary>
        protected abstract bool PreserveInitialParameterValues { get; }

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

            // fields for the initial values of all the parameters of the method
            if (PreserveInitialParameterValues)
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
                        Debug.Assert(synthesizedKind == SynthesizedLocalKind.AwaitSpill);
                        continue;
                    }

                    Debug.Assert(local.RefKind == RefKind.None);
                    StateMachineFieldSymbol field = null;

                    if (!local.SynthesizedKind.IsSlotReusable(F.Compilation.Options.OptimizationLevel))
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
                            int syntaxOffset = this.method.CalculateLocalSyntaxOffset(declaratorSyntax.SpanStart, declaratorSyntax.SyntaxTree);
                            int ordinal = synthesizedLocalOrdinals.AssignLocalOrdinal(synthesizedKind, syntaxOffset);
                            id = new LocalDebugId(syntaxOffset, ordinal);

                            // map local id to the previous id, if available:
                            int previousSlotIndex;
                            if (mapToPreviousFields && slotAllocatorOpt.TryGetPreviousHoistedLocalSlotIndex(declaratorSyntax, F.ModuleBuilderOpt.Translate(fieldType, declaratorSyntax, diagnostics), synthesizedKind, id, out previousSlotIndex))
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
                        var proxyField = F.StateMachineField(containingType, GeneratedNames.ThisProxyFieldName(), isPublic: true);
                        proxiesBuilder.Add(parameter, new CapturedToStateMachineFieldReplacement(proxyField, isReusable: false));

                        if (PreserveInitialParameterValues)
                        {
                            var initialThis = containingType.IsStructType() ?
                                F.StateMachineField(containingType, GeneratedNames.StateMachineThisParameterProxyName(), isPublic: true) : proxyField;

                            initialParameters.Add(parameter, new CapturedToStateMachineFieldReplacement(initialThis, isReusable: false));
                        }
                    }
                    else
                    {
                        // The field needs to be public iff it is initialized directly from the kickoff method 
                        // (i.e. not for IEnumerable which loads the values from parameter proxies).
                        var proxyField = F.StateMachineField(typeMap.SubstituteType(parameter.Type).Type, parameter.Name, isPublic: !PreserveInitialParameterValues);
                        proxiesBuilder.Add(parameter, new CapturedToStateMachineFieldReplacement(proxyField, isReusable: false));

                        if (PreserveInitialParameterValues)
                        {
                            var field = F.StateMachineField(typeMap.SubstituteType(parameter.Type).Type, GeneratedNames.StateMachineParameterProxyFieldName(parameter.Name), isPublic: true);
                            initialParameters.Add(parameter, new CapturedToStateMachineFieldReplacement(field, isReusable: false));
                        }
                    }
                }
            }

            proxies = proxiesBuilder;
        }

        private BoundStatement GenerateKickoffMethodBody()
        {
            F.CurrentMethod = method;
            var bodyBuilder = ArrayBuilder<BoundStatement>.GetInstance();

            var frameType = method.IsGenericMethod ? stateMachineType.Construct(method.TypeArguments) : stateMachineType;
            LocalSymbol stateMachineVariable = F.SynthesizedLocal(frameType, null);
            InitializeStateMachine(bodyBuilder, frameType, stateMachineVariable);

            // plus code to initialize all of the parameter proxies result.proxy
            var proxies = PreserveInitialParameterValues ? initialParameters : nonReusableLocalProxies;

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
                ImmutableArray<LocalFunctionSymbol>.Empty,
                bodyBuilder.ToImmutableAndFree());
        }

        protected SynthesizedImplementationMethod OpenMethodImplementation(
            MethodSymbol methodToImplement,
            string methodName = null,
            bool hasMethodBodyDependency = false)
        {
            var result = new SynthesizedStateMachineDebuggerHiddenMethod(methodName, methodToImplement, (StateMachineTypeSymbol)F.CurrentType, null, hasMethodBodyDependency);
            F.ModuleBuilderOpt.AddSynthesizedDefinition(F.CurrentType, result);
            F.CurrentMethod = result;
            return result;
        }

        protected MethodSymbol OpenPropertyImplementation(MethodSymbol getterToImplement)
        {
            var prop = new SynthesizedStateMachineProperty(getterToImplement, (StateMachineTypeSymbol)F.CurrentType);
            F.ModuleBuilderOpt.AddSynthesizedDefinition(F.CurrentType, prop);

            var getter = prop.GetMethod;
            F.ModuleBuilderOpt.AddSynthesizedDefinition(F.CurrentType, getter);

            F.CurrentMethod = getter;
            return getter;
        }

        protected SynthesizedImplementationMethod OpenMoveNextMethodImplementation(MethodSymbol methodToImplement)
        {
            var result = new SynthesizedStateMachineMoveNextMethod(methodToImplement, (StateMachineTypeSymbol)F.CurrentType);
            F.ModuleBuilderOpt.AddSynthesizedDefinition(F.CurrentType, result);
            F.CurrentMethod = result;
            return result;
        }
    }
}
