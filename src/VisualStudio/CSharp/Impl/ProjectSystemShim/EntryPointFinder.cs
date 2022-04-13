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
            // This differs from the VB implementation (Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim.EntryPointFinder)
            // because we don't ever consider forms entry points.
            // Techinically, this is wrong but it just doesn't matter since the
            // ref assemblies are unlikely to have a random Main() method that matches
            var visitor = new EntryPointFinder();
            visitor.Visit(symbol);
            return visitor.EntryPoints;
        }
    }
}
