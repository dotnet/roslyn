// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageService;

internal readonly struct ForEachSymbols(
    IMethodSymbol? getEnumeratorMethod,
    IMethodSymbol? moveNextMethod,
    IPropertySymbol? currentProperty,
    IMethodSymbol? disposeMethod,
    ITypeSymbol? elementType)
{
    public readonly IMethodSymbol? GetEnumeratorMethod = getEnumeratorMethod;
    public readonly IMethodSymbol? MoveNextMethod = moveNextMethod;
    public readonly IPropertySymbol? CurrentProperty = currentProperty;
    public readonly IMethodSymbol? DisposeMethod = disposeMethod;
    public readonly ITypeSymbol? ElementType = elementType;
}
