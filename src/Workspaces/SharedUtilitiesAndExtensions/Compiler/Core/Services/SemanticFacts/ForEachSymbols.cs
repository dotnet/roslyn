// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.LanguageService
{
    internal readonly struct ForEachSymbols
    {
        public readonly IMethodSymbol GetEnumeratorMethod;
        public readonly IMethodSymbol MoveNextMethod;
        public readonly IPropertySymbol CurrentProperty;
        public readonly IMethodSymbol DisposeMethod;
        public readonly ITypeSymbol ElementType;

        internal ForEachSymbols(IMethodSymbol getEnumeratorMethod,
                                IMethodSymbol moveNextMethod,
                                IPropertySymbol currentProperty,
                                IMethodSymbol disposeMethod,
                                ITypeSymbol elementType)
            : this()
        {
            this.GetEnumeratorMethod = getEnumeratorMethod;
            this.MoveNextMethod = moveNextMethod;
            this.CurrentProperty = currentProperty;
            this.DisposeMethod = disposeMethod;
            this.ElementType = elementType;
        }
    }
}
