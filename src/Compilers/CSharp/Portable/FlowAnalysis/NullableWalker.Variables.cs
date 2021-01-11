// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class NullableWalker
    {
        internal sealed class Variables
        {
            // Members of variables are tracked up to a fixed depth, to avoid cycles. The
            // _maxSlotDepth value is arbitrary but large enough to allow most scenarios.
            private const int maxSlotDepth = 5;

            /// <summary>
            /// A mapping from local variables to the index of their slot in a flow analysis local state.
            /// </summary>
            private readonly Dictionary<VariableIdentifier, int> _variableSlot;

            /// <summary>
            /// The inferred type at the point of declaration of var locals and parameters.
            /// </summary>
            private readonly Dictionary<Symbol, TypeWithAnnotations> _variableTypes;

            /// <summary>
            /// A mapping from the local variable slot to the symbol for the local variable itself.  This
            /// is used in the implementation of region analysis (support for extract method) to compute
            /// the set of variables "always assigned" in a region of code.
            ///
            /// The first slot, slot 0, is reserved for indicating reachability, so the first tracked variable will
            /// be given slot 1. When referring to VariableIdentifier.ContainingSlot, slot 0 indicates
            /// that the variable in VariableIdentifier.Symbol is a root, i.e. not nested within another
            /// tracked variable. Slots &lt; 0 are illegal.
            /// </summary>
            private readonly ArrayBuilder<VariableIdentifier> _variableBySlot;

            internal Variables()
            {
                _variableSlot = new Dictionary<VariableIdentifier, int>();
                _variableTypes = new Dictionary<Symbol, TypeWithAnnotations>(Microsoft.CodeAnalysis.CSharp.Symbols.SymbolEqualityComparer.ConsiderEverything);
                _variableBySlot = new ArrayBuilder<VariableIdentifier>();
                _variableBySlot.Add(default); // slot 0 reserved for reachability
            }

            internal bool TryGetValue(VariableIdentifier identifier, out int slot)
            {
                if (_variableSlot.TryGetValue(identifier, out slot))
                {
                    return true;
                }
                slot = -1;
                return false;
            }

            internal int Add(VariableIdentifier identifier)
            {
                if (GetSlotDepth(identifier.ContainingSlot) >= maxSlotDepth)
                {
                    return -1;
                }

                int slot = _variableBySlot.Count;
                _variableSlot.Add(identifier, slot);
                _variableBySlot.Add(identifier);

                Debug.Assert(_variableBySlot.Count == _variableSlot.Count + 1);
                return slot;
            }

            internal bool TryGetType(Symbol symbol, out TypeWithAnnotations type)
            {
                return _variableTypes.TryGetValue(symbol, out type);
            }

            internal void SetType(Symbol symbol, TypeWithAnnotations type)
            {
                _variableTypes[symbol] = type;
            }

            internal VariableIdentifier this[int slot] => _variableBySlot[slot];

            internal int Count => _variableBySlot.Count;

            internal void Save(ArrayBuilder<(VariableIdentifier, int)> variableSlot, ArrayBuilder<(Symbol, TypeWithAnnotations)> variableTypes)
            {
                Debug.Assert(_variableBySlot.Count == _variableSlot.Count + 1);
                foreach (var pair in _variableSlot)
                {
                    variableSlot.Add((pair.Key, pair.Value));
                }
                foreach (var pair in _variableTypes)
                {
                    variableTypes.Add((pair.Key, pair.Value));
                }
            }

            internal void Restore(ArrayBuilder<(VariableIdentifier, int)> variableSlot, ArrayBuilder<(Symbol, TypeWithAnnotations)> variableTypes)
            {
                Debug.Assert(variableSlot.Count <= _variableSlot.Count);
                Debug.Assert(variableSlot.All(pair => _variableSlot[pair.Item1] == pair.Item2));
                Debug.Assert(variableTypes.All(pair => _variableTypes[pair.Item1] == pair.Item2));

                _variableSlot.Clear();
                foreach (var (key, value) in variableSlot)
                {
                    _variableSlot.Add(key, value);
                }
                _variableTypes.Clear();
                foreach (var (key, value) in variableTypes)
                {
                    _variableTypes.Add(key, value);
                }
                _variableBySlot.Clip(variableSlot.Count + 1); // slot 0 reserved for reachability
            }

            internal int RootSlot(int slot)
            {
                while (true)
                {
                    int containingSlot = this[slot].ContainingSlot;
                    if (containingSlot == 0)
                    {
                        return slot;
                    }
                    else
                    {
                        slot = containingSlot;
                    }
                }
            }

            private int GetSlotDepth(int slot)
            {
                int depth = 0;
                while (slot > 0)
                {
                    depth++;
                    slot = this[slot].ContainingSlot;
                }
                return depth;
            }
        }
    }
}
