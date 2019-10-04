// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Immutable;

#pragma warning disable CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)

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
            public static readonly SymbolAndDiagnostics Empty = new SymbolAndDiagnostics(null, ImmutableArray<Diagnostic>.Empty);

            public readonly Symbol? Symbol;
            public readonly ImmutableArray<Diagnostic> Diagnostics;

            public SymbolAndDiagnostics(Symbol? symbol, ImmutableArray<Diagnostic> diagnostics)
            {
                this.Symbol = symbol;
                this.Diagnostics = diagnostics;
            }
        }
    }
}
