using System;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CodeFixes.AddImport
{
    internal abstract partial class AbstractAddImportCodeFixProvider
    {
        private struct SymbolReference : IComparable<SymbolReference>
        {
            public readonly INamespaceOrTypeSymbol Symbol;
            public readonly ProjectId ProjectId;

            public SymbolReference(INamespaceOrTypeSymbol symbol, ProjectId projectId)
            {
                Symbol = symbol;
                ProjectId = projectId;
            }

            public int CompareTo(SymbolReference other)
            {
                return INamespaceOrTypeSymbolExtensions.CompareNamespaceOrTypeSymbols(this.Symbol, other.Symbol);
            }
        }
    }
}
