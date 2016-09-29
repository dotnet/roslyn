// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class DataFlowPass
    {
        protected struct VariableIdentifier : IEquatable<VariableIdentifier>
        {
            public readonly Symbol Symbol;
            public readonly int ContainingSlot;

            public VariableIdentifier(Symbol symbol, int containingSlot = 0)
            {
                Debug.Assert(symbol.Kind == SymbolKind.Local || symbol.Kind == SymbolKind.Field || symbol.Kind == SymbolKind.Parameter ||
                    (symbol as MethodSymbol)?.MethodKind == MethodKind.LocalFunction);
                Symbol = symbol;
                ContainingSlot = containingSlot;
            }

            public bool Exists
            {
                get { return (object)Symbol != null; }
            }
            public override int GetHashCode()
            {
                return Roslyn.Utilities.Hash.Combine(Symbol, ContainingSlot);
            }
            public bool Equals(VariableIdentifier other)
            {
                return ((object)Symbol == null ? (object)other.Symbol == null : Symbol.Equals(other.Symbol)) && ContainingSlot == other.ContainingSlot;
            }
            public override bool Equals(object obj)
            {
                VariableIdentifier? other = obj as VariableIdentifier?;
                return other.HasValue && Equals(other.Value);
            }
            public static bool operator ==(VariableIdentifier left, VariableIdentifier right)
            {
                return left.Equals(right);
            }
            public static bool operator !=(VariableIdentifier left, VariableIdentifier right)
            {
                return !left.Equals(right);
            }
        }
    }
}
