// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class DataFlowPass
    {
        private class LocalFuncInfo
        {
            public LocalFuncInfo(BoundLocalFunctionStatement boundNode)
            {
                BoundNode = boundNode;
            }

            public struct UsedVariable : IEqualityComparer<UsedVariable>
            {
                public bool Written { get; }
                public Symbol CapturedSymbol { get; }
                public Symbol SymbolOpt { get; }
                public int? SlotOpt { get; }

                public UsedVariable(bool written,
                                    Symbol capturedSymbol,
                                    Symbol symbol = null,
                                    int? slot = null)
                {
                    Written = written;
                    CapturedSymbol = capturedSymbol;
                    SymbolOpt = symbol;
                    SlotOpt = slot;
                }

                public bool Equals(UsedVariable left, UsedVariable right) =>
                    left.Written == right.Written &&
                    left.CapturedSymbol == right.CapturedSymbol &&
                    left.SymbolOpt == right.SymbolOpt &&
                    left.SlotOpt == right.SlotOpt;

                public int GetHashCode(UsedVariable usedVar) =>
                    Hash.Combine(usedVar.Written,
                    Hash.Combine(usedVar.CapturedSymbol.GetHashCode(),
                    Hash.Combine(usedVar.SymbolOpt?.GetHashCode() ?? 0,
                                 usedVar.SlotOpt?.GetHashCode() ?? 0)));
            }

            public OrderedSet<UsedVariable> UsedVariables { get; } =
                new OrderedSet<UsedVariable>();

            private PooledHashSet<LocalFunctionSymbol> _usedLocalFuncs;
            public PooledHashSet<LocalFunctionSymbol> UsedLocalFunctions
            {
                get
                {
                    if (_usedLocalFuncs == null)
                    {
                        _usedLocalFuncs = PooledHashSet<LocalFunctionSymbol>.GetInstance();
                    }

                    return _usedLocalFuncs;
                }
            }

            /// <summary>
            /// Indicates that the set of "used" variables changed during the
            /// last data flow pass. If the set changed, we must re-run the
            /// analysis until we have reached a fixed-point.
            /// </summary>
            public bool IsDirty { get; set; } = false;

            public BoundLocalFunctionStatement BoundNode { get; }

            public void Free()
            {
                _usedLocalFuncs?.Free();
            }
        }

        // Save the list of reads, writes, and diagnostics for all local funcs
        private SmallDictionary<LocalFunctionSymbol, LocalFuncInfo> _localFuncResults;
        private SmallDictionary<LocalFunctionSymbol, LocalFuncInfo> LocalFuncResults
        {
            get
            {
                if (_localFuncResults == null)
                {
                    _localFuncResults = new SmallDictionary<LocalFunctionSymbol, LocalFuncInfo>();
                }

                return _localFuncResults;
            }
        }

        private void VisitLocalFunctions(BoundBlock block)
        {
            foreach (var stmt in block.Statements)
            {
                if (stmt.Kind == BoundKind.LocalFunctionStatement)
                {
                    var localFunc = (BoundLocalFunctionStatement)stmt;

                    // Initialize local func before visiting
                    if (!LocalFuncResults.ContainsKey(localFunc.Symbol))
                    {
                        LocalFuncResults.Add(localFunc.Symbol, new LocalFuncInfo(localFunc));
                    }
                    VisitLambdaOrLocalFunction(localFunc);
                }
            }

            if ((object)currentMethodOrLambda == topLevelMethod &&
                _localFuncResults != null)
            {
                // Visit dirty local functions until the read/written set is stable
                while (HasDirtyLocalFunctions(LocalFuncResults.Values))
                {
                    foreach (var kvp in LocalFuncResults)
                    {
                        var info = kvp.Value;

                        // The local func counts as dirty if it itself is dirty
                        // or if one of its dependencies is dirty
                        bool isDirty = info.IsDirty;
                        foreach (var dependency in info.UsedLocalFunctions)
                        {
                            isDirty |= LocalFuncResults[dependency].IsDirty;
                            if (isDirty)
                            {
                                break;
                            }
                        }

                        info.IsDirty = false;
                        if (isDirty)
                        {
                            // Note: Recursively visits this local function, which
                            // may cause other local functions to be visited, even
                            // if they're not marked dirty.
                            // Consider: Is there a more efficient walk that we could
                            // do here?
                            VisitLambdaOrLocalFunction(info.BoundNode);
                        }
                    }
                }
            }
        }

        private static bool HasDirtyLocalFunctions<T>(T localFuncInfos)
            where T : IEnumerable<LocalFuncInfo>
        {
            foreach (var info in localFuncInfos)
            {
                if (info.IsDirty)
                {
                    return true;
                }
            }
            return false;
        }

        private void NoteReadInLocalFunction(Symbol variable, Symbol capturedSymbol, int? slotOpt = null)
        {
            var localFunc = GetNearestLocalFunctionOpt();

            Debug.Assert(localFunc != null);

            var results = LocalFuncResults[localFunc];
            var usedVar = new LocalFuncInfo.UsedVariable(written: false,
                                                         capturedSymbol: capturedSymbol,
                                                         symbol: variable,
                                                         slot: slotOpt);
            if (!results.UsedVariables.Contains(usedVar))
            {
                results.UsedVariables.Add(usedVar);
                results.IsDirty = true;
            }
        }

        private void AssignVarInLocalFunction(int slot, Symbol capturedSymbol)
        {
            var localFunc = GetNearestLocalFunctionOpt();

            Debug.Assert(localFunc != null);

            var info = LocalFuncResults[localFunc];
            var usedVar = new LocalFuncInfo.UsedVariable(written: true,
                                                         capturedSymbol: capturedSymbol,
                                                         slot: slot);
            if (!info.UsedVariables.Contains(usedVar))
            {
                info.UsedVariables.Add(usedVar);
                info.IsDirty = true;
            }
        }

        private void VisitLocalFunctionAccess(
            LocalFunctionSymbol localFunc,
            CSharpSyntaxNode syntax,
            bool write)
        {
            _usedLocalFunctions.Add(localFunc);
            LocalFuncInfo info;
            if (LocalFuncResults.TryGetValue(localFunc, out info))
            {
                foreach (var usedVar in info.UsedVariables)
                {
                    bool capturedInLocalFunction =
                        IsCapturedInLocalFunction(usedVar.CapturedSymbol);

                    if (write && usedVar.Written)
                    {
                        if (capturedInLocalFunction)
                        {
                            AssignVarInLocalFunction(usedVar.SlotOpt.Value, usedVar.CapturedSymbol);
                        }
                        else
                        {
                            SetSlotState(usedVar.SlotOpt.Value, assigned: true);
                        }
                    }
                    else
                    {
                        if (capturedInLocalFunction)
                        {
                            NoteReadInLocalFunction(usedVar.SymbolOpt,
                                                    usedVar.CapturedSymbol,
                                                    slotOpt: usedVar.SlotOpt);
                        }
                        else
                        {
                            CheckAssigned(usedVar.SymbolOpt, syntax, usedVar.SlotOpt);
                        }
                    }
                }
            }

            var containingLocalFunc = GetNearestLocalFunctionOpt();
            if ((object)containingLocalFunc != null)
            {
                info = LocalFuncResults[containingLocalFunc];
                if (!info.UsedLocalFunctions.Contains(localFunc))
                {
                    info.UsedLocalFunctions.Add(localFunc);
                    info.IsDirty = true;
                }
            }
        }

        private bool IsCapturedInLocalFunction(Symbol variable,
            ParameterSymbol rangeVariableUnderlyingParameter = null)
        {
            if (variable == null)
            {
                return false;
            }
            
            if (!IsCaptured(variable,
                            rangeVariableUnderlyingParameter,
                            currentMethodOrLambda))
            {
                return false;
            }

            var localFunc = GetNearestLocalFunctionOpt();
            if ((object)localFunc == null)
            {
                return false;
            }

            return IsCaptured(variable,
                              rangeVariableUnderlyingParameter,
                              localFunc);
        }

        /// <summary>
        /// If the nearest enclosing non-lambda method is a local function, returns
        /// that local function. Otherwise, returns null.
        /// </summary>
        private LocalFunctionSymbol GetNearestLocalFunctionOpt()
        {
            // Lambdas inherit from their containing method/function
            Symbol containingSymbol = currentMethodOrLambda;
            while ((object)containingSymbol != null)
            {
                if (containingSymbol.Kind == SymbolKind.Method &&
                    ((MethodSymbol)containingSymbol).MethodKind != MethodKind.AnonymousFunction)
                {
                    break;
                }
                containingSymbol = containingSymbol.ContainingSymbol;
            }

            return containingSymbol as LocalFunctionSymbol;
        }
    }
}
