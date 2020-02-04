// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class ISymbolExtensions
    {
        /// <summary>
        /// Returns true if this symbol contains anything unsafe within it.  for example
        /// List&lt;int*[]&gt; is unsafe, as it "int* Goo { get; }"
        /// </summary>
        public static bool IsUnsafe([NotNullWhen(returnValue: true)] this ISymbol? member)
        {
            // TODO(cyrusn): Defer to compiler code to handle this once it can.
            return member?.Accept(new IsUnsafeVisitor()) == true;
        }

        public static ImmutableArray<IParameterSymbol> GetParameters(this ISymbol? symbol)
        {
            switch (symbol)
            {
                case IMethodSymbol m: return m.Parameters;
                case IPropertySymbol nt: return nt.Parameters;
                default: return ImmutableArray<IParameterSymbol>.Empty;
            }
        }

        public static bool IsPointerType([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            return symbol is IPointerTypeSymbol;
        }
    }
}
