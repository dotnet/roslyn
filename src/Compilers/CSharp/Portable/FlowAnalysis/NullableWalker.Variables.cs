// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class NullableWalker
    {
        /// <summary>
        /// An immutable copy of <see cref="Variables"/>.
        /// </summary>
        [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
        internal sealed class VariablesSnapshot
        {
            /// <summary>
            /// Unique identifier in the chain of nested VariablesSnapshot instances. The value starts at 0
            /// for the outermost method and increases at each nested function.
            /// </summary>
            internal readonly int Id;

            /// <summary>
            /// VariablesSnapshot instance for containing method, or null if this is the outermost method.
            /// </summary>
            internal readonly VariablesSnapshot? Container;

            /// <summary>
            /// Symbol that contains this set of variables. This is typically a method but may be a field
            /// when analyzing a field initializer. The symbol may be null at the outermost scope when
            /// analyzing an attribute argument value or a parameter default value.
            /// </summary>
            internal readonly Symbol? Symbol;

            /// <summary>
            /// Mapping from variable to slot.
            /// </summary>
            internal readonly ImmutableArray<KeyValuePair<VariableIdentifier, int>> VariableSlot;

            /// <summary>
            /// Mapping from local or parameter to inferred type.
            /// </summary>
            internal readonly ImmutableDictionary<Symbol, TypeWithAnnotations> VariableTypes;

            internal VariablesSnapshot(int id, VariablesSnapshot? container, Symbol? symbol, ImmutableArray<KeyValuePair<VariableIdentifier, int>> variableSlot, ImmutableDictionary<Symbol, TypeWithAnnotations> variableTypes)
            {
                Id = id;
                Container = container;
                Symbol = symbol;
                VariableSlot = variableSlot;
                VariableTypes = variableTypes;
            }

            internal bool TryGetType(Symbol symbol, out TypeWithAnnotations type)
            {
                return VariableTypes.TryGetValue(symbol, out type);
            }

            private string GetDebuggerDisplay()
            {
                var symbol = (object?)Symbol ?? "<null>";
                return $"Id={Id}, Symbol={symbol}, Count={VariableSlot.Length}";
            }
        }

        /// <summary>
        /// A collection of variables associated with a method scope. For a particular method, the variables
        /// may contain parameters and locals and any fields from other variables in the collection. If the method
        /// is a nested function (a lambda or a local function), there is a reference to the variables collection at
        /// the containing method scope. The outermost scope may also contain variables for static fields.
        /// Each variable (parameter, local, or field of other variable) must be associated with the variables collection
        /// for that method where the parameter or local are declared, even if the variable is used in a nested scope.
        /// </summary>
        [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
        internal sealed class Variables
        {
            // Members of variables are tracked up to a fixed depth, to avoid cycles. The
            // MaxSlotDepth value is arbitrary but large enough to allow most scenarios.
            private const int MaxSlotDepth = 5;

            // An int slot is a combination of a 15-bit id (in the high-order 16 bits) and a 16-bit index.
            // Id value starts at 0 for the outermost method and increases at each nested function.
            // There is no relationship between ids of sibling nested functions - the ids of sibling
            // functions may be the same or different.
            private const int IdOffset = 16;
            private const int IdMask = (1 << 15) - 1;
            private const int IndexMask = (1 << 16) - 1;

#if DEBUG
            /// <summary>
            /// Used to offset child ids to help catch cases where Variables
            /// and LocalState instances are mismatched.
            /// </summary>
            private readonly Random _nextIdOffset;
#endif

            /// <summary>
            /// Unique identifier in the chain of nested Variables instances. The value starts at 0
            /// for the outermost method and increases at each nested function.
            /// </summary>
            internal readonly int Id;

            /// <summary>
            /// Variables instance for containing method, or null if this is the outermost method.
            /// </summary>
            internal readonly Variables? Container;

            /// <summary>
            /// Symbol that contains this set of variables. This is typically a method but may be a field
            /// when analyzing a field initializer. The symbol may be null at the outermost scope when
            /// analyzing an attribute argument value or a parameter default value.
            /// </summary>
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
            /// A mapping from the local variable slot to the symbol for the local variable itself.
            ///
            /// The first slot, slot 0, is reserved for indicating reachability, so the first tracked variable will
            /// be given slot 1. When referring to VariableIdentifier.ContainingSlot, slot 0 indicates
            /// that the variable in VariableIdentifier.Symbol is a root, i.e. not nested within another
            /// tracked variable. Slots less than 0 are illegal.
            /// </summary>
            private readonly ArrayBuilder<VariableIdentifier> _variableBySlot = ArrayBuilder<VariableIdentifier>.GetInstance(1, default);

            internal static Variables Create(Symbol? symbol)
            {
                return new Variables(id: 0, container: null, symbol);
            }

            internal static Variables Create(VariablesSnapshot snapshot)
            {
                var container = snapshot.Container is null ? null : Create(snapshot.Container);
                var variables = new Variables(snapshot.Id, container, snapshot.Symbol);
                variables.Populate(snapshot);
                return variables;
            }

            private int GetNextId()
            {
                return Id +
#if DEBUG
                    _nextIdOffset.Next(maxValue: 7) +
#endif
                    1;
            }

            private void Populate(VariablesSnapshot snapshot)
            {
                Debug.Assert(_variableSlot.Count == 0);
                Debug.Assert(_variableTypes.Count == 0);
                Debug.Assert(_variableBySlot.Count == 1);

                _variableBySlot.AddMany(default, snapshot.VariableSlot.Length);
                foreach (var pair in snapshot.VariableSlot)
                {
                    var identifier = pair.Key;
                    var index = pair.Value;
                    _variableSlot.Add(identifier, index);
                    _variableBySlot[index] = identifier;
                }

                foreach (var pair in snapshot.VariableTypes)
                {
                    _variableTypes.Add(pair.Key, pair.Value);
                }
            }

            private Variables(int id, Variables? container, Symbol? symbol)
            {
                Debug.Assert(id >= 0);
                Debug.Assert(id <= IdMask);
                Debug.Assert(container is null || container.Id < id);
#if DEBUG
                _nextIdOffset = container?._nextIdOffset ?? new Random();
#endif
                Id = id;
                Container = container;
                Symbol = symbol;
            }

            internal void Free()
            {
                Container?.Free();
                _variableBySlot.Free();
                _variableTypes.Free();
                _variableSlot.Free();
            }

            internal VariablesSnapshot CreateSnapshot()
            {
                return new VariablesSnapshot(
                    Id,
                    Container?.CreateSnapshot(),
                    Symbol,
                    ImmutableArray.CreateRange(_variableSlot),
                    ImmutableDictionary.CreateRange(_variableTypes));
            }

            internal Variables CreateNestedMethodScope(MethodSymbol method)
            {
                // <Metalama> Disabled because it fails in debug build on our code.
                // Debug.Assert(GetVariablesForMethodScope(method) is null);
                // Debug.Assert(!(method.ContainingSymbol is MethodSymbol containingMethod) ||
                //     ((object?)GetVariablesForMethodScope(containingMethod) == this) ||
                //     Container is null);
                // </Metalama>

                return new Variables(id: GetNextId(), this, method);
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
                var variables = GetVariablesForVariable(identifier);
                int slot = variables.AddInternal(identifier);
                // ContainingSlot must be from the same Variables collection.
                Debug.Assert(slot <= 0 ||
                    identifier.ContainingSlot <= 0 ||
                    DeconstructSlot(slot).Id == DeconstructSlot(identifier.ContainingSlot).Id);
                return slot;
            }

            private int AddInternal(VariableIdentifier identifier)
            {
                if (getSlotDepth(identifier.ContainingSlot) >= MaxSlotDepth)
                {
                    return -1;
                }
                int index = NextAvailableIndex;
                if (index > IndexMask)
                {
                    return -1;
                }
                _variableSlot.Add(identifier, index);
                _variableBySlot.Add(identifier);
                return ConstructSlot(Id, index);

                int getSlotDepth(int slot)
                {
                    int depth = 0;
                    while (slot > 0)
                    {
                        depth++;
                        var (id, index) = DeconstructSlot(slot);
                        Debug.Assert(id == Id);
                        slot = _variableBySlot[index].ContainingSlot;
                    }
                    return depth;
                }
            }

            internal bool TryGetType(Symbol symbol, out TypeWithAnnotations type)
            {
                var variables = GetVariablesContainingSymbol(symbol);
                return variables._variableTypes.TryGetValue(symbol, out type);
            }

            internal void SetType(Symbol symbol, TypeWithAnnotations type)
            {
                var variables = GetVariablesContainingSymbol(symbol);
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

            internal int NextAvailableIndex => _variableBySlot.Count;

            internal int GetTotalVariableCount()
            {
                int fromContainer = Container?.GetTotalVariableCount() ?? 0;
                return fromContainer + _variableSlot.Count;
            }

            internal void GetMembers(ArrayBuilder<(VariableIdentifier, int)> builder, int containingSlot)
            {
                (int id, int index) = DeconstructSlot(containingSlot);
                var variables = GetVariablesForId(id)!;
                var variableBySlot = variables._variableBySlot;
                for (index++; index < variableBySlot.Count; index++)
                {
                    var variable = variableBySlot[index];
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
                return GetVariablesContainingSymbol(identifier.Symbol);
            }

            private Variables GetVariablesContainingSymbol(Symbol symbol)
            {
                switch (symbol)
                {
                    case LocalSymbol:
                    case ParameterSymbol:
                        if (symbol.ContainingSymbol is MethodSymbol method &&
                            GetVariablesForMethodScope(method) is { } variables)
                        {
                            return variables;
                        }
                        break;
                }
                // Fallback to the outermost scope for the remaining cases. Those cases include: static fields;
                // variables declared in field initializers; locals and parameters when the root symbol is null;
                // and error cases such as an instance field referenced in a static method (no containing slot).
                return GetRootScope();
            }

            internal Variables GetRootScope()
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

            internal Variables? GetVariablesForMethodScope(MethodSymbol method)
            {
                // https://github.com/dotnet/roslyn/issues/73772: is this needed if we also delete the weird cascading in EnterParameters?
                method = method.PartialImplementationPart ?? method;
                var variables = this;
                while (true)
                {
                    if ((object)method == variables.Symbol)
                    {
                        return variables;
                    }
                    variables = variables.Container;
                    if (variables is null)
                    {
                        return null;
                    }
                }
            }

            internal static int ConstructSlot(int id, int index)
            {
                Debug.Assert(id >= 0);
                Debug.Assert(id <= IdMask);
                Debug.Assert(index >= 0);
                Debug.Assert(index <= IndexMask);

                return index < 0 ? index : (id << IdOffset) | index;
            }

            internal static (int Id, int Index) DeconstructSlot(int slot)
            {
                Debug.Assert(slot > -1);
                return slot < 0 ? (0, slot) : (slot >> IdOffset & IdMask, slot & IndexMask);
            }

            private string GetDebuggerDisplay()
            {
                var symbol = (object?)Symbol ?? "<null>";
                return $"Id={Id}, Symbol={symbol}, Count={_variableSlot.Count}";
            }
        }
    }
}
