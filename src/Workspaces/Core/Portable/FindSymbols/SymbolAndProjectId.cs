// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal struct SymbolAndProjectId
    {
        public readonly ISymbol Symbol;
        public readonly ProjectId ProjectId;

        public SymbolAndProjectId(ISymbol symbol, ProjectId projectId)
        {
            Symbol = symbol;
            ProjectId = projectId;
        }

        public static SymbolAndProjectId<TSymbol> Create<TSymbol>(
            TSymbol symbol, ProjectId projectId) where TSymbol : ISymbol
        {
            return new SymbolAndProjectId<TSymbol>(symbol, projectId);
        }
    }

    internal struct SymbolAndProjectId<TSymbol> where TSymbol : ISymbol
    {
        public readonly TSymbol Symbol;
        public readonly ProjectId ProjectId;

        public SymbolAndProjectId(TSymbol symbol, ProjectId projectId)
        {
            Symbol = symbol;
            ProjectId = projectId;
        }

        public static implicit operator SymbolAndProjectId(SymbolAndProjectId<TSymbol> value)
        {
            return new SymbolAndProjectId(value.Symbol, value.ProjectId);
        }
    }
}