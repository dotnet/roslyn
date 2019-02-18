// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class LocalDataFlowPass<TLocalState>
    {
        internal struct VariableIdentifier : IEquatable<VariableIdentifier>
        {
            public readonly Symbol Symbol;
            public readonly int ContainingSlot;

            public VariableIdentifier(Symbol symbol, int containingSlot = 0)
            {
                Debug.Assert(symbol.Kind == SymbolKind.Local || symbol.Kind == SymbolKind.Field || symbol.Kind == SymbolKind.Parameter ||
                    (symbol as MethodSymbol)?.MethodKind == MethodKind.LocalFunction ||
                    symbol.Kind == SymbolKind.Property || symbol.Kind == SymbolKind.Event);
                Symbol = symbol;
                ContainingSlot = containingSlot;
            }

            public bool Exists
            {
                get { Debug.Assert((object)Symbol != null);  return (object)Symbol != null; }
            }

            public override int GetHashCode()
            {
                Debug.Assert((object)Symbol != null);

                int currentKey = ContainingSlot;
                int thisIndex = Symbol.MemberIndex;
                return (thisIndex < 0) ?
                    Hash.Combine(Symbol.OriginalDefinition, currentKey) :
                    Hash.Combine(thisIndex, currentKey);
            }

            public bool Equals(VariableIdentifier other)
            {
                Debug.Assert((object)Symbol != null);
                Debug.Assert((object)other.Symbol != null);

                if (ContainingSlot != other.ContainingSlot)
                {
                    return false;
                }

                int thisIndex = Symbol.MemberIndex;
                int otherIndex = other.Symbol.MemberIndex;
                if (thisIndex != otherIndex)
                {
                    return false;
                }

                if (thisIndex < 0)
                {
                    return Symbol.OriginalDefinition.Equals(other.Symbol.OriginalDefinition);
                }

                return true;
            }

            public override bool Equals(object obj)
            {
                throw ExceptionUtilities.Unreachable;
            }

            public static bool operator ==(VariableIdentifier left, VariableIdentifier right)
            {
                throw ExceptionUtilities.Unreachable;
            }

            public static bool operator !=(VariableIdentifier left, VariableIdentifier right)
            {
                throw ExceptionUtilities.Unreachable;
            }

            public override string ToString()
            {
                return $"ContainingSlot={ContainingSlot}, Symbol={Symbol.GetDebuggerDisplay()}";
            }
        }
    }
}
