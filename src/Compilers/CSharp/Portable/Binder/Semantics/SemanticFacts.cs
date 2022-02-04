// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{

    internal partial class Symbol
    {
        /// <summary>
        /// Checks if 'symbol' is accessible from within named type 'within'.  If 'symbol' is accessed off
        /// of an expression then 'throughTypeOpt' is the type of that expression. This is needed to
        /// properly do protected access checks.
        /// </summary>
        public static bool IsSymbolAccessible(
            Symbol symbol,
            NamedTypeSymbol within,
            NamedTypeSymbol throughTypeOpt = null)
        {
            if ((object)symbol == null)
            {
                throw new ArgumentNullException(nameof(symbol));
            }

            if ((object)within == null)
            {
                throw new ArgumentNullException(nameof(within));
            }

            var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
            return AccessCheck.IsSymbolAccessible(
                symbol,
                within,
                ref discardedUseSiteInfo,
                throughTypeOpt);
        }

        /// <summary>
        /// Checks if 'symbol' is accessible from within assembly 'within'.  
        /// </summary>
        public static bool IsSymbolAccessible(
            Symbol symbol,
            AssemblySymbol within)
        {
            if ((object)symbol == null)
            {
                throw new ArgumentNullException(nameof(symbol));
            }

            if ((object)within == null)
            {
                throw new ArgumentNullException(nameof(within));
            }

            var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
            return AccessCheck.IsSymbolAccessible(symbol, within, ref discardedUseSiteInfo);
        }
    }
}
