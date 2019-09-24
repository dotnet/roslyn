// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal static class UnitTestingSymbolExtensions
    {
        public static SymbolKey UnitTesting_GetSymbolKey(this ISymbol symbol, CancellationToken cancellationToken = default)
            => symbol.GetSymbolKey(cancellationToken);
    }
}
