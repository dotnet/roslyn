// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    public static partial class SymbolFinder
    {
        /// <summary>
        /// Find the declared symbols from either source, referenced projects or metadata assemblies with the specified name.
        /// </summary>
        public static async Task<IEnumerable<ISymbol>> FindDeclarationsAsync(
            Project project, string name, bool ignoreCase, CancellationToken cancellationToken = default(CancellationToken))
        {
            var declarations = await DeclarationFinder.FindAllDeclarationsAsync(
                project, name, ignoreCase, cancellationToken).ConfigureAwait(false);
            return declarations.SelectAsArray(t => t.Symbol);
        }

        /// <summary>
        /// Find the declared symbols from either source, referenced projects or metadata assemblies with the specified name.
        /// </summary>
        public static async Task<IEnumerable<ISymbol>> FindDeclarationsAsync(
            Project project, string name, bool ignoreCase, SymbolFilter filter, CancellationToken cancellationToken = default(CancellationToken))
        {
            var declarations = await DeclarationFinder.FindAllDeclarationsAsync(
                project, name, ignoreCase, filter, cancellationToken).ConfigureAwait(false);
            return declarations.SelectAsArray(t => t.Symbol);
        }
    }
}