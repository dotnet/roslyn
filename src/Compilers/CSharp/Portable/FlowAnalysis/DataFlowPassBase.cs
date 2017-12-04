// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract partial class DataFlowPassBase<TLocalState> : AbstractFlowPass<TLocalState>
        where TLocalState : PreciseAbstractFlowPass<TLocalState>.AbstractLocalState
    {
        /// <summary>
        /// A mapping from local variables to the index of their slot in a flow analysis local state.
        /// </summary>
        private readonly PooledDictionary<VariableIdentifier, int> _variableSlot = PooledDictionary<VariableIdentifier, int>.GetInstance();

        /// <summary>
        /// A mapping from the local variable slot to the symbol for the local variable itself.  This
        /// is used in the implementation of region analysis (support for extract method) to compute
        /// the set of variables "always assigned" in a region of code.
        /// </summary>
        protected VariableIdentifier[] variableBySlot = new VariableIdentifier[1];

        /// <summary>
        /// Variable slots are allocated to local variables sequentially and never reused.  This is
        /// the index of the next slot number to use.
        /// </summary>
        protected int nextVariableSlot = 1;

        /// <summary>
        /// A cache for remember which structs are empty.
        /// </summary>
        protected readonly EmptyStructTypeCache _emptyStructTypeCache;

        protected DataFlowPassBase(
            CSharpCompilation compilation,
            Symbol member,
            BoundNode node,
            EmptyStructTypeCache emptyStructs,
            bool trackUnassignments)
            : base(compilation, member, node, trackUnassignments: trackUnassignments)
        {
            _emptyStructTypeCache = emptyStructs;
        }

        protected DataFlowPassBase(
            CSharpCompilation compilation,
            Symbol member,
            BoundNode node,
            EmptyStructTypeCache emptyStructs,
            BoundNode firstInRegion,
            BoundNode lastInRegion,
            bool trackRegions,
            bool trackUnassignments)
            : base(compilation, member, node, firstInRegion, lastInRegion, trackRegions: trackRegions, trackUnassignments: trackUnassignments)
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

        /// <summary>
        /// Force a variable to have a slot.  Returns -1 if the variable has an empty struct type.
        /// </summary>
        protected int GetOrCreateSlot(Symbol symbol, int containingSlot = 0)
        {
            if (symbol.Kind == SymbolKind.RangeVariable) return -1;

            containingSlot = DescendThroughTupleRestFields(ref symbol, containingSlot, forceContainingSlotsToExist: true);

            VariableIdentifier identifier = new VariableIdentifier(symbol, containingSlot);
            int slot;

            // Since analysis may proceed in multiple passes, it is possible the slot is already assigned.
            if (!_variableSlot.TryGetValue(identifier, out slot))
            {
                var variableType = VariableType(symbol)?.TypeSymbol;
                 if (_emptyStructTypeCache.IsEmptyStructType(variableType))
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

            Normalize(ref this.State);
            return slot;
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
                while (containingType != symbol.ContainingType)
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

                    containingType = restField.Type.TypeSymbol.TupleUnderlyingTypeOrSelf();
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
                Debug.Assert(variableId.Symbol.Kind == SymbolKind.Field || variableId.Symbol.Kind == SymbolKind.Property);
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
                    return (object)MethodThisParameter != null ? GetOrCreateSlot(MethodThisParameter) : -1;
                case BoundKind.BaseReference:
                    return GetOrCreateSlot(MethodThisParameter);
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
                        int containingSlot = MakeSlot(receiver);
                        return (containingSlot == -1) ? -1 : GetOrCreateSlot(member, containingSlot);
                    }
                    break;
                case BoundKind.AssignmentOperator:
                    return MakeSlot(((BoundAssignmentOperator)node).Left);
            }
            return -1;
        }

        protected void VisitStatementsWithLocalFunctions(BoundBlock block)
        {
            // Visit the statements in two phases:
            //   1. Local function declarations
            //   2. Everything else
            //
            // The idea behind visiting local functions first is
            // that we may be able to gather the captured variables
            // they read and write ahead of time in a single pass, so
            // when they are used by other statements in the block we
            // won't have to recompute the set by doing multiple passes.
            //
            // If the local functions contain forward calls to other local
            // functions then we may have to do another pass regardless,
            // but hopefully that will be an uncommon case in real-world code.

            // First phase
            if (!block.LocalFunctions.IsDefaultOrEmpty)
            {
                foreach (var stmt in block.Statements)
                {
                    if (stmt.Kind == BoundKind.LocalFunctionStatement)
                    {
                        VisitAlways(stmt);
                    }
                }
            }

            // Second phase
            foreach (var stmt in block.Statements)
            {
                if (stmt.Kind != BoundKind.LocalFunctionStatement)
                {
                    VisitStatement(stmt);
                }
            }
        }

        protected static TypeSymbolWithAnnotations VariableType(Symbol s)
        {
            switch (s.Kind)
            {
                case SymbolKind.Local:
                    return ((LocalSymbol)s).Type;
                case SymbolKind.Field:
                    return ((FieldSymbol)s).Type;
                case SymbolKind.Parameter:
                    return ((ParameterSymbol)s).Type;
                case SymbolKind.Method:
                    Debug.Assert(((MethodSymbol)s).MethodKind == MethodKind.LocalFunction);
                    return null;
                case SymbolKind.Property:
                    return ((PropertySymbol)s).Type;
                default:
                    throw ExceptionUtilities.UnexpectedValue(s.Kind);
            }
        }
    }
}
