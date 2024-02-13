// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Symbols
{
    internal interface INamedTypeSymbolInternal : ITypeSymbolInternal
    {
        /// <summary>
        /// For enum types, gets the underlying type. Returns null on all other
        /// kinds of types.
        /// </summary>
        INamedTypeSymbolInternal? EnumUnderlyingType { get; }

        ImmutableArray<ISymbolInternal> GetMembers();
        ImmutableArray<ISymbolInternal> GetMembers(string name);
    }
}
