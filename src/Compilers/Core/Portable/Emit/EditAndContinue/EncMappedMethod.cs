// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis.Emit;

internal readonly struct EncMappedMethod(IMethodSymbolInternal precedingCompilationMethod, Func<SyntaxNode, SyntaxNode?>? syntaxMap, Func<SyntaxNode, RuntimeRudeEdit?>? runtimeRudeEdit)
{
    /// <summary>
    /// The method from the immediately preceding compilation.
    /// Used for syntax offset calculations.
    /// </summary>
    public readonly IMethodSymbolInternal PrecedingCompilationMethod = precedingCompilationMethod;

    public readonly Func<SyntaxNode, SyntaxNode?>? SyntaxMap = syntaxMap;
    public readonly Func<SyntaxNode, RuntimeRudeEdit?>? RuntimeRudeEdit = runtimeRudeEdit;
}
