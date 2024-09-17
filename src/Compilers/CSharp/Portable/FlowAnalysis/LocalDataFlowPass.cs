// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Does a data flow analysis for state attached to local variables and fields of struct locals.
    /// </summary>
    internal abstract partial class LocalDataFlowPass<TLocalState, TLocalFunctionState> : AbstractFlowPass<TLocalState, TLocalFunctionState>
        where TLocalState : LocalDataFlowPass<TLocalState, TLocalFunctionState>.ILocalDataFlowState
        where TLocalFunctionState : AbstractFlowPass<TLocalState, TLocalFunctionState>.AbstractLocalFunctionState
    {
        internal interface ILocalDataFlowState : ILocalState
        {
            /// <summary>
            /// True if new variables introduced in <see cref="AbstractFlowPass{TLocalState, TLocalFunctionState}" /> should be set
            /// to the bottom state. False if they should be set to the top state.
            /// </summary>
            bool NormalizeToBottom { get; }
        }

        /// <summary>
        /// A cache for remember which structs are empty.
        /// </summary>
        protected readonly EmptyStructTypeCache _emptyStructTypeCache;

        protected LocalDataFlowPass(
            CSharpCompilation compilation,
            Symbol? member,
            BoundNode node,
            EmptyStructTypeCache emptyStructs,
            bool trackUnassignments)
            : base(compilation, member, node, nonMonotonicTransferFunction: trackUnassignments)
        {
            Debug.Assert(emptyStructs != null);
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

        protected abstract bool TryGetVariable(VariableIdentifier identifier, out int slot);

        protected abstract int AddVariable(VariableIdentifier identifier);

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
            // Skip LocalStoreTracker from data flow analysis.
            // The variable is declared by synthesized instrumentation code in every instrumented method (including async, iterators and lambdas).
            // It is of a ref-struct type, which is normally not allowed to be used in some of these methods, but is designed to not be lifted to
            // a closure or state machine field and only directly accessed from the frame it is declared in.
            if (symbol is LocalSymbol { SynthesizedKind: SynthesizedLocalKind.LocalStoreTracker })
            {
                return -1;
            }

            containingSlot = DescendThroughTupleRestFields(ref symbol, containingSlot, forceContainingSlotsToExist: false);

            int slot;
            return TryGetVariable(new VariableIdentifier(symbol, containingSlot), out slot) ? slot : -1;
        }

        protected virtual bool IsEmptyStructType(TypeSymbol type)
        {
            return _emptyStructTypeCache.IsEmptyStructType(type);
        }

        /// <summary>
        /// Force a variable to have a slot.  Returns -1 if the variable has an empty struct type.
        /// </summary>
        protected virtual int GetOrCreateSlot(Symbol symbol, int containingSlot = 0, bool forceSlotEvenIfEmpty = false, bool createIfMissing = true)
        {
            Debug.Assert(containingSlot >= 0);
            Debug.Assert(symbol != null);

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
            if (!TryGetVariable(identifier, out slot))
            {
                if (!createIfMissing)
                {
                    return -1;
                }

                var variableType = symbol.GetTypeOrReturnType().Type;
                if (!forceSlotEvenIfEmpty && IsEmptyStructType(variableType))
                {
                    return -1;
                }

                slot = AddVariable(identifier);
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

        /// <summary>
        /// Sets the starting state for any newly declared variables in the LocalDataFlowPass.
        /// </summary>
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
            if (symbol is TupleElementFieldSymbol fieldSymbol)
            {
                TypeSymbol containingType = symbol.ContainingType;

                // for tuple fields the variable identifier represents the underlying field
                symbol = fieldSymbol.TupleUnderlyingField;

                // descend through Rest fields
                // force corresponding slots if do not exist
                while (!TypeSymbol.Equals(containingType, symbol.ContainingType, TypeCompareKind.ConsiderEverything))
                {
                    var restField = containingType.GetMembers(NamedTypeSymbol.ValueTupleRestFieldName).FirstOrDefault(s => s is not TupleVirtualElementFieldSymbol) as FieldSymbol;
                    if (restField is null)
                    {
                        return -1;
                    }

                    if (forceContainingSlotsToExist)
                    {
                        containingSlot = GetOrCreateSlot(restField, containingSlot);

                        if (containingSlot < 0)
                        {
                            return -1;
                        }
                    }
                    else
                    {
                        if (!TryGetVariable(new VariableIdentifier(restField, containingSlot), out containingSlot))
                        {
                            return -1;
                        }
                    }

                    containingType = restField.Type;
                }
            }

            return containingSlot;
        }

        protected abstract bool TryGetReceiverAndMember(BoundExpression expr, out BoundExpression? receiver, [NotNullWhen(true)] out Symbol? member, bool useAsLvalue = false);

        /// <summary>
        /// Return the slot for a variable, or -1 if it is not tracked (because, for example, it is an empty struct).
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected virtual int MakeSlot(BoundExpression node, bool useAsLvalue = false)
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
                    if (TryGetReceiverAndMember(node, out BoundExpression? receiver, out Symbol? member, useAsLvalue))
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

        protected int MakeMemberSlot(BoundExpression? receiverOpt, Symbol member)
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

        protected static bool HasInitializer(Symbol field) => field switch
        {
            SourceMemberFieldSymbol f => f.HasInitializer,
            SynthesizedBackingFieldSymbolBase f => f.HasInitializer,
            SourceFieldLikeEventSymbol e => e.AssociatedEventField?.HasInitializer == true,
            _ => false
        };
    }
}
