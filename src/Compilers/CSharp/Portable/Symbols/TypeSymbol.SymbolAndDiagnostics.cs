// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;

#pragma warning disable CS0660 // Warning is reported only for Full Solution Analysis

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
