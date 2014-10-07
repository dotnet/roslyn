using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Common;
using Microsoft.CodeAnalysis.Common.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    public struct SymbolKey
    {
        private readonly string name;
        private readonly string containingTypeName;

        public SymbolKey(ISymbol symbol)
        {
            this.name = symbol.Name;
            this.containingTypeName = symbol.ContainingType != null ? symbol.ContainingType.Name : null;
        }

        internal SymbolKey(string name, string containingTypeName = null)
        {
            this.name = name;
            this.containingTypeName = containingTypeName;
        }

        public string Name
        {
            get { return this.name; }
        }

        public string ContainingTypeName
        {
            get { return this.containingTypeName; }
        }

        public bool IsMember
        {
            get { return this.containingTypeName != null; }
        }
    }
}