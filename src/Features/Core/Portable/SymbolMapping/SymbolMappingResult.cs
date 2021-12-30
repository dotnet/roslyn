// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SymbolMapping
{
    internal class SymbolMappingResult
    {
        public Project Project { get; }
        public ISymbol Symbol { get; }

        internal SymbolMappingResult(Project project, ISymbol symbol)
        {
            Contract.ThrowIfNull(project);
            Contract.ThrowIfNull(symbol);

            Project = project;
            Symbol = symbol;
        }

        public Solution Solution => Project.Solution;
    }
}
