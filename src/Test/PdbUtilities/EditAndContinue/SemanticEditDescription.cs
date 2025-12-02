// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests;

internal sealed class SemanticEditDescription(
    SemanticEditKind kind,
    Func<Compilation, ISymbol> symbolProvider,
    Func<Compilation, ISymbol>? newSymbolProvider = null,
    Func<SyntaxNode, RuntimeRudeEdit?>? rudeEdits = null,
    bool preserveLocalVariables = false)
{
    public readonly SemanticEditKind Kind = kind;
    public readonly Func<Compilation, ISymbol> SymbolProvider = symbolProvider;
    public readonly Func<Compilation, ISymbol> NewSymbolProvider = newSymbolProvider ?? symbolProvider;
    public readonly Func<SyntaxNode, RuntimeRudeEdit?>? RudeEdits = rudeEdits;
    public readonly bool PreserveLocalVariables = preserveLocalVariables;
}

internal sealed class ResourceEditDescription(
    ResourceEditKind kind,
    string name)
{
    public readonly ResourceEditKind Kind = kind;
    public readonly string Name = name;
}
