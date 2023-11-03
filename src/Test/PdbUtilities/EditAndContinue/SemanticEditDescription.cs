﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests;

internal sealed class SemanticEditDescription(
    SemanticEditKind kind,
    Func<Compilation, ISymbol> symbolProvider,
    Func<Compilation, ISymbol>? newSymbolProvider = null,
    bool preserveLocalVariables = false)
{
    public readonly SemanticEditKind Kind = kind;
    public readonly Func<Compilation, ISymbol> SymbolProvider = symbolProvider;
    public readonly Func<Compilation, ISymbol> NewSymbolProvider = newSymbolProvider ?? symbolProvider;
    public readonly bool PreserveLocalVariables = preserveLocalVariables;
}
