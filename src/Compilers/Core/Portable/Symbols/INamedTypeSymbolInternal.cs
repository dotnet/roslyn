// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Symbols
{
    internal interface INamedTypeSymbolInternal : ITypeSymbolInternal
    {
        /// <summary>
        /// For enum types, gets the underlying type. Returns null on all other
        /// kinds of types.
        /// </summary>
        INamedTypeSymbolInternal EnumUnderlyingType { get; }
    }
}
