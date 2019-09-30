// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Does a data flow analysis for state attached to local variables and fields of struct locals.
    /// </summary>
    internal abstract partial class LocalDataFlowPass<TLocalState> : AbstractFlowPass<TLocalState>
        where TLocalState : AbstractFlowPass<TLocalState>.ILocalState
    {
        /// <summary>
        /// A mapping from local variables to the index of their slot in a flow analysis local state.
        /// </summary>
        protected readonly PooledDictionary<VariableIdentifier, int> _variableSlot = PooledDictionary<VariableIdentifier, int>.GetInstance();

        /// <summary>
        /// A mapping from the local variable slot to the symbol for the local variable itself.  This
        /// is used in the implementation of region analysis (support for extract method) to compute
        /// the set of variables "always assigned" in a region of code.
        ///
        /// The first slot, slot 0, is reserved for indicating reachability, so the first tracked variable will
        /// be given slot 1. When referring to <see cref="VariableIdentifier.ContainingSlot"/>, slot 0 indicates
        /// that the variable in <see cref="VariableIdentifier.Symbol"/> is a root, i.e. not nested within another
        /// tracked variable. Slots &lt; 0 are illegal.
        /// </summary>
        protected VariableIdentifier[] variableBySlot = new VariableIdentifier[1];

        /// <summary>
        /// Variable slots are allocated to local variables sequentially and never reused.  This is
        /// the index of the next slot number to use.
        /// </summary>
        protected int nextVariableSlot = 1;

        private readonly int _maxSlotDepth;

        /// <summary>
        /// A cache for remember which structs are empty.
        /// </summary>
        protected readonly EmptyStructTypeCache _emptyStructTypeCache;

        protected LocalDataFlowPass(
            CSharpCompilation compilation,
            Symbol member,
            BoundNode node,
            EmptyStructTypeCache emptyStructs,
            bool trackUnassignments,
            int maxSlotDepth = 0)
            : base(compilation, member, node, nonMonotonicTransferFunction: trackUnassignments)
        {
            _maxSlotDepth = maxSlotDepth;
            _emptyStructTypeCache = emptyStructs;
        }

        protected LocalDataFlowPass(
            CSharpCompilation compilation,
            Symbol member,
            BoundNode node,
            EmptyStructTypeCache emptyStructs,
            BoundNode firstInRegion,
            BoundNode lastInRegion,
            bool trackRegions,
            bool trackUnassignments)
            : base(compilation, member, node, firstInRegion, lastInRegion, trackRegions: trackRegions, nonMonotonicTransferFunction: trackUnassignments)
        {
            _emptyStructTypeCache = emptyStructs;
        }

        protected override void Free()
        {
            _variableSlot.Free();
            base.Free();
        }

        /// <summary>
        /// Locals are given slots when their declarations are encountered.  We only need give slots
        /// to local variables, out parameters, and the "this" variable of a struct constructs.
        /// Other variables are not given slots, and are therefore not tracked by the analysis.  This
        /// returns -1 for a variable that is not tracked, for fields of structs that have the same
        /// assigned status as the container, and for structs that (recursively) contain no data members.
        /// We do not need to track references to
        /// variables that occur before the variable is declared, as those are reported in an
        /// earlier phase as "use before declaration". That allows us to avoid giving slots to local
        /// variables before processing their declarations.
        /// </summary>
        protected int VariableSlot(Symbol symbol, int containingSlot = 0)
        {
            containingSlot = DescendThroughTupleRestFields(ref symbol, containingSlot, forceContainingSlotsToExist: false);

            int slot;
            return (_variableSlot.TryGetValue(new VariableIdentifier(symbol, containingSlot), out slot)) ? slot : -1;
        }

        protected virtual bool IsEmptyStructType(TypeSymbol type)
        {
            return _emptyStructTypeCache.IsEmptyStructType(type);
        }

        /// <summary>
        /// Force a variable to have a slot.  Returns -1 if the variable has an empty struct type.
        /// </summary>
        protected int GetOrCreateSlot(Symbol symbol, int containingSlot = 0, bool forceSlotEvenIfEmpty = false)
        {
            Debug.Assert(containingSlot >= 0);

            if (symbol.Kind == SymbolKind.RangeVariable) return -1;

            containingSlot = DescendThroughTupleRestFields(ref symbol, containingSlot, forceContainingSlotsToExist: true);

            if (containingSlot < 0)
            {
                // Error case. Diagnostics should already have been produced.
                return -1;
            }

            VariableIdentifier identifier = new VariableIdentifier(symbol, containingSlot);
            int slot;

            // Since analysis may proceed in multiple passes, it is possible the slot is already assigned.
            if (!_variableSlot.TryGetValue(identifier, out slot))
            {
                var variableType = symbol.GetTypeOrReturnType().Type;
                if (!forceSlotEvenIfEmpty && IsEmptyStructType(variableType))
                {
                    return -1;
                }

                if (_maxSlotDepth > 0 && GetSlotDepth(containingSlot) >= _maxSlotDepth)
                {
                    return -1;
                }

                slot = nextVariableSlot++;
                _variableSlot.Add(identifier, slot);
                if (slot >= variableBySlot.Length)
                {
                    Array.Resize(ref this.variableBySlot, slot * 2);
                }

                variableBySlot[slot] = identifier;
            }

            if (IsConditionalState)
            {
                Normalize(ref this.StateWhenTrue);
                Normalize(ref this.StateWhenFalse);
            }
            else
            {
                Normalize(ref this.State);
            }

            return slot;
        }

        private int GetSlotDepth(int slot)
        {
            int depth = 0;
            while (slot > 0)
            {
                depth++;
                slot = variableBySlot[slot].ContainingSlot;
            }
            return depth;
        }

        protected abstract void Normalize(ref TLocalState state);

        /// <summary>
        /// Descends through Rest fields of a tuple if "symbol" is an extended field
        /// As a result the "symbol" will be adjusted to be the field of the innermost tuple
        /// and a corresponding containingSlot is returned.
        /// Return value -1 indicates a failure which could happen for the following reasons
        /// a) Rest field does not exist, which could happen in rare error scenarios involving broken ValueTuple types
        /// b) Rest is not tracked already and forceSlotsToExist is false (otherwise we create slots on demand)
        /// </summary>
        private int DescendThroughTupleRestFields(ref Symbol symbol, int containingSlot, bool forceContainingSlotsToExist)
        {
            var fieldSymbol = symbol as TupleFieldSymbol;
            if ((object)fieldSymbol != null)
            {
                TypeSymbol containingType = ((TupleTypeSymbol)symbol.ContainingType).UnderlyingNamedType;

                // for tuple fields the variable identifier represents the underlying field
                symbol = fieldSymbol.TupleUnderlyingField;

                // descend through Rest fields
                // force corresponding slots if do not exist
                while (!TypeSymbol.Equals(containingType, symbol.ContainingType, TypeCompareKind.ConsiderEverything2))
                {
                    var restField = containingType.GetMembers(TupleTypeSymbol.RestFieldName).FirstOrDefault() as FieldSymbol;
                    if ((object)restField == null)
                    {
                        return -1;
                    }

                    if (forceContainingSlotsToExist)
                    {
                        containingSlot = GetOrCreateSlot(restField, containingSlot);
                    }
                    else
                    {
                        if (!_variableSlot.TryGetValue(new VariableIdentifier(restField, containingSlot), out containingSlot))
                        {
                            return -1;
                        }
                    }

                    containingType = restField.Type.TupleUnderlyingTypeOrSelf();
                }
            }

            return containingSlot;
        }

        protected abstract bool TryGetReceiverAndMember(BoundExpression expr, out BoundExpression receiver, out Symbol member);

        protected Symbol GetNonMemberSymbol(int slot)
        {
            VariableIdentifier variableId = variableBySlot[slot];
            while (variableId.ContainingSlot > 0)
            {
                Debug.Assert(variableId.Symbol.Kind == SymbolKind.Field || variableId.Symbol.Kind == SymbolKind.Property || variableId.Symbol.Kind == SymbolKind.Event);
                variableId = variableBySlot[variableId.ContainingSlot];
            }
            return variableId.Symbol;
        }

        /// <summary>
        /// Return the slot for a variable, or -1 if it is not tracked (because, for example, it is an empty struct).
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected virtual int MakeSlot(BoundExpression node)
        {
            switch (node.Kind)
            {
                case BoundKind.ThisReference:
                case BoundKind.BaseReference:
                    return (object)MethodThisParameter != null ? GetOrCreateSlot(MethodThisParameter) : -1;
                case BoundKind.Local:
                    return GetOrCreateSlot(((BoundLocal)node).LocalSymbol);
                case BoundKind.Parameter:
                    return GetOrCreateSlot(((BoundParameter)node).ParameterSymbol);
                case BoundKind.RangeVariable:
                    return MakeSlot(((BoundRangeVariable)node).Value);
                case BoundKind.FieldAccess:
                case BoundKind.EventAccess:
                case BoundKind.PropertyAccess:
                    if (TryGetReceiverAndMember(node, out BoundExpression receiver, out Symbol member))
                    {
                        Debug.Assert((receiver is null) != member.RequiresInstanceReceiver());
                        return MakeMemberSlot(receiver, member);
                    }
                    break;
                case BoundKind.AssignmentOperator:
                    return MakeSlot(((BoundAssignmentOperator)node).Left);
            }
            return -1;
        }

        protected int MakeMemberSlot(BoundExpression receiverOpt, Symbol member)
        {
            int containingSlot;
            if (member.RequiresInstanceReceiver())
            {
                if (receiverOpt is null)
                {
                    return -1;
                }
                containingSlot = MakeSlot(receiverOpt);
                if (containingSlot < 0)
                {
                    return -1;
                }
            }
            else
            {
                containingSlot = 0;
            }
            return GetOrCreateSlot(member, containingSlot);
        }
    }
}
