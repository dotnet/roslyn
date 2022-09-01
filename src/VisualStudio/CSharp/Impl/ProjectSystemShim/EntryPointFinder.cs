// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim
{
    internal class EntryPointFinder
    {
        [Obsolete("FindEntryPoints on a INamespaceSymbol is deprecated, please pass in the Compilation instead.")]
        public static IEnumerable<INamedTypeSymbol> FindEntryPoints(INamespaceSymbol symbol)
        {
            return FindEntryPoints(symbol.ContainingCompilation!);
        }

        public static IEnumerable<INamedTypeSymbol> FindEntryPoints(Compilation compilation)
            => compilation.GetEntryPointCandidates(default)
                .SelectAsArray(static x => x.ContainingSymbol as INamedTypeSymbol)
                .WhereNotNull();
    }
}
