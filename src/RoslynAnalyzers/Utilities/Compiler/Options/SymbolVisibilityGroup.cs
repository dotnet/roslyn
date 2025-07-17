// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Analyzer.Utilities
{
    /// <summary>
    /// Describes a group of effective <see cref="SymbolVisibility"/> for symbols.
    /// </summary>
    [Flags]
#pragma warning disable CA1714 // Flags enums should have plural names
    public enum SymbolVisibilityGroup
#pragma warning restore CA1714 // Flags enums should have plural names
    {
        // NOTE: Below fields names are used in the .editorconfig specification
        //       for symbol visibility analyzer option. Hence the names should *not* be modified,
        //       as that would be a breaking change for .editorconfig specification.
        None = 0x0,
        Public = 0x1,
        Internal = 0x2,
        Private = 0x4,
        Friend = Internal,
        All = Public | Internal | Private
    }

    internal static class SymbolVisibilityGroupExtensions
    {
        public static bool Contains(this SymbolVisibilityGroup symbolVisibilityGroup, SymbolVisibility symbolVisibility)
        {
            return symbolVisibility switch
            {
                SymbolVisibility.Public => (symbolVisibilityGroup & SymbolVisibilityGroup.Public) != 0,

                SymbolVisibility.Internal => (symbolVisibilityGroup & SymbolVisibilityGroup.Internal) != 0,

                SymbolVisibility.Private => (symbolVisibilityGroup & SymbolVisibilityGroup.Private) != 0,

                _ => throw new ArgumentOutOfRangeException(nameof(symbolVisibility), symbolVisibility, null),
            };
        }
    }
}
