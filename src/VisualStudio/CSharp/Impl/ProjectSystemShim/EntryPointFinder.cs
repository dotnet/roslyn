// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim
{
    internal class EntryPointFinder : AbstractEntryPointFinder
    {
        protected override bool MatchesMainMethodName(string name)
        {
            return name == "Main";
        }

        public static IEnumerable<INamedTypeSymbol> FindEntryPoints(INamespaceSymbol symbol)
        {
            var visitor = new EntryPointFinder();
            visitor.Visit(symbol);
            return visitor.EntryPoints;
        }
    }
}
