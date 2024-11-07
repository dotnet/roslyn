// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

#pragma warning disable 8618
// ReSharper disable UnassignedGetOnlyAutoProperty

namespace Metalama.Compiler;

// ReSharper disable once ClassCannotBeInstantiated
public readonly struct DiagnosticFilteringRequest
{
    /// <summary>
    ///     Gets the <see cref="Diagnostic" /> whose suppression is being considered.
    /// </summary>
    public Diagnostic Diagnostic { get; }

    /// <summary>
    ///     Gets the <see cref="SyntaxNode" /> on which the diagnostic was reported.
    /// </summary>
    public SyntaxNode SyntaxNode { get; }

    /// <summary>
    ///     Gets the current <see cref="Compilation" />.
    /// </summary>
    public Compilation Compilation { get; }

    /// <summary>
    ///     Gets the <see cref="ISymbol" /> containing the <see cref="SyntaxNode" />.
    ///     The <see cref="DiagnosticFilterDelegate" /> will be invoked with different values of the <see cref="Symbol" />
    ///     property, depth first.
    /// </summary>
    public ISymbol Symbol { get; }

    internal DiagnosticFilteringRequest(Diagnostic diagnostic, SyntaxNode syntaxNode, Compilation compilation,
        ISymbol symbol)
    {
        Diagnostic = diagnostic;
        SyntaxNode = syntaxNode;
        Compilation = compilation;
        Symbol = symbol;
    }
}

