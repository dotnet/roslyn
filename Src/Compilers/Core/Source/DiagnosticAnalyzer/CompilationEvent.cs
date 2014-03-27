// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    public abstract class CompilationEvent
    {
        protected CompilationEvent(Compilation compilation)
        {
            this.Compilation = compilation;
        }

        public Compilation Compilation { get; private set; }

        /// <summary>
        /// Flush any cached data in this <see cref="CompilationEvent"/> to minimize space usage (at the possible expense of time later).
        /// The principal effect of this is to free cached information on events that have a <see cref="SemanticModel"/>.
        /// </summary>
        public virtual void FlushCache() { }

        /// <summary>
        /// The first event placed into a compilation's event queue.
        /// </summary>
        public class CompilationStarted : CompilationEvent
        {
            public CompilationStarted(Compilation compilation) : base(compilation) { }
            public override string ToString()
            {
                return "CompilationStarted";
            }
        }

        /// <summary>
        /// The last event placed into a compilation's event queue.
        /// </summary>
        public class CompilationCompleted : CompilationEvent
        {
            public CompilationCompleted(Compilation compilation) : base(compilation) { }
            public override string ToString()
            {
                return "CompilationCompleted";
            }
        }

        /// <summary>
        /// An event for each declaration in the program (namespace, type, method, field, parameter, etc).
        /// Note that some symbols may have multiple declarations (namespaces, partial types) and may therefore
        /// have multiple events.
        /// </summary>
        public class SymbolDeclared : CompilationEvent
        {
            public SymbolDeclared(Compilation compilation, ISymbol symbol) : base(compilation)
            {
                this.Symbol = symbol;
            }
            public SymbolDeclared(Compilation compilation, ISymbol symbol, Lazy<SemanticModel> lazySemanticModel) : this(compilation, symbol)
            {
                this.lazySemanticModel = lazySemanticModel;
            }
            public ISymbol Symbol { get; private set; }

            // At most one of these should be non-null.
            private Lazy<SemanticModel> lazySemanticModel;
            private SemanticModel semanticModel;
            private WeakReference<SemanticModel> weakModel = null;
            public SemanticModel SemanticModel(SyntaxReference reference)
            {
                lock (this)
                {
                    var semanticModel = this.semanticModel;
                    if (semanticModel == null && this.lazySemanticModel != null)
                    {
                        this.semanticModel = semanticModel = this.lazySemanticModel.Value;
                        this.lazySemanticModel = null;
                    }
                    if (semanticModel == null && this.weakModel != null)
                    {
                        this.weakModel.TryGetTarget(out semanticModel);
                    }
                    if (semanticModel == null || semanticModel.SyntaxTree != reference.SyntaxTree)
                    {
                        semanticModel = Compilation.GetSemanticModel(reference.SyntaxTree);
                        this.weakModel = new WeakReference<SemanticModel>(semanticModel);
                    }

                    return semanticModel;
                }
            }
            override public void FlushCache()
            {
                lock (this)
                {
                    var semanticModel = this.semanticModel;
                    this.lazySemanticModel = null;
                    if (semanticModel == null) return;
                    this.weakModel = new WeakReference<SemanticModel>(semanticModel);
                    this.semanticModel = null;
                }
            }
            public override string ToString()
            {
                var refs = Symbol.DeclaringSyntaxReferences;
                var loc = refs.Length != 0 ? " @ " + refs[0].GetLocation().GetLineSpan() : null;
                return "SymbolDeclared(" + this.Symbol.Name + " " + this.Symbol + loc + ")";
            }
        }

        public class CompilationUnitCompleted : CompilationEvent
        {
            public CompilationUnitCompleted(Compilation compilation, SemanticModel semanticModel, SyntaxTree compilationUnit) : base(compilation)
            {
                this.SemanticModel = semanticModel;
                this.CompilationUnit = compilationUnit;
            }
            private SemanticModel semanticModel;
            private WeakReference<SemanticModel> weakModel = null;
            public SemanticModel SemanticModel
            {
                get
                {
                    var semanticModel = this.semanticModel;
                    var weakModel = this.weakModel;
                    if (semanticModel == null && weakModel == null || !weakModel.TryGetTarget(out semanticModel))
                    {
                        semanticModel = Compilation.GetSemanticModel(CompilationUnit);
                        this.weakModel = new WeakReference<SemanticModel>(semanticModel);
                    }
                    return semanticModel;
                }
                private set
                {
                    this.semanticModel = value;
                }
            }
            override public void FlushCache()
            {
                var semanticModel = this.semanticModel;
                if (semanticModel == null) return;
                this.weakModel = new WeakReference<SemanticModel>(semanticModel);
                this.semanticModel = null;
            }

            public SyntaxTree CompilationUnit { get; private set; }
            public override string ToString()
            {
                return "CompilationUnitCompleted(" + CompilationUnit.FilePath + ")";
            }
        }
    }

}
