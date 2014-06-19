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
    /// A region analysis walker that records reads and writes of all variables, both inside and outside the region.
    /// </summary>
    internal class ReadWriteWalker : AbstractRegionDataFlowPass
    {
        internal static void Analyze(
            CSharpCompilation compilation, Symbol member, BoundNode node, BoundNode firstInRegion, BoundNode lastInRegion, HashSet<PrefixUnaryExpressionSyntax> unassignedVariableAddressOfSyntaxes,
            out IEnumerable<Symbol> readInside,
            out IEnumerable<Symbol> writtenInside,
            out IEnumerable<Symbol> readOutside,
            out IEnumerable<Symbol> writtenOutside,
            out IEnumerable<Symbol> captured,
            out IEnumerable<Symbol> unsafeAddressTaken)
        {
            var walker = new ReadWriteWalker(compilation, member, node, firstInRegion, lastInRegion, unassignedVariableAddressOfSyntaxes);
            try
            {
                bool badRegion = false;
                walker.Analyze(ref badRegion);
                if (badRegion)
                {
                    readInside = writtenInside = readOutside = writtenOutside = captured = unsafeAddressTaken = Enumerable.Empty<Symbol>();
                }
                else
                {
                    readInside = walker.readInside;
                    writtenInside = walker.writtenInside;
                    readOutside = walker.readOutside;
                    writtenOutside = walker.writtenOutside;

                    captured = walker.GetCaptured();
                    unsafeAddressTaken = walker.GetUnsafeAddressTaken();
                }
            }
            finally
            {
                walker.Free();
            }
        }

        private readonly HashSet<Symbol> readInside = new HashSet<Symbol>();
        private readonly HashSet<Symbol> writtenInside = new HashSet<Symbol>();
        private readonly HashSet<Symbol> readOutside = new HashSet<Symbol>();
        private readonly HashSet<Symbol> writtenOutside = new HashSet<Symbol>();

        private ReadWriteWalker(CSharpCompilation compilation, Symbol member, BoundNode node, BoundNode firstInRegion, BoundNode lastInRegion,
            HashSet<PrefixUnaryExpressionSyntax> unassignedVariableAddressOfSyntaxes)
            : base(compilation, member, node, firstInRegion, lastInRegion, unassignedVariableAddressOfSyntaxes: unassignedVariableAddressOfSyntaxes)
        {
        }

        protected override void EnterRegion()
        {
            for (MethodSymbol m = this.currentMethodOrLambda; (object)m != null; m = m.ContainingSymbol as MethodSymbol)
            {
                foreach (var p in m.Parameters)
                {
                    if (p.RefKind != RefKind.None) readOutside.Add(p);
                }

                if (!m.IsStatic && (object)m.ThisParameter != null && m.ThisParameter.RefKind != RefKind.None)
                {
                    readOutside.Add(m.ThisParameter);
                }
            }

            base.EnterRegion();
        }

        protected override void NoteRead(Symbol variable)
        {
            if ((object)variable == null) return;
            if (variable.Kind != SymbolKind.Field) (IsInside ? readInside : readOutside).Add(variable);
            base.NoteRead(variable);
        }

        protected override void NoteWrite(Symbol variable, BoundExpression value, bool read)
        {
            if ((object)variable == null) return;
            (IsInside ? writtenInside : writtenOutside).Add(variable);
            base.NoteWrite(variable, value, read);
        }

        override protected void CheckAssigned(BoundExpression expr, FieldSymbol fieldSymbol, CSharpSyntaxNode node)
        {
            base.CheckAssigned(expr, fieldSymbol, node);
            if (!IsInside && node.Span.Contains(RegionSpan) && expr is BoundFieldAccess)
            {
                NoteReceiverRead(expr as BoundFieldAccess);
            }
        }

        private void NoteReceiverWritten(BoundFieldAccess expr)
        {
            NoteReceiverReadOrWritten(expr, writtenInside);
        }

        private void NoteReceiverRead(BoundFieldAccess expr)
        {
            NoteReceiverReadOrWritten(expr, readInside);
        }

        /// <summary>
        /// When we read a field from a struct, the receiver isn't seen as being read until we get to the
        /// end of the field access expression, because we only read the relevant piece of the struct.
        /// But we want the receiver to be considered to be read in the region in that case.
        /// For example, if an rvalue expression is x.y.z and the region is x.y, we want x to be included
        /// in the ReadInside set.  That is implemented here.
        /// </summary>
        private void NoteReceiverReadOrWritten(BoundFieldAccess expr, HashSet<Symbol> readOrWritten)
        {
            if (expr.FieldSymbol.IsStatic) return;
            if (expr.FieldSymbol.ContainingType.IsReferenceType) return;
            var receiver = expr.ReceiverOpt;
            if (receiver == null) return;
            var receiverSyntax = receiver.Syntax;
            if (receiverSyntax == null) return;
            switch (receiver.Kind)
            {
                case BoundKind.Local:
                    if (RegionContains(receiverSyntax.Span))
                    {
                        readOrWritten.Add(((BoundLocal)receiver).LocalSymbol);
                    }
                    break;
                case BoundKind.ThisReference:
                    if (RegionContains(receiverSyntax.Span))
                    {
                        readOrWritten.Add(this.MethodThisParameter);
                    }
                    break;
                case BoundKind.BaseReference:
                    if (RegionContains(receiverSyntax.Span))
                    {
                        readOrWritten.Add(this.MethodThisParameter);
                    }
                    break;
                case BoundKind.Parameter:
                    if (RegionContains(receiverSyntax.Span))
                    {
                        readOrWritten.Add(((BoundParameter)receiver).ParameterSymbol);
                    }
                    break;
                case BoundKind.RangeVariable:
                    if (RegionContains(receiverSyntax.Span))
                    {
                        readOrWritten.Add(((BoundRangeVariable)receiver).RangeVariableSymbol);
                    }
                    break;
                case BoundKind.FieldAccess:
                    if (receiver.Type.IsStructType() && receiverSyntax.Span.OverlapsWith(RegionSpan))
                    {
                        NoteReceiverReadOrWritten(receiver as BoundFieldAccess, readOrWritten);
                    }
                    break;
            }
        }

        protected override void AssignImpl(BoundNode node, BoundExpression value, RefKind refKind, bool written, bool read)
        {
            switch (node.Kind)
            {
                case BoundKind.RangeVariable:
                    if (written) NoteWrite(((BoundRangeVariable)node).RangeVariableSymbol, value, read);
                    break;

                case BoundKind.QueryClause:
                    {
                        base.AssignImpl(node, value, refKind, written, read);
                        var symbol = ((BoundQueryClause)node).DefinedSymbol;
                        if ((object)symbol != null)
                        {
                            if (written) NoteWrite(symbol, value, read);
                        }
                    }
                    break;

                case BoundKind.FieldAccess:
                    {
                        base.AssignImpl(node, value, refKind, written, read);
                        var fieldAccess = node as BoundFieldAccess;
                        if (!IsInside && node.Syntax != null && node.Syntax.Span.Contains(RegionSpan))
                        {
                            NoteReceiverWritten(fieldAccess);
                        }
                    }
                    break;

                default:
                    base.AssignImpl(node, value, refKind, written, read);
                    break;
            }
        }

        public override BoundNode VisitUnboundLambda(UnboundLambda node)
        {
            return VisitLambda(node.BindForErrorRecovery());
        }

        public override void VisitForEachIterationVariable(BoundForEachStatement node)
        {
            var local = node.IterationVariable;
            if ((object)local != null)
            {
                MakeSlot(local);
                Assign(node, value: null);
            }
        }

        public override BoundNode VisitRangeVariable(BoundRangeVariable node)
        {
            NoteRead(node.RangeVariableSymbol);
            return null;
        }

        public override BoundNode VisitQueryClause(BoundQueryClause node)
        {
            Assign(node, value: null);
            return base.VisitQueryClause(node);
        }
    }
}