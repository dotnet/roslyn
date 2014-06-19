// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A region analysis walker that computes the set of variables for
    /// which their assigned values flow out of the region.
    /// A variable assigned inside is used outside if an analysis that
    /// treats assignments in the region as unassigning the variable would
    /// cause "unassigned" errors outside the region.
    /// </summary>
    class DataFlowsOutWalker : AbstractRegionDataFlowPass
    {
        private readonly HashSet<Symbol> dataFlowsIn;

        DataFlowsOutWalker(CSharpCompilation compilation, Symbol member, BoundNode node, BoundNode firstInRegion, BoundNode lastInRegion, HashSet<Symbol> unassignedVariables, HashSet<Symbol> dataFlowsIn)
            : base(compilation, member, node, firstInRegion, lastInRegion, unassignedVariables, trackUnassignments: true)
        {
            this.dataFlowsIn = dataFlowsIn;
        }

        internal static HashSet<Symbol> Analyze(CSharpCompilation compilation, Symbol member, BoundNode node, BoundNode firstInRegion, BoundNode lastInRegion, HashSet<Symbol> unassignedVariables, HashSet<Symbol> dataFlowsIn)
        {
            var walker = new DataFlowsOutWalker(compilation, member, node, firstInRegion, lastInRegion, unassignedVariables, dataFlowsIn);
            try
            {
                bool badRegion = false;
                var result = walker.Analyze(ref badRegion);
#if DEBUG
                // Assert that DataFlowsOut only contains variables that were assigned to inside the region
                Debug.Assert(badRegion || !result.Any((variable) => !walker.assignedInside.Contains(variable)));
#endif
                return badRegion ? new HashSet<Symbol>() : result;
            }
            finally
            {
                walker.Free();
            }
        }

        private readonly HashSet<Symbol> dataFlowsOut = new HashSet<Symbol>();

#if DEBUG
        // we'd like to ensure that only variables get returned in DataFlowsOut that were assigned to inside the region.
        private readonly HashSet<Symbol> assignedInside = new HashSet<Symbol>();
#endif

        new HashSet<Symbol> Analyze(ref bool badRegion)
        {
            base.Analyze(ref badRegion, null);
            return dataFlowsOut;
        }

        protected override void EnterRegion()
        {
            // to handle loops properly, we must assume that every variable that flows in is
            // assigned at the beginning of the loop.  If it isn't, then it must be in a loop
            // and flow out of the region in that loop (and into the region inside the loop).
            foreach (var variable in dataFlowsIn)
            {
                int slot = this.MakeSlot(variable);
                if (slot > 0 && !this.State.IsAssigned(slot))
                {
                    dataFlowsOut.Add(variable);
                }
            }

            base.EnterRegion();
        }

        protected override void NoteWrite(Symbol variable, BoundExpression value, bool read)
        {
            // any reachable assignment to a ref or out parameter can be visible to the caller in the face of exceptions.
            if (this.State.Reachable && IsInside)
            {
                var param = variable as ParameterSymbol;
                if (FlowsOut(param))
                {
                    dataFlowsOut.Add(param);
                }

#if DEBUG
                if ((object)param != null)
                {
                    assignedInside.Add(param);
                }
#endif
            }

            base.NoteWrite(variable, value, read);
        }

#if DEBUG
        private Symbol GetNodeSymbol(BoundNode node)
        {
            while (node != null)
            {
                switch (node.Kind)
                {
                    case BoundKind.FieldAccess:
                        {
                            var fieldAccess = (BoundFieldAccess)node;
                            if (MayRequireTracking(fieldAccess.ReceiverOpt, fieldAccess.FieldSymbol))
                            {
                                node = fieldAccess.ReceiverOpt;
                                continue;
                            }

                            return null;
                        }

                    case BoundKind.LocalDeclaration:
                        {
                            return ((BoundLocalDeclaration)node).LocalSymbol;
                        }

                    case BoundKind.ThisReference:
                        {
                            return MethodThisParameter;
                        }

                    case BoundKind.Local:
                        {
                            return ((BoundLocal)node).LocalSymbol;
                        }

                    case BoundKind.DeclarationExpression:
                        {
                            return ((BoundDeclarationExpression)node).LocalSymbol;
                        }

                    case BoundKind.Parameter:
                        {
                            return ((BoundParameter)node).ParameterSymbol;
                        }

                    case BoundKind.CatchBlock:
                        {
                            var local = ((BoundCatchBlock)node).Locals.FirstOrDefault();
                            return (object)local != null && local.DeclarationKind == LocalDeclarationKind.Catch ? local : null;
                        }

                    case BoundKind.ForEachStatement:
                        {
                            return ((BoundForEachStatement)node).IterationVariable;
                        }

                    case BoundKind.RangeVariable:
                        {
                            return ((BoundRangeVariable)node).RangeVariableSymbol;
                        }

                    case BoundKind.EventAccess:
                        {
                            var eventAccess = (BoundEventAccess)node;
                            FieldSymbol associatedField = eventAccess.EventSymbol.AssociatedField;
                            if ((object)associatedField != null)
                            {
                                if (MayRequireTracking(eventAccess.ReceiverOpt, associatedField))
                                {
                                    node = eventAccess.ReceiverOpt;
                                    continue;
                                }
                            }
                            return null;
                        }

                    default:
                        {
                            return null;
                        }
                }
            }

            return null;
        }
#endif

        protected override void AssignImpl(BoundNode node, BoundExpression value, RefKind refKind, bool written, bool read)
        {
            if (IsInside)
            {
#if DEBUG
                {
                    Symbol variable = GetNodeSymbol(node);
                    if ((object)variable != null)
                    {
                        assignedInside.Add(variable);
                    }
                }
#endif
                written = false;

                // any reachable assignment to a ref or out parameter can be visible to the caller in the face of exceptions.
                if (State.Reachable)
                {
                    ParameterSymbol param = Param(node);
                    if (FlowsOut(param))
                    {
                        this.dataFlowsOut.Add(param);
                    }
                }
            }

            base.AssignImpl(node, value, refKind, written, read);
        }

        private bool FlowsOut(ParameterSymbol param)
        {
            return (object)param != null && param.RefKind != RefKind.None && !param.IsImplicitlyDeclared && !RegionContains(param.Locations[0].SourceSpan);
        }

        private ParameterSymbol Param(BoundNode node)
        {
            switch (node.Kind)
            {
                case BoundKind.Parameter: return ((BoundParameter)node).ParameterSymbol;
                case BoundKind.ThisReference: return this.MethodThisParameter;
                default: return null;
            }
        }

        public override BoundNode VisitQueryClause(BoundQueryClause node)
        {
            return base.VisitQueryClause(node);
        }

        protected override void ReportUnassigned(Symbol symbol, CSharpSyntaxNode node)
        {
            if (!dataFlowsOut.Contains(symbol) && !(symbol is FieldSymbol) && !IsInside)
            {
                dataFlowsOut.Add(symbol);
            }
            base.ReportUnassigned(symbol, node);
        }

        protected override void ReportUnassignedOutParameter(ParameterSymbol parameter, CSharpSyntaxNode node, Location location)
        {
            if (!dataFlowsOut.Contains(parameter) && (node == null || node is ReturnStatementSyntax))
            {
                dataFlowsOut.Add(parameter);
            }
            base.ReportUnassignedOutParameter(parameter, node, location);
        }

        protected override void ReportUnassigned(FieldSymbol fieldSymbol, int unassignedSlot, CSharpSyntaxNode node)
        {
            if (!IsInside)
            {
                //  if the field access is reported as unassigned it should mean the original local 
                //  or parameter flows out, so we should get the symbol associated with the expression
                var symbol = GetNonFieldSymbol(unassignedSlot);
                if (!dataFlowsOut.Contains(symbol))
                {
                    dataFlowsOut.Add(symbol);
                }
            }
            base.ReportUnassigned(fieldSymbol, unassignedSlot, node);
        }
    }
}