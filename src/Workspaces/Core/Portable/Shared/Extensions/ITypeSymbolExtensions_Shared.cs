// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class ITypeSymbolExtensions
    {
        public static bool IsSystemVoid([NotNullWhen(returnValue: true)] this ITypeSymbol? symbol)
        {
            return symbol?.SpecialType == SpecialType.System_Void;
        }
    }
}
