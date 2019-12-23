// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal partial class TypeSymbol
    {
        /// <summary>
        /// Represents the method by which this type implements a given interface type
        /// and/or the corresponding diagnostics.
        /// </summary>
        protected class SymbolAndDiagnostics
        {
            public static readonly SymbolAndDiagnostics Empty = new SymbolAndDiagnostics(null, ImmutableBindingDiagnostic<AssemblySymbol>.Empty);

            public readonly Symbol Symbol;
            public readonly ImmutableBindingDiagnostic<AssemblySymbol> Diagnostics;

            public SymbolAndDiagnostics(Symbol symbol, ImmutableBindingDiagnostic<AssemblySymbol> diagnostics)
            {
                this.Symbol = symbol;
                this.Diagnostics = diagnostics;
            }
        }
    }
}
