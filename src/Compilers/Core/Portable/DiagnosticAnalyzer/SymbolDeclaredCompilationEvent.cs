// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// An event for each declaration in the program (namespace, type, method, field, parameter, etc).
    /// Note that some symbols may have multiple declarations (namespaces, partial types) and may therefore
    /// have multiple events.
    /// </summary>
    internal sealed class SymbolDeclaredCompilationEvent : CompilationEvent
    {
        private readonly Lazy<ImmutableArray<SyntaxReference>> _lazyCachedDeclaringReferences;

        public SymbolDeclaredCompilationEvent(Compilation compilation, ISymbol symbol) : base(compilation)
        {
            this.Symbol = symbol;
            this._lazyCachedDeclaringReferences = new Lazy<ImmutableArray<SyntaxReference>>(() => symbol.DeclaringSyntaxReferences);
        }

        public SymbolDeclaredCompilationEvent(Compilation compilation, ISymbol symbol, Lazy<SemanticModel> lazySemanticModel) : this(compilation, symbol)
        {
            _lazySemanticModel = lazySemanticModel;
        }
        private SymbolDeclaredCompilationEvent(SymbolDeclaredCompilationEvent original, SemanticModel newSemanticModel) : this(original.Compilation, original.Symbol)
        {
            _semanticModel = newSemanticModel;
        }

        public ISymbol Symbol { get; }

        // PERF: We avoid allocations in re-computing syntax references for declared symbol during event processing by caching them directly on this member.
        public ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => _lazyCachedDeclaringReferences.Value;

        // At most one of these should be non-null.
        private Lazy<SemanticModel> _lazySemanticModel;
        private SemanticModel _semanticModel;
        private WeakReference<SemanticModel> _weakModel;

        /// <summary>
        /// Lockable object only instance is knowledgeable about.
        /// </summary>
        private readonly object _gate = new object();

        public SemanticModel SemanticModel(SyntaxReference reference)
        {
            lock (_gate)
            {
                var semanticModel = _semanticModel;
                if (semanticModel == null && _lazySemanticModel != null)
                {
                    _semanticModel = semanticModel = _lazySemanticModel.Value;
                    _lazySemanticModel = null;
                }
                if (semanticModel == null)
                {
                    _weakModel?.TryGetTarget(out semanticModel);
                }
                if (semanticModel == null || semanticModel.SyntaxTree != reference.SyntaxTree)
                {
                    semanticModel = Compilation.GetSemanticModel(reference.SyntaxTree);
                    _weakModel = new WeakReference<SemanticModel>(semanticModel);
                }

                return semanticModel;
            }
        }
        override public void FlushCache()
        {
            lock (_gate)
            {
                var semanticModel = _semanticModel;
                _lazySemanticModel = null;
                if (semanticModel == null) return;
                _weakModel = new WeakReference<SemanticModel>(semanticModel);
                _semanticModel = null;
            }
        }

        public SymbolDeclaredCompilationEvent WithSemanticModel(SemanticModel model)
        {
            return new SymbolDeclaredCompilationEvent(this, model);
        }

        private static SymbolDisplayFormat s_displayFormat = SymbolDisplayFormat.FullyQualifiedFormat;
        public override string ToString()
        {
            var name = this.Symbol.Name;
            if (name == "") name = "<empty>";
            var loc = DeclaringSyntaxReferences.Length != 0 ? " @ " + String.Join(", ", System.Linq.Enumerable.Select(DeclaringSyntaxReferences, r => r.GetLocation().GetLineSpan())) : null;
            return "SymbolDeclaredCompilationEvent(" + name + " " + this.Symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat) + loc + ")";
        }
    }
}
