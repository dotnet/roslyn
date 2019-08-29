// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

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
