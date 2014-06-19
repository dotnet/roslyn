// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract class StateMachineRewriter
    {
        protected readonly BoundStatement body;
        protected readonly MethodSymbol method;
        protected readonly TypeCompilationState compilationState;
        protected readonly DiagnosticBag diagnostics;
        protected readonly SyntheticBoundNodeFactory F;
        protected readonly SynthesizedContainer stateMachineClass;
        protected FieldSymbol stateField;
        protected Dictionary<Symbol, CapturedSymbolReplacement> variableProxies;
        protected HashSet<Symbol> variablesCaptured;
        protected Dictionary<Symbol, CapturedSymbolReplacement> initialParameters;
        protected readonly bool generateDebugInfo;

        protected StateMachineRewriter(
            BoundStatement body,
            MethodSymbol method,
            SynthesizedContainer stateMachineClass,
            TypeCompilationState compilationState,
            DiagnosticBag diagnostics,
            bool generateDebugInfo)
        {
            this.body = body;
            this.method = method;
            this.stateMachineClass = stateMachineClass;
            this.compilationState = compilationState;
            this.diagnostics = diagnostics;
            this.generateDebugInfo = generateDebugInfo;
            this.F = new SyntheticBoundNodeFactory(method, body.Syntax, compilationState, diagnostics);
            Debug.Assert(F.CurrentClass == method.ContainingType);
            Debug.Assert(F.Syntax == body.Syntax);
        }

        /// <summary>
        /// True if the initial values of locals in the rewritten method need to be preserved. (e.g. enumerable iterator methods)
        /// </summary>
        abstract protected bool PreserveInitialLocals { get; }

        /// <summary>
        /// Add fields to the state machine class that are unique to async or iterator methods.
        /// </summary>
        abstract protected void GenerateFields();

        /// <summary>
        /// Initialize the state machine class.
        /// </summary>
        abstract protected void InitializeStateMachine(ArrayBuilder<BoundStatement> bodyBuilder, NamedTypeSymbol frameType, LocalSymbol stateMachineLocal);

        /// <summary>
        /// Generate implementation-specific state machine initialization for the replacement method body.
        /// </summary>
        abstract protected BoundStatement GenerateReplacementBody(LocalSymbol stateMachineVariable, NamedTypeSymbol frameType);

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

            TypeMap TypeMap = stateMachineClass.TypeMap;
            F.OpenNestedType(stateMachineClass);

            // Add a field: int _state
            var intType = F.SpecialType(SpecialType.System_Int32);
            stateField = F.StateMachineField(intType, GeneratedNames.MakeStateMachineStateName(), IsStateFieldPublic);

            GenerateFields();

            // and fields for the initial values of all the parameters of the method
            if (PreserveInitialLocals)
            {
                initialParameters = new Dictionary<Symbol, CapturedSymbolReplacement>();
            }

            // add fields for the captured variables of the method
            var dictionary = IteratorAndAsyncCaptureWalker.Analyze(compilationState.ModuleBuilderOpt.Compilation, method, body);
            IOrderedEnumerable<Symbol> captured =
                from local in dictionary.Keys
                orderby local.Name, local.Locations.Length == 0 ? 0 : local.Locations[0].SourceSpan.Start
                select local;
            this.variablesCaptured = new HashSet<Symbol>(captured);
            this.variableProxies = new Dictionary<Symbol, CapturedSymbolReplacement>();

            CreateInitialProxies(TypeMap, captured, dictionary);
            GenerateMethodImplementations();

            // Return a replacement body for the original method
            return ReplaceOriginalMethod();
        }

        private void CreateInitialProxies(
           TypeMap TypeMap,
           IOrderedEnumerable<Symbol> captured,
           MultiDictionary<Symbol, CSharpSyntaxNode> locations)
        {
            foreach (var sym in captured)
            {
                var local = sym as LocalSymbol;
                if ((object)local != null && local.DeclarationKind != LocalDeclarationKind.CompilerGenerated)
                {
                    Debug.Assert(local.RefKind == RefKind.None); // there are no user-declared ref variables
                    MakeInitialProxy(TypeMap, locations, local);
                    continue;
                }

                var parameter = sym as ParameterSymbol;
                if ((object)parameter != null)
                {
                    if (parameter.IsThis)
                    {
                        var proxyField = F.StateMachineField(method.ContainingType, GeneratedNames.IteratorThisProxyName(), isPublic: true);
                        variableProxies.Add(parameter, new CapturedToFrameSymbolReplacement(proxyField));

                        if (PreserveInitialLocals)
                        {
                            var initialThis = method.ContainingType.IsStructType() ? 
                                F.StateMachineField(method.ContainingType, GeneratedNames.IteratorThisProxyProxyName(), isPublic: true) : proxyField;

                            initialParameters.Add(parameter, new CapturedToFrameSymbolReplacement(initialThis));
                        }
                    }
                    else
                    {
                        var proxyField = F.StateMachineField(TypeMap.SubstituteType(parameter.Type), parameter.Name, isPublic: true);
                        variableProxies.Add(parameter, new CapturedToFrameSymbolReplacement(proxyField));

                        if (PreserveInitialLocals)
                        {
                            string proxyName = GeneratedNames.IteratorParameterProxyName(parameter.Name);
                            initialParameters.Add(parameter, new CapturedToFrameSymbolReplacement(
                                F.StateMachineField(TypeMap.SubstituteType(parameter.Type), proxyName, isPublic: true)));
                        }
                        if (parameter.Type.IsRestrictedType())
                        {
                            // CS4013: Instance of type '{0}' cannot be used inside an anonymous function, query expression, iterator block or async method
                            diagnostics.Add(ErrorCode.ERR_SpecialByRefInLambda, parameter.Locations[0], parameter.Type);
                        }
                    }
                }
            }
        }

        private void MakeInitialProxy(TypeMap TypeMap, MultiDictionary<Symbol, CSharpSyntaxNode> locations, LocalSymbol local)
        {
            Debug.Assert(local.RefKind == RefKind.None);
            CapturedSymbolReplacement result = new CapturedToFrameSymbolReplacement(MakeHoistedField(TypeMap, local, local.Type));

            if (local.Type.IsRestrictedType())
            {
                foreach (CSharpSyntaxNode syntax in locations[local])
                {
                    // CS4013: Instance of type '{0}' cannot be used inside an anonymous function, query expression, iterator block or async method
                    diagnostics.Add(ErrorCode.ERR_SpecialByRefInLambda, syntax.Location, local.Type);
                }
            }

            variableProxies.Add(local, result);
        }

        private int nextLocalNumber = 1;
        private SynthesizedFieldSymbolBase MakeHoistedField(TypeMap TypeMap, LocalSymbol local, TypeSymbol type)
        {
            Debug.Assert(local.DeclarationKind != LocalDeclarationKind.CompilerGenerated);
            int index = nextLocalNumber++;

            // Special Case: There's logic in the EE to recognize locals that have been captured by a lambda
            // and would have been hoisted for the state machine.  Basically, we just hoist the local containing
            // the instance of the lambda display class and retain its original name (rather than using an
            // iterator local name).  See FUNCBRECEE::ImportIteratorMethodInheritedLocals.
            string fieldName = local.DeclarationKind == LocalDeclarationKind.CompilerGeneratedLambdaDisplayClassLocal
                ? local.Name
                : GeneratedNames.MakeIteratorLocalName(local.Name, index);

            return F.StateMachineField(TypeMap.SubstituteType(type), fieldName, index);
        }

        private BoundStatement ReplaceOriginalMethod()
        {
            F.CurrentMethod = method;
            var bodyBuilder = ArrayBuilder<BoundStatement>.GetInstance();
            var frameType = method.IsGenericMethod ? stateMachineClass.Construct(method.TypeArguments) : stateMachineClass;
            LocalSymbol stateMachineVariable = F.SynthesizedLocal(frameType, null);
            InitializeStateMachine(bodyBuilder, frameType, stateMachineVariable);

            // plus code to initialize all of the parameter proxies result.proxy
            Dictionary<Symbol, CapturedSymbolReplacement> copyDest = PreserveInitialLocals ? initialParameters : variableProxies;

            // starting with the "this" proxy
            if (!method.IsStatic)
            {
                Debug.Assert((object)method.ThisParameter != null);

                CapturedSymbolReplacement proxy;
                if (copyDest.TryGetValue(method.ThisParameter, out proxy))
                    bodyBuilder.Add(F.Assignment(proxy.Replacement(F.Syntax, frameType1 => F.Local(stateMachineVariable)), F.This()));
            }

            foreach (var parameter in method.Parameters)
            {
                CapturedSymbolReplacement proxy;
                if (copyDest.TryGetValue(parameter, out proxy))
                    bodyBuilder.Add(F.Assignment(proxy.Replacement(F.Syntax, frameType1 => F.Local(stateMachineVariable)),
                                                 F.Parameter(parameter)));
            }

            bodyBuilder.Add(GenerateReplacementBody(stateMachineVariable, frameType));
            return F.Block(
                ImmutableArray.Create<LocalSymbol>(stateMachineVariable),
                bodyBuilder.ToImmutableAndFree());
        }

        protected SynthesizedImplementationMethod OpenMethodImplementation(
            MethodSymbol methodToImplement,
            string methodName = null,
            bool debuggerHidden = false, 
            bool hasMethodBodyDependency = false,
            MethodSymbol asyncKickoffMethod = null)
        {
            var result = new SynthesizedStateMachineMethod(methodName, methodToImplement, F.CurrentClass, asyncKickoffMethod, null, debuggerHidden, hasMethodBodyDependency);
            F.ModuleBuilderOpt.AddSynthesizedDefinition(F.CurrentClass, result);
            F.CurrentMethod = result;
            return result;
        }

        protected MethodSymbol OpenPropertyImplementation(
            MethodSymbol getterToImplement, 
            bool debuggerHidden = false, 
            bool hasMethodBodyDependency = false)
        {
            var prop = new SynthesizedStateMachineProperty(getterToImplement, F.CurrentClass, debuggerHidden, hasMethodBodyDependency);
            F.ModuleBuilderOpt.AddSynthesizedDefinition(F.CurrentClass, prop);

            var getter = prop.GetMethod;
            F.ModuleBuilderOpt.AddSynthesizedDefinition(F.CurrentClass, getter);

            F.CurrentMethod = getter;
            return getter;
        }
    }
}