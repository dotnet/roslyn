// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class DataFlowPass
    {
        protected struct VariableIdentifier : IEquatable<VariableIdentifier>
        {
            public static VariableIdentifier None = new VariableIdentifier(null, 0);

            public readonly Symbol Symbol;
            public readonly int ContainingSlot;

            public VariableIdentifier(Symbol symbol, int containingSlot = 0)
            {
                Debug.Assert((object)symbol == null || symbol.Kind == SymbolKind.Local || symbol.Kind == SymbolKind.Field || symbol.Kind == SymbolKind.Parameter);
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