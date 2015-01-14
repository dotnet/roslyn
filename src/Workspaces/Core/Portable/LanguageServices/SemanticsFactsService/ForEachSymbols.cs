// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal struct ForEachSymbols
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
