// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Symbols;
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

        protected FieldSymbol stateField;
        protected IReadOnlyDictionary<Symbol, CapturedSymbolReplacement> nonReusableLocalProxies;
        protected IReadOnlySet<Symbol> variablesCaptured;
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
            this.diagnostics = diagnostics;

            this.F = new SyntheticBoundNodeFactory(method, body.Syntax, compilationState, diagnostics);
            Debug.Assert(F.CurrentClass == method.ContainingType);
            Debug.Assert(F.Syntax == body.Syntax);
        }

        /// <summary>
        /// True if the initial values of locals in the rewritten method need to be preserved. (e.g. enumerable iterator methods)
        /// </summary>
        protected abstract bool PreserveInitialParameterValues { get; }

        /// <summary>
        /// Add fields to the state machine class that control the state machine.
        /// </summary>
        protected virtual void GenerateControlFields()
        {
            // Add a field: int _state
            var intType = F.SpecialType(SpecialType.System_Int32);
            this.stateField = F.StateMachineField(intType, GeneratedNames.MakeStateMachineStateName(), IsStateFieldPublic);
        }

        /// <summary>
        /// Initialize the state machine class.
        /// </summary>
        protected abstract void InitializeStateMachine(ArrayBuilder<BoundStatement> bodyBuilder, NamedTypeSymbol frameType, LocalSymbol stateMachineLocal);

        /// <summary>
        /// Generate implementation-specific state machine initialization for the replacement method body.
        /// </summary>
        protected abstract BoundStatement GenerateReplacementBody(LocalSymbol stateMachineVariable, NamedTypeSymbol frameType);

        /// <summary>
        /// Generate implementation-specific state machine member method implementations.
        /// </summary>
        protected abstract void GenerateMethodImplementations();

        protected abstract bool IsStateFieldPublic { get; }

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
            var variablesCaptured = IteratorAndAsyncCaptureWalker.Analyze(F.CompilationState.ModuleBuilderOpt.Compilation, method, body);
            this.nonReusableLocalProxies = CreateNonReusableLocalProxies(variablesCaptured);
            this.variablesCaptured = variablesCaptured;

            GenerateMethodImplementations();

            // Return a replacement body for the original method
            return ReplaceOriginalMethod();
        }

        private IReadOnlyDictionary<Symbol, CapturedSymbolReplacement> CreateNonReusableLocalProxies(MultiDictionary<Symbol, CSharpSyntaxNode> variablesCaptured)
        {
            var proxies = new Dictionary<Symbol, CapturedSymbolReplacement>();

            var typeMap = stateMachineType.TypeMap;

            var orderedCaptured =
                from local in variablesCaptured.Keys
                orderby local.Name, (local.Locations.Length == 0) ? 0 : local.Locations[0].SourceSpan.Start
                select local;

            foreach (var capturedVariable in orderedCaptured)
            {
                if (capturedVariable.Kind == SymbolKind.Local)
                {
                    var local = (LocalSymbol)capturedVariable;
                    if (local.SynthesizedKind == SynthesizedLocalKind.UserDefined ||
                        local.SynthesizedKind == SynthesizedLocalKind.LambdaDisplayClass)
                    {
                        // create proxies for user-defined variables and for lambda closures:
                        Debug.Assert(local.RefKind == RefKind.None);
                        proxies.Add(local, MakeNonReusableLocalProxy(typeMap, variablesCaptured, local));
                    }
                }
                else
                {
                    var parameter = (ParameterSymbol)capturedVariable;
                    if (parameter.IsThis)
                    {
                        var proxyField = F.StateMachineField(method.ContainingType, GeneratedNames.ThisProxyName(), isPublic: true);
                        proxies.Add(parameter, new CapturedToFrameSymbolReplacement(proxyField, isReusable: false));

                        if (PreserveInitialParameterValues)
                        {
                            var initialThis = method.ContainingType.IsStructType() ?
                                F.StateMachineField(method.ContainingType, GeneratedNames.StateMachineThisParameterProxyName(), isPublic: true) : proxyField;

                            initialParameters.Add(parameter, new CapturedToFrameSymbolReplacement(initialThis, isReusable: false));
                        }
                    }
                    else
                    {
                        var proxyField = F.StateMachineField(typeMap.SubstituteType(parameter.Type), parameter.Name, isPublic: true);
                        proxies.Add(parameter, new CapturedToFrameSymbolReplacement(proxyField, isReusable: false));

                        if (PreserveInitialParameterValues)
                        {
                            string proxyName = GeneratedNames.StateMachineParameterProxyName(parameter.Name);
                            initialParameters.Add(parameter, new CapturedToFrameSymbolReplacement(
                                F.StateMachineField(typeMap.SubstituteType(parameter.Type), proxyName, isPublic: true), 
                                isReusable: false));
                        }

                        if (parameter.Type.IsRestrictedType())
                        {
                            // CS4013: Instance of type '{0}' cannot be used inside an anonymous function, query expression, iterator block or async method
                            diagnostics.Add(ErrorCode.ERR_SpecialByRefInLambda, parameter.Locations[0], parameter.Type);
                        }
                    }
                }
            }

            return proxies;
        }

        private CapturedSymbolReplacement MakeNonReusableLocalProxy(TypeMap TypeMap, MultiDictionary<Symbol, CSharpSyntaxNode> locations, LocalSymbol local)
        {
            Debug.Assert(local.RefKind == RefKind.None);
            CapturedSymbolReplacement result = new CapturedToFrameSymbolReplacement(MakeHoistedLocalField(TypeMap, local, local.Type), isReusable: false);

            if (local.Type.IsRestrictedType())
            {
                foreach (CSharpSyntaxNode syntax in locations[local])
                {
                    // CS4013: Instance of type '{0}' cannot be used inside an anonymous function, query expression, iterator block or async method
                    diagnostics.Add(ErrorCode.ERR_SpecialByRefInLambda, syntax.Location, local.Type);
                }
            }

            return result;
        }

        private int nextLocalNumber = 1;

        private SynthesizedFieldSymbolBase MakeHoistedLocalField(TypeMap TypeMap, LocalSymbol local, TypeSymbol type)
        {
            Debug.Assert(local.SynthesizedKind == SynthesizedLocalKind.UserDefined ||
                         local.SynthesizedKind == SynthesizedLocalKind.LambdaDisplayClass);

            int index = nextLocalNumber++;

            // Special Case: There's logic in the EE to recognize locals that have been captured by a lambda
            // and would have been hoisted for the state machine.  Basically, we just hoist the local containing
            // the instance of the lambda display class and retain its original name (rather than using an
            // iterator local name).  See FUNCBRECEE::ImportIteratorMethodInheritedLocals.
            string fieldName = (local.SynthesizedKind == SynthesizedLocalKind.LambdaDisplayClass)
                ? GeneratedNames.MakeLambdaDisplayClassStorageName(index)
                : GeneratedNames.MakeHoistedLocalFieldName(local.Name, index);

            return F.StateMachineField(TypeMap.SubstituteType(type), fieldName, index);
        }

        private BoundStatement ReplaceOriginalMethod()
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

            bodyBuilder.Add(GenerateReplacementBody(stateMachineVariable, frameType));
            return F.Block(
                ImmutableArray.Create(stateMachineVariable),
                bodyBuilder.ToImmutableAndFree());
        }

        protected SynthesizedImplementationMethod OpenMethodImplementation(
            MethodSymbol methodToImplement,
            string methodName = null,
            bool debuggerHidden = false,
            bool generateDebugInfo = true,
            bool hasMethodBodyDependency = false)
        {
            var result = new SynthesizedStateMachineMethod(methodName, methodToImplement, (StateMachineTypeSymbol)F.CurrentClass, null, debuggerHidden, generateDebugInfo, hasMethodBodyDependency);
            F.ModuleBuilderOpt.AddSynthesizedDefinition(F.CurrentClass, result);
            F.CurrentMethod = result;
            return result;
        }

        protected MethodSymbol OpenPropertyImplementation(
            MethodSymbol getterToImplement,
            bool debuggerHidden = false,
            bool hasMethodBodyDependency = false)
        {
            var prop = new SynthesizedStateMachineProperty(getterToImplement, (StateMachineTypeSymbol)F.CurrentClass, debuggerHidden, hasMethodBodyDependency);
            F.ModuleBuilderOpt.AddSynthesizedDefinition(F.CurrentClass, prop);

            var getter = prop.GetMethod;
            F.ModuleBuilderOpt.AddSynthesizedDefinition(F.CurrentClass, getter);

            F.CurrentMethod = getter;
            return getter;
        }

        protected bool IsDebuggerHidden(MethodSymbol method)
        {
            var debuggerHiddenAttribute = F.Compilation.GetWellKnownType(WellKnownType.System_Diagnostics_DebuggerHiddenAttribute);
            foreach (var a in this.method.GetAttributes())
            {
                if (a.AttributeClass == debuggerHiddenAttribute) return true;
            }

            return false;
        }
    }
}