// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using System;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal class LocalFunctionAssignmentPass : DataFlowPass
    {
        private SmallDictionary<LocalFunctionSymbol, LocalFuncInfo> _assignedVars =
            new SmallDictionary<LocalFunctionSymbol, LocalFuncInfo>();

        internal LocalFunctionAssignmentPass(
            CSharpCompilation compilation,
            Symbol member,
            BoundNode node,
            bool trackUnassignments = false,
            HashSet<PrefixUnaryExpressionSyntax> unassignedVariableAddressOfSyntaxes = null,
            bool requireOutParamsAssigned = true)
            : base(compilation, member, node, trackUnassignments, unassignedVariableAddressOfSyntaxes, requireOutParamsAssigned)
        {
        }

        protected override LocalFuncAssignmentResults GetResults() =>
            new LocalFuncAssignmentResults(_assignedVars, ImmutableArray.Create(variableBySlot));

        protected override void ReportUnassigned(FieldSymbol fieldSymbol, int unassignedSlot, CSharpSyntaxNode node)
        {
            // Don't report diagnostics
        }

        protected override void ReportUnassigned(Symbol symbol, CSharpSyntaxNode node)
        {
            // Don't report diagnostics
        }

        protected override void ReportUnassignedOutParameter(ParameterSymbol parameter, CSharpSyntaxNode node, Location location)
        {
            // Don't report diagnostics
        }

        protected override void AssignImpl(BoundNode node, BoundExpression value, RefKind refKind, bool written, bool read)
        {
            base.AssignImpl(node, value, refKind, written, read);
        }

        protected override ImmutableArray<PendingBranch> RemoveReturns()
        {
            return PendingBranches.ToImmutable();
        }

        public override BoundNode VisitBlock(BoundBlock node)
        {
            do
            {
                ClearDirtyBits(_assignedVars.Values);

                foreach (var stmt in node.Statements)
                {
                    if ((object)currentMethodOrLambda == topLevelMethod)
                    {
                        if (stmt.Kind == BoundKind.LocalFunctionStatement)
                        {
                            VisitLocalFunctionStatement((BoundLocalFunctionStatement)stmt);
                        }
                    }
                    else
                    {
                        VisitStatement(stmt);
                    }
                }
            }
            while (HasDirtyLocalFunctions(_assignedVars.Values));

            return null;
        }

        protected override void ReplayReadsAndWrites(
            LocalFunctionSymbol localFunc,
            CSharpSyntaxNode syntax,
            bool writes)
        {
            // Only need to replay writes
            if (!writes) return;

            LocalFuncInfo info;
            if (_assignedVars.TryGetValue(localFunc, out info))
            {
                foreach (var slot in info.UsedVars)
                {
                    SetSlotState(slot, assigned: true);
                }
            }
        }

        public override BoundNode VisitLocalFunctionStatement(BoundLocalFunctionStatement node) =>
            // Record assignments outside local functions
            VisitLambdaOrLocalFunction(node, recordAssigns: true);

        protected override void RecordCapturedAssigns(
            ref LocalState original,
            ref LocalState @new)
        {
            for (int slot = 1; slot < @new.Assigned.Capacity; slot++)
            {
                if (@new.Assigned[slot] &&
                    (slot >= original.Assigned.Capacity ||
                     !original.Assigned[slot]))
                {
                    RecordIfCaptured(slot);
                }
            }
        }

        private void RecordIfCaptured(int slot)
        {
            Debug.Assert(currentMethodOrLambda.MethodKind == MethodKind.LocalFunction);

            // Find the root slot, since that would be the only
            // slot, if any, that is captured in a local function
            var rootVarInfo = variableBySlot[RootSlot(slot)];

            var rootSymbol = rootVarInfo.Symbol;
            if (rootSymbol != null &&
                IsCaptured(rootSymbol, currentMethodOrLambda))
            {
                var localFunc = (LocalFunctionSymbol)currentMethodOrLambda;
                LocalFuncInfo info;
                if (_assignedVars.TryGetValue(localFunc, out info))
                {
                    if (!info.UsedVars.Contains(slot))
                    {
                        info.UsedVars.Add(slot);
                        info.IsDirty = true;
                    }
                }
                else
                {
                    info = new LocalFuncInfo();
                    info.UsedVars.Add(slot);
                    info.IsDirty = true;
                    _assignedVars[localFunc] = info;
                }
            }
        }
    }
}