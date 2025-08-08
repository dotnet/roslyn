// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities.Lightup
{
    internal static class ITypeParameterSymbolExtensions
    {
        public static bool HasReferenceTypeConstraint(this ITypeParameterSymbol typeParameterSymbol)
            => typeParameterSymbol.HasReferenceTypeConstraint;
    }
}
