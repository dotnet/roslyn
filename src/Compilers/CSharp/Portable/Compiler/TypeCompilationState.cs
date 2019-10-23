// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Represents the state of compilation of one particular type.
    /// This includes, for example, a collection of synthesized methods created during lowering. 
    /// </summary>
    /// <remarks>
    /// WARNING: Note that the collection class is not thread-safe and will 
    /// need to be revised if emit phase is changed to support multithreading when
    /// translating a particular type.
    /// </remarks>
    internal sealed class TypeCompilationState
    {
        /// <summary> Synthesized method info </summary>
        internal struct MethodWithBody
        {
            public readonly MethodSymbol Method;
            public readonly BoundStatement Body;
            public readonly ImportChain? ImportChain;

            internal MethodWithBody(MethodSymbol method, BoundStatement body, ImportChain? importChain)
            {
                RoslynDebug.Assert(method != null);
                RoslynDebug.Assert(body != null);

                this.Method = method;
                this.Body = body;
                this.ImportChain = importChain;
            }
        }

        /// <summary> Flat array of created methods, non-empty if not-null </summary>
        private ArrayBuilder<MethodWithBody>? _synthesizedMethods;

        /// <summary> 
        /// Map of wrapper methods created for base access of base type virtual methods from 
        /// other classes (like those created for lambdas...); actually each method symbol will 
        /// only need one wrapper to call it non-virtually.
        /// </summary>
        private Dictionary<MethodSymbol, MethodSymbol>? _wrappers;

        /// <summary>
        /// Type symbol being compiled, or null if we compile a synthesized type that doesn't have a symbol (e.g. PrivateImplementationDetails).
        /// </summary>
        private readonly NamedTypeSymbol _typeOpt;

        /// <summary>
        /// The builder for generating code, or null if not in emit phase.
        /// </summary>
        public readonly PEModuleBuilder ModuleBuilderOpt;

        /// <summary>
        /// Any generated methods that don't suppress debug info will use this
        /// list of debug imports.
        /// </summary>
        public ImportChain? CurrentImportChain;

        public readonly CSharpCompilation Compilation;

        public SynthesizedClosureEnvironment? StaticLambdaFrame;

        /// <summary>
        /// A graph of method->method references for this(...) constructor initializers.
        /// Used to detect and report initializer cycles.
        /// </summary>
        private SmallDictionary<MethodSymbol, MethodSymbol>? _constructorInitializers;

        public TypeCompilationState(NamedTypeSymbol typeOpt, CSharpCompilation compilation, PEModuleBuilder moduleBuilderOpt)
        {
            this.Compilation = compilation;
            _typeOpt = typeOpt;
            this.ModuleBuilderOpt = moduleBuilderOpt;
        }

        /// <summary>
        /// The type for which this compilation state is being used.
        /// </summary>
        public NamedTypeSymbol Type
        {
            get
            {
                // NOTE: currently it can be null if only private implementation type methods are compiled
                RoslynDebug.Assert((object)_typeOpt != null);
                return _typeOpt;
            }
        }

        /// <summary>
        /// The type passed to the runtime binder as context.
        /// </summary>
        public NamedTypeSymbol? DynamicOperationContextType
        {
            get
            {
                return this.ModuleBuilderOpt?.GetDynamicOperationContextType(this.Type);
            }
        }

        public bool Emitting
        {
            get { return ModuleBuilderOpt != null; }
        }

        public ArrayBuilder<MethodWithBody>? SynthesizedMethods
        {
            get { return _synthesizedMethods; }
            set
            {
                Debug.Assert(_synthesizedMethods == null);
                _synthesizedMethods = value;
            }
        }

        /// <summary> 
        /// Add a 'regular' synthesized method.
        /// </summary>
        public void AddSynthesizedMethod(MethodSymbol method, BoundStatement body)
        {
            if (_synthesizedMethods == null)
            {
                _synthesizedMethods = ArrayBuilder<MethodWithBody>.GetInstance();
            }

            _synthesizedMethods.Add(new MethodWithBody(method, body, CurrentImportChain));
        }

        /// <summary> 
        /// Add a 'wrapper' synthesized method and map it to the original one so it can be reused. 
        /// </summary>
        /// <remarks>
        /// Wrapper methods are created for base access of base type virtual methods from 
        /// other classes (like those created for lambdas...).
        /// </remarks>
        public void AddMethodWrapper(MethodSymbol method, MethodSymbol wrapper, BoundStatement body)
        {
            this.AddSynthesizedMethod(wrapper, body);

            if (_wrappers == null)
            {
                _wrappers = new Dictionary<MethodSymbol, MethodSymbol>();
            }

            _wrappers.Add(method, wrapper);
        }

        /// <summary> The index of the next wrapped method to be used </summary>
        public int NextWrapperMethodIndex
        {
            get { return _wrappers == null ? 0 : _wrappers.Count; }
        }

        /// <summary> 
        /// Get a 'wrapper' method for the original one. 
        /// </summary>
        /// <remarks>
        /// Wrapper methods are created for base access of base type virtual methods from 
        /// other classes (like those created for lambdas...).
        /// </remarks>
        public MethodSymbol? GetMethodWrapper(MethodSymbol method)
        {
            MethodSymbol? wrapper = null;
            return _wrappers != null && _wrappers.TryGetValue(method, out wrapper) ? wrapper : null;
        }

        /// <summary> Free resources allocated for this method collection </summary>
        public void Free()
        {
            if (_synthesizedMethods != null)
            {
                _synthesizedMethods.Free();
                _synthesizedMethods = null;
            }

            _wrappers = null;
            _constructorInitializers = null;
        }

        /// <summary>
        /// Report an error if adding the edge (method1, method2) to the ctor-initializer
        /// graph would add a new cycle to that graph.
        /// </summary>
        /// <param name="method1">a calling ctor</param>
        /// <param name="method2">the chained-to ctor</param>
        /// <param name="syntax">where to report a cyclic error if needed</param>
        /// <param name="diagnostics">a diagnostic bag for receiving the diagnostic</param>
        internal void ReportCtorInitializerCycles(MethodSymbol method1, MethodSymbol method2, SyntaxNode syntax, DiagnosticBag diagnostics)
        {
            // precondition and postcondition: the graph _constructorInitializers is acyclic.
            // If adding the edge (method1, method2) would induce a cycle, we report an error
            // and do not add it to the set of edges. If it would not induce a cycle we add
            // it to the set of edges and return.

            if (method1 == method2)
            {
                // direct recursion is diagnosed elsewhere
                throw ExceptionUtilities.Unreachable;
            }

            if (_constructorInitializers == null)
            {
                _constructorInitializers = new SmallDictionary<MethodSymbol, MethodSymbol>();
                _constructorInitializers.Add(method1, method2);
                return;
            }

            MethodSymbol next = method2;
            while (true)
            {
                if (_constructorInitializers.TryGetValue(next, out next))
                {
                    RoslynDebug.Assert((object)next != null);
                    if (method1 == next)
                    {
                        // We found a (new) cycle containing the edge (method1, method2). Report an
                        // error and do not add the edge.
                        diagnostics.Add(ErrorCode.ERR_IndirectRecursiveConstructorCall, syntax.Location, method1);
                        return;
                    }
                }
                else
                {
                    // we've reached the end of the path without finding a cycle. Add the new edge.
                    _constructorInitializers.Add(method1, method2);
                    return;
                }
            }
        }
    }
}
