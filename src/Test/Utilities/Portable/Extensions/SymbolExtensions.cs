// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Test.Extensions
{
    internal static class SymbolExtensions
    {
        public static string ToTestDisplayString(this ISymbol symbol)
        {
            return symbol.ToDisplayString(SymbolDisplayFormat.TestFormat);
        }
    }
}
