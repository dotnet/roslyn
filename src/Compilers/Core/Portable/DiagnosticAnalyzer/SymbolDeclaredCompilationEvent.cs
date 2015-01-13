// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// An event for each declaration in the program (namespace, type, method, field, parameter, etc).
    /// Note that some symbols may have multiple declarations (namespaces, partial types) and may therefore
    /// have multiple events.
    /// </summary>
    public sealed class SymbolDeclaredCompilationEvent : CompilationEvent
    {
        public SymbolDeclaredCompilationEvent(Compilation compilation, ISymbol symbol) : base(compilation)
        {
            this.Symbol = symbol;
        }
        public SymbolDeclaredCompilationEvent(Compilation compilation, ISymbol symbol, Lazy<SemanticModel> lazySemanticModel) : this(compilation, symbol)
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
        static SymbolDisplayFormat displayFormat = SymbolDisplayFormat.FullyQualifiedFormat;
        public override string ToString()
        {
            var refs = Symbol.DeclaringSyntaxReferences;
            var name = this.Symbol.Name;
            if (name == "") name = "<empty>";
            var loc = refs.Length != 0 ? " @ " + String.Join(", ", System.Linq.Enumerable.Select(refs, r => r.GetLocation().GetLineSpan())) : null;
            return "SymbolDeclaredCompilationEvent(" + name + " " + this.Symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat) + loc + ")";
        }
    }
}
