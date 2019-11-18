﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal static class UnitTestingSymbolExtensions
    {
        public static UnitTestingSymbolKeyWrapper GetSymbolKey(this ISymbol symbol, CancellationToken cancellationToken)
            => new UnitTestingSymbolKeyWrapper(SymbolKeyExtensions.GetSymbolKey(symbol, cancellationToken));
    }
}
