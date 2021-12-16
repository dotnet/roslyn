// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal static class UnitTestingSymbolExtensions
    {
        public static string GetSymbolKeyString(this ISymbol symbol, CancellationToken cancellationToken)
            => SymbolKey.Create(symbol, cancellationToken).ToString();

    }
}
