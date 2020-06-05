// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim
{
    internal class EntryPointFinder : AbstractEntryPointFinder
    {
        protected override bool MatchesMainMethodName(string name)
            => name == "Main";

        public static IEnumerable<INamedTypeSymbol> FindEntryPoints(INamespaceSymbol symbol)
        {
            var visitor = new EntryPointFinder();
            visitor.Visit(symbol);
            return visitor.EntryPoints;
        }
    }
}
