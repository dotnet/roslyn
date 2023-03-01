// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp;

internal sealed class ListPatternBufferInfo
{
    public readonly TypeSymbol BufferType;
    public readonly MethodSymbol Constructor;
    public readonly MethodSymbol TryGetElementFromStartMethod;
    public readonly MethodSymbol GetElementFromEndMethod;

    public ListPatternBufferInfo(
        TypeSymbol bufferType,
        MethodSymbol constructor,
        MethodSymbol tryGetElementFromStartMethod,
        MethodSymbol getElementFromEndMethod)
    {
        BufferType = bufferType;
        Constructor = constructor;
        TryGetElementFromStartMethod = tryGetElementFromStartMethod;
        GetElementFromEndMethod = getElementFromEndMethod;
    }
}
