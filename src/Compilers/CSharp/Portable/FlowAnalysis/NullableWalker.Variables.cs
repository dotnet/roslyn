// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class NullableWalker
    {
        [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
        internal sealed class Variables
        {
            private sealed class Factory
            {
                internal int NextId;
                internal int MaxSlotDepth;

                internal Factory(int maxSlotDepth)
                {
                    MaxSlotDepth = maxSlotDepth;
                }
            }

            private const int IdOffset = 16;
            private const int IdOrIndexMask = (1 << IdOffset) - 1;
            private const int IndexMax = IdOrIndexMask;

            private readonly Factory _factory;
            internal readonly int Id;
            internal readonly Variables? Container;
            internal readonly Symbol? Symbol;

            /// <summary>
            /// A mapping from local variables to the index of their slot in a flow analysis local state.
            /// </summary>
            private readonly PooledDictionary<VariableIdentifier, int> _variableSlot = PooledDictionary<VariableIdentifier, int>.GetInstance();

            /// <summary>
            /// The inferred type at the point of declaration of var locals and parameters.
            /// </summary>
            private readonly PooledDictionary<Symbol, TypeWithAnnotations> _variableTypes = SpecializedSymbolCollections.GetPooledSymbolDictionaryInstance<Symbol, TypeWithAnnotations>();

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

            /// <summary>
            /// Variable slots are allocated to local variables sequentially and never reused.  This is
            /// the index of the next slot number to use.
            /// </summary>
            private int _nextVariableIndex = 1;

            internal static Variables Create(Symbol? symbol, int maxSlotDepth)
            {
                return new Variables(new Factory(maxSlotDepth), container: null, symbol);
            }

            private Variables(Factory factory, Variables? container, Symbol? symbol)
            {
                Debug.Assert(container is null || container.Id < factory.NextId);
                _factory = factory;
                // PROTOTYPE: Handle > 64K nested methods (distinct ids). See NullableStateTooManyNestedFunctions().
                Id = _factory.NextId++;
                Container = container;
                Symbol = symbol;
                _variableBySlot = ArrayBuilder<VariableIdentifier>.GetInstance();
                _variableBySlot.Add(default); // slot 0 reserved for reachability
            }

            internal void Free()
            {
                // PROTOTYPE:
#if false
                Container?.Free();
                _variableBySlot.Free();
                _variableTypes.Free();
                _variableSlot.Free();
#endif
            }

            internal Variables CreateNestedFunction(MethodSymbol method)
            {
                Debug.Assert(GetVariablesForSymbol(method) is null or { Symbol: null });
                return new Variables(_factory, this, method);
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

            internal bool TryGetValue(VariableIdentifier identifier, out int slot)
            {
                var variables = GetVariablesForVariable(identifier);
                return variables.TryGetValueInternal(identifier, out slot);
            }

            private bool TryGetValueInternal(VariableIdentifier identifier, out int slot)
            {
                if (_variableSlot.TryGetValue(identifier, out int index))
                {
                    slot = ConstructSlot(Id, index);
                    return true;
                }
                slot = -1;
                return false;
            }

            internal int Add(VariableIdentifier identifier)
            {
                if (_nextVariableIndex > IndexMax)
                {
                    return -1;
                }
                if (GetSlotDepth(identifier.ContainingSlot) >= _factory.MaxSlotDepth)
                {
                    return -1;
                }
                var variables = GetVariablesForVariable(identifier);
                return variables.AddInternal(identifier);
            }

            private int AddInternal(VariableIdentifier identifier)
            {
                int index = _nextVariableIndex++;
                _variableSlot.Add(identifier, index);
                while (index >= _variableBySlot.Count)
                {
                    _variableBySlot.Add(default);
                }
                _variableBySlot[index] = identifier;
                return ConstructSlot(Id, index);
            }

            internal bool TryGetType(Symbol symbol, out TypeWithAnnotations type)
            {
                var variables = GetVariablesContainingVariable(symbol);
                return variables._variableTypes.TryGetValue(symbol, out type);
            }

            internal void SetType(Symbol symbol, TypeWithAnnotations type)
            {
                var variables = GetVariablesContainingVariable(symbol);
                Debug.Assert((object)variables == this);
                variables._variableTypes[symbol] = type;
            }

            internal VariableIdentifier this[int slot]
            {
                get
                {
                    (int id, int index) = DeconstructSlot(slot);
                    var variables = GetVariablesForId(id);
                    return variables!._variableBySlot[index];
                }
            }

            internal int Count => _nextVariableIndex;

            internal void GetMembers(ArrayBuilder<(VariableIdentifier, int)> builder, int containingSlot)
            {
                (int id, int index) = DeconstructSlot(containingSlot);
                var variables = GetVariablesForId(id)!;

                for (index++; index < variables.Count; index++)
                {
                    var variable = variables._variableBySlot[index];
                    if (variable.ContainingSlot == containingSlot)
                    {
                        builder.Add((variable, ConstructSlot(id, index)));
                    }
                }
            }

            private Variables GetVariablesForVariable(VariableIdentifier identifier)
            {
                int containingSlot = identifier.ContainingSlot;
                if (containingSlot > 0)
                {
                    return GetVariablesForId(DeconstructSlot(containingSlot).Id)!;
                }
                return GetVariablesContainingVariable(identifier.Symbol);
            }

            internal Variables GetVariablesContainingVariable(Symbol symbol)
            {
                switch (symbol)
                {
                    case LocalSymbol:
                    case ParameterSymbol:
                    case MethodSymbol { MethodKind: MethodKind.LocalFunction }:
                        if (symbol.ContainingSymbol is MethodSymbol method &&
                            GetVariablesForSymbol(method.PartialImplementationPart ?? method) is { } variables)
                        {
                            return variables;
                        }
                        break;
                }
                return GetVariablesRoot();
            }

            private Variables GetVariablesRoot()
            {
                var variables = this;
                while (variables.Container is { } container)
                {
                    variables = container;
                }
                return variables;
            }

            private Variables? GetVariablesForId(int id)
            {
                var variables = this;
                do
                {
                    if (variables.Id == id)
                    {
                        return variables;
                    }
                    variables = variables.Container;
                }
                while (variables is { });
                return null;
            }

            private Variables? GetVariablesForSymbol(MethodSymbol symbol)
            {
                var variables = this;
                while (true)
                {
                    if (variables.Symbol is null || (object)symbol == variables.Symbol)
                    {
                        return variables;
                    }
                    variables = variables.Container!;
                    if (variables is null)
                    {
                        return null;
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

            internal static int ConstructSlot(int id, int index)
            {
                return index < 0 ? index : (id << IdOffset) + index;
            }

            internal static (int Id, int Index) DeconstructSlot(int slot)
            {
                Debug.Assert(slot > -1);
                return slot < 0 ? (0, slot) : ((slot >> IdOffset & IdOrIndexMask), slot & IdOrIndexMask);
            }

            internal string GetDebuggerDisplay()
            {
                var symbol = (object?)Symbol ?? "<null>";
                return $"Id={Id}, Symbol={symbol}, Count={Count}";
            }
        }
    }
}
