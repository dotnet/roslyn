// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Threading;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal static class UnitTestingSymbolExtensions
    {
        [Obsolete("Use GetSymbolKeyString instead")]
        public static UnitTestingSymbolKeyWrapper GetSymbolKey(this ISymbol symbol, CancellationToken cancellationToken)
            => new UnitTestingSymbolKeyWrapper(SymbolKeyExtensions.GetSymbolKey(symbol, cancellationToken));

        public static string GetSymbolKeyString(this ISymbol symbol, CancellationToken cancellationToken)
            => SymbolKey.Create(symbol, cancellationToken).ToString();

    }
}
