// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Utilities;

internal readonly struct CSharpLightweightOverloadResolution(
    SemanticModel semanticModel,
    int position,
    SeparatedSyntaxList<ArgumentSyntax> arguments)
{
    private readonly LightweightOverloadResolution _overloadResolution = new(CSharpSemanticFacts.Instance, semanticModel, position, arguments);

    public (IMethodSymbol? method, int parameterIndex) RefineOverloadAndPickParameter(SymbolInfo symbolInfo, ImmutableArray<IMethodSymbol> candidates)
        => _overloadResolution.RefineOverloadAndPickParameter(symbolInfo, candidates);

    public int FindParameterIndexIfCompatibleMethod(IMethodSymbol method)
        => _overloadResolution.FindParameterIndexIfCompatibleMethod(method);
}
