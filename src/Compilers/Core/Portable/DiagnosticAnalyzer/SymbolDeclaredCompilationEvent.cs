// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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

        public SymbolDeclaredCompilationEvent(Compilation compilation, ISymbol symbol, SemanticModel? semanticModelWithCachedBoundNodes = null)
            : base(compilation)
        {
            Symbol = symbol;
            SemanticModelWithCachedBoundNodes = semanticModelWithCachedBoundNodes;
            _lazyCachedDeclaringReferences = new Lazy<ImmutableArray<SyntaxReference>>(() => symbol.DeclaringSyntaxReferences);
        }

        public ISymbol Symbol { get; }
        public SemanticModel? SemanticModelWithCachedBoundNodes { get; }

        // PERF: We avoid allocations in re-computing syntax references for declared symbol during event processing by caching them directly on this member.
        public ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => _lazyCachedDeclaringReferences.Value;

        public override string ToString()
        {
            var name = Symbol.Name;
            if (name == "") name = "<empty>";
            var loc = DeclaringSyntaxReferences.Length != 0 ? " @ " + string.Join(", ", System.Linq.Enumerable.Select(DeclaringSyntaxReferences, r => r.GetLocation().GetLineSpan())) : null;
            return "SymbolDeclaredCompilationEvent(" + name + " " + Symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat) + loc + ")";
        }
    }
}
