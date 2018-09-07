// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Injected attribute symbols are injected early in the compilation, and so can be referenced in source.
    /// But we track their usage and only emit them if they are used.
    /// Their method bodies are always compiled, in case we do need to emit them.
    /// </summary>
    internal abstract class InjectedAttributeSymbol : SynthesizedEmbeddedAttributeSymbol
    {
        internal abstract void AddDiagnostics(DiagnosticBag recipient);

        // All the diagnostics involved in constructing this symbol will only be produced
        // if the symbol is referenced and so ends up emitted.
        // We collect all those diagnostics here.
        protected readonly DiagnosticBag _diagnostics;

        public InjectedAttributeSymbol(
            AttributeDescription description,
            NamespaceSymbol containingNamespace,
            CSharpCompilation compilation,
            Func<CSharpCompilation, NamedTypeSymbol, DiagnosticBag, ImmutableArray<MethodSymbol>> getConstructors)
            : this(description, containingNamespace, compilation, getConstructors, new DiagnosticBag())
        {
        }

        private InjectedAttributeSymbol(
            AttributeDescription description,
            NamespaceSymbol containingNamespace,
            CSharpCompilation compilation,
            Func<CSharpCompilation, NamedTypeSymbol, DiagnosticBag, ImmutableArray<MethodSymbol>> getConstructors,
            DiagnosticBag diagnostics)
            : base(description, containingNamespace, compilation, getConstructors, diagnostics)
        {
            _diagnostics = diagnostics;
        }
    }
}
