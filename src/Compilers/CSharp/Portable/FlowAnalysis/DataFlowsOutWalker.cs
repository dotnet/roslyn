// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A region analysis walker that computes the set of variables for
    /// which their assigned values flow out of the region.
    /// A variable assigned inside is used outside if an analysis that
    /// treats assignments in the region as unassigning the variable would
    /// cause "unassigned" errors outside the region.
    /// </summary>
    internal class DataFlowsOutWalker : AbstractRegionDataFlowPass
    {
        private readonly ImmutableArray<ISymbol> _dataFlowsIn;

        private DataFlowsOutWalker(CSharpCompilation compilation, Symbol member, BoundNode node, BoundNode firstInRegion, BoundNode lastInRegion, HashSet<Symbol> unassignedVariables, ImmutableArray<ISymbol> dataFlowsIn)
            : base(compilation, member, node, firstInRegion, lastInRegion, unassignedVariables, trackUnassignments: true)
        {
            _dataFlowsIn = dataFlowsIn;
        }

        internal static HashSet<Symbol> Analyze(CSharpCompilation compilation, Symbol member, BoundNode node, BoundNode firstInRegion, BoundNode lastInRegion, HashSet<Symbol> unassignedVariables, ImmutableArray<ISymbol> dataFlowsIn)
        {
            var walker = new DataFlowsOutWalker(compilation, member, node, firstInRegion, lastInRegion, unassignedVariables, dataFlowsIn);
            try
            {
                bool badRegion = false;
                var result = walker.Analyze(ref badRegion);
#if DEBUG
                // Assert that DataFlowsOut only contains variables that were assigned to inside the region
                // https://github.com/dotnet/roslyn/issues/41600 blocks some tests with local functions. 
                // Enable the following assert once the issue is fixed.
                //Debug.Assert(badRegion || !result.Any((variable) => !walker._assignedInside.Contains(variable)));
#endif
                return badRegion ? new HashSet<Symbol>() : result;
            }
            finally
            {
                walker.Free();
            }
        }

        private readonly HashSet<Symbol> _dataFlowsOut = new HashSet<Symbol>();

#if DEBUG
        // we'd like to ensure that only variables get returned in DataFlowsOut that were assigned to inside the region.
        private readonly HashSet<Symbol> _assignedInside = new HashSet<Symbol>();
#endif

        private HashSet<Symbol> Analyze(ref bool badRegion)
        {
            base.Analyze(ref badRegion, null);
            return _dataFlowsOut;
        }

        protected override ImmutableArray<PendingBranch> Scan(ref bool badRegion)
        {
            _dataFlowsOut.Clear();
            return base.Scan(ref badRegion);
        }

        protected override void EnterRegion()
        {
            // to handle loops properly, we must assume that every variable that flows in is
            // assigned at the beginning of the loop.  If it isn't, then it must be in a loop
            // and flow out of the region in that loop (and into the region inside the loop).
            foreach (ISymbol variable in _dataFlowsIn)
            {
                Symbol variableSymbol = variable.GetSymbol();
                int slot = this.GetOrCreateSlot(variableSymbol);
                if (slot > 0 && !this.State.IsAssigned(slot))
                {
                    _dataFlowsOut.Add(variableSymbol);
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
                    _dataFlowsOut.Add(param);
                }

#if DEBUG
                if ((object)param != null)
                {
                    _assignedInside.Add(param);
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
                    case BoundKind.ListPattern:
                    case BoundKind.RecursivePattern:
                    case BoundKind.DeclarationPattern:
                        {
                            return ((BoundObjectPattern)node).Variable as LocalSymbol;
                        }

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

                    case BoundKind.Parameter:
                        {
                            return ((BoundParameter)node).ParameterSymbol;
                        }

                    case BoundKind.CatchBlock:
                        {
                            var local = ((BoundCatchBlock)node).Locals.FirstOrDefault();
                            return local?.DeclarationKind == LocalDeclarationKind.CatchVariable ? local : null;
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

                    case BoundKind.LocalFunctionStatement:
                        {
                            return ((BoundLocalFunctionStatement)node).Symbol;
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

        protected override void AssignImpl(BoundNode node, BoundExpression value, bool isRef, bool written, bool read)
        {
            if (IsInside)
            {
#if DEBUG
                {
                    Symbol variable = GetNodeSymbol(node);
                    if ((object)variable != null)
                    {
                        _assignedInside.Add(variable);
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
                        _dataFlowsOut.Add(param);
                    }
                }
            }

            base.AssignImpl(node, value, isRef, written, read);
        }

        private bool FlowsOut(ParameterSymbol param)
        {
            return (object)param != null &&
                   ((param.RefKind != RefKind.None && !param.IsImplicitlyDeclared && !RegionContains(param.GetFirstLocation().SourceSpan)) ||
                    param.ContainingSymbol is SynthesizedPrimaryConstructor); // Primary constructor parameter can be used in other initializers and methods 
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

        protected override void ReportUnassigned(Symbol symbol, SyntaxNode node, int slot, bool skipIfUseBeforeDeclaration)
        {
            if (!IsInside)
            {
                // If the field access is reported as unassigned it should mean the original local
                // or parameter flows out, so we should get the symbol associated with the expression
                _dataFlowsOut.Add(symbol.Kind == SymbolKind.Field ? GetNonMemberSymbol(slot) : symbol);
            }

            base.ReportUnassigned(symbol, node, slot, skipIfUseBeforeDeclaration);
        }

        protected override void ReportUnassignedOutParameter(ParameterSymbol parameter, SyntaxNode node, Location location)
        {
            if (!_dataFlowsOut.Contains(parameter) && (node == null || node is ReturnStatementSyntax))
            {
                _dataFlowsOut.Add(parameter);
            }
            base.ReportUnassignedOutParameter(parameter, node, location);
        }
    }
}
