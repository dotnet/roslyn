// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;

#pragma warning disable 8618
// ReSharper disable UnassignedGetOnlyAutoProperty

namespace Metalama.Compiler
{
    // ReSharper disable once ClassCannotBeInstantiated
    public sealed class DiagnosticFilteringRequest
    {
        public Diagnostic Diagnostic { get; }

        public SyntaxNode SyntaxNode { get; }

        public Compilation Compilation { get; }

        public bool IsSuppressed { get; private set; }

        public ISymbol Symbol { get; }

        public void Suppress()
        {
            this.IsSuppressed = true;
        }

#if !METALAMA_COMPILER_INTERFACE
        internal DiagnosticFilteringRequest(Diagnostic diagnostic, SyntaxNode syntaxNode, Compilation compilation, ISymbol symbol)
        {
            Diagnostic = diagnostic;
            SyntaxNode = syntaxNode;
            Compilation = compilation;
            this.Symbol = symbol;
        }
#else
        private DiagnosticFilteringRequest()
        {
        }
#endif
    }
}
