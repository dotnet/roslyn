// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

#pragma warning disable 8618
// ReSharper disable UnassignedGetOnlyAutoProperty

namespace Caravela.Compiler
{

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

#if !CARAVELA_COMPILER_INTERFACE
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
