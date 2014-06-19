// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
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
            public readonly ConsList<Imports> DebugImports;

            internal MethodWithBody(MethodSymbol method, BoundStatement body, ConsList<Imports> debugImports)
            {
                this.Method = method;
                this.Body = body;
                this.DebugImports = debugImports;
            }
        }

        /// <summary> Flat array of created methods, non-empty if not-null </summary>
        private ArrayBuilder<MethodWithBody> synthesizedMethods;

        /// <summary> 
        /// Map of wrapper methods created for base access of base type virtual methods from 
        /// other classes (like those created for lambdas...); actually each method symbol will 
        /// only need one wrapper to call it non-virtually.
        /// </summary>
        private Dictionary<MethodSymbol, MethodSymbol> wrappers;

        private int nextTempNumber;

        private readonly NamedTypeSymbol type;

        /// <summary>
        /// The builder for generating code, or null if not in emit phase.
        /// </summary>
        public readonly PEModuleBuilder ModuleBuilderOpt;

        /// <summary>
        /// Any generated methods that don't suppress debug info will use this
        /// list of debug imports.
        /// </summary>
        public ConsList<Imports> CurrentDebugImports { get; set; }

        public readonly CSharpCompilation Compilation;

        public TypeCompilationState(NamedTypeSymbol type, CSharpCompilation compilation, PEModuleBuilder moduleBuilderOpt)
        {
            this.Compilation = compilation;
            this.type = type;
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
                // TODO: is it used? if yes, make sure it is not accessed when type is not available; 
                Debug.Assert((object)this.type != null);
                return type;
            }
        }

        public bool Emitting
        {
            get { return ModuleBuilderOpt != null; }
        }

        public int GenerateTempNumber()
        {
            return nextTempNumber++;
        }

        /// <summary> 
        /// Add a 'regular' synthesized method.
        /// </summary>
        public bool HasSynthesizedMethods
        {
            get { return this.synthesizedMethods != null; }
        }

        public ArrayBuilder<MethodWithBody> SynthesizedMethods
        {
            get { return this.synthesizedMethods; }
        }

        public void AddSynthesizedMethod(MethodSymbol method, BoundStatement body)
        {
            if (this.synthesizedMethods == null)
            {
                this.synthesizedMethods = ArrayBuilder<MethodWithBody>.GetInstance();
            }

            synthesizedMethods.Add(new MethodWithBody(method, body, method.GenerateDebugInfo ? CurrentDebugImports : null));
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

            if (this.wrappers == null)
            {
                this.wrappers = new Dictionary<MethodSymbol, MethodSymbol>();
            }

            this.wrappers.Add(method, wrapper);
        }

        /// <summary> The index of the next wrapped method to be used </summary>
        public int NextWrapperMethodIndex
        {
            get { return this.wrappers == null ? 0 : this.wrappers.Count; }
        }

        /// <summary> 
        /// Get a 'wrapper' method for the original one. 
        /// </summary>
        /// <remarks>
        /// Wrapper methods are created for base access of base type virtual methods from 
        /// other classes (like those created for lambdas...).
        /// </remarks>
        public MethodSymbol GetMethodWrapper(MethodSymbol method)
        {
            MethodSymbol wrapper = null;
            return this.wrappers != null && this.wrappers.TryGetValue(method, out wrapper) ? wrapper : null;
        }

        /// <summary> Free resources allocated for this method collection </summary>
        public void Free()
        {
            if (this.synthesizedMethods != null)
            {
                this.synthesizedMethods.Free();
                this.synthesizedMethods = null;
            }

            this.wrappers = null;
        }
    }
}
