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
            out IEnumerable<Symbol> unsafeAddressTaken,
            out IEnumerable<Symbol> capturedInside,
            out IEnumerable<Symbol> capturedOutside,
            out IEnumerable<MethodSymbol> usedLocalFunctions)
        {
            var walker = new ReadWriteWalker(compilation, member, node, firstInRegion, lastInRegion, unassignedVariableAddressOfSyntaxes);
            try
            {
                bool badRegion = false;
                walker.Analyze(ref badRegion);
                if (badRegion)
                {
                    readInside = writtenInside = readOutside = writtenOutside = captured = unsafeAddressTaken = capturedInside = capturedOutside = Enumerable.Empty<Symbol>();
                    usedLocalFunctions = Enumerable.Empty<MethodSymbol>();
                }
                else
                {
                    readInside = walker._readInside;
                    writtenInside = walker._writtenInside;
                    readOutside = walker._readOutside;
                    writtenOutside = walker._writtenOutside;

                    captured = walker.GetCaptured();
                    capturedInside = walker.GetCapturedInside();
                    capturedOutside = walker.GetCapturedOutside();

                    unsafeAddressTaken = walker.GetUnsafeAddressTaken();

                    usedLocalFunctions = walker.GetUsedLocalFunctions();
                }
            }
            finally
            {
                walker.Free();
            }
        }

        private readonly HashSet<Symbol> _readInside = new HashSet<Symbol>();
        private readonly HashSet<Symbol> _writtenInside = new HashSet<Symbol>();
        private readonly HashSet<Symbol> _readOutside = new HashSet<Symbol>();
        private readonly HashSet<Symbol> _writtenOutside = new HashSet<Symbol>();

        private ReadWriteWalker(CSharpCompilation compilation, Symbol member, BoundNode node, BoundNode firstInRegion, BoundNode lastInRegion,
            HashSet<PrefixUnaryExpressionSyntax> unassignedVariableAddressOfSyntaxes)
            : base(compilation, member, node, firstInRegion, lastInRegion, unassignedVariableAddressOfSyntaxes: unassignedVariableAddressOfSyntaxes)
        {
        }

        protected override void EnterRegion()
        {
            Symbol current = CurrentSymbol;
            bool ignoreThisParameter = false;

            while (current?.Kind is SymbolKind.Method or SymbolKind.Field or SymbolKind.Property)
            {
                if (current is MethodSymbol m)
                {
                    foreach (var p in m.Parameters)
                    {
                        if (p.RefKind != RefKind.None) _readOutside.Add(p);
                    }

                    if (_symbol.TryGetInstanceExtensionParameter(out ParameterSymbol extensionParameter) && extensionParameter.RefKind != RefKind.None)
                    {
                        _readOutside.Add(extensionParameter);
                    }

                    Debug.Assert(!ignoreThisParameter || m is SynthesizedPrimaryConstructor);

                    if (!ignoreThisParameter)
                    {
                        var thisParameter = m.ThisParameter;
                        if ((object)thisParameter != null && thisParameter.RefKind != RefKind.None)
                        {
                            _readOutside.Add(thisParameter);
                        }
                    }
                }

                Symbol containing = current.ContainingSymbol;

                if (!current.IsStatic &&
                    containing is SourceMemberContainerTypeSymbol { PrimaryConstructor: { } primaryConstructor } &&
                    (object)current != primaryConstructor)
                {
                    current = primaryConstructor;
                    ignoreThisParameter = true;
                }
                else
                {
                    current = containing;
                }
            }

            base.EnterRegion();
        }

        /// <summary>
        /// Note that a variable is read.
        /// </summary>
        /// <param name="variable">The variable</param>
        /// <param name="rangeVariableUnderlyingParameter">If variable.Kind is RangeVariable, its underlying lambda parameter. Else null.</param>
        protected override void NoteRead(Symbol variable, ParameterSymbol rangeVariableUnderlyingParameter = null)
        {
            if ((object)variable == null) return;
            if (variable.Kind != SymbolKind.Field) (IsInside ? _readInside : _readOutside).Add(variable);
            base.NoteRead(variable, rangeVariableUnderlyingParameter);
        }

        protected override void NoteWrite(Symbol variable, BoundExpression value, bool read, bool isRef)
        {
            if ((object)variable == null) return;
            (IsInside ? _writtenInside : _writtenOutside).Add(variable);
            base.NoteWrite(variable, value, read: read, isRef: isRef);
        }

        protected override void CheckAssigned(BoundExpression expr, FieldSymbol fieldSymbol, SyntaxNode node)
        {
            base.CheckAssigned(expr, fieldSymbol, node);
            if (!IsInside && node.Span.Contains(RegionSpan) && (expr.Kind == BoundKind.FieldAccess))
            {
                NoteReceiverRead((BoundFieldAccess)expr);
            }
        }

        private void NoteReceiverWritten(BoundFieldAccess expr)
        {
            NoteReceiverReadOrWritten(expr, _writtenInside);
        }

        private void NoteReceiverWritten(BoundInlineArrayAccess expr)
        {
            NoteExpressionReadOrWritten(expr.Expression, _writtenInside);
        }

        private void NoteReceiverRead(BoundFieldAccess expr)
        {
            NoteReceiverReadOrWritten(expr, _readInside);
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
            NoteExpressionReadOrWritten(receiver, readOrWritten);
        }

        private void NoteExpressionReadOrWritten(BoundExpression receiver, HashSet<Symbol> readOrWritten)
        {
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
                        NoteReceiverReadOrWritten((BoundFieldAccess)receiver, readOrWritten);
                    }
                    break;
                case BoundKind.InlineArrayAccess:
                    if (receiverSyntax.Span.OverlapsWith(RegionSpan))
                    {
                        var elementAccess = (BoundInlineArrayAccess)receiver;
                        NoteExpressionReadOrWritten(elementAccess.Expression, readOrWritten);
                    }
                    break;
            }
        }

        protected override void AssignImpl(BoundNode node, BoundExpression value, bool isRef, bool written, bool read)
        {
            switch (node.Kind)
            {
                case BoundKind.RangeVariable:
                    if (written) NoteWrite(((BoundRangeVariable)node).RangeVariableSymbol, value, read: read, isRef: isRef);
                    break;

                case BoundKind.QueryClause:
                    {
                        base.AssignImpl(node, value, isRef, written, read);
                        var symbol = ((BoundQueryClause)node).DefinedSymbol;
                        if ((object)symbol != null)
                        {
                            if (written) NoteWrite(symbol, value, read: read, isRef: isRef);
                        }
                    }
                    break;

                case BoundKind.FieldAccess:
                    {
                        base.AssignImpl(node, value, isRef, written, read);
                        var fieldAccess = node as BoundFieldAccess;
                        if (!IsInside && node.Syntax != null && node.Syntax.Span.Contains(RegionSpan))
                        {
                            NoteReceiverWritten(fieldAccess);
                        }
                    }
                    break;
                case BoundKind.InlineArrayAccess:
                    {
                        base.AssignImpl(node, value, isRef, written, read);
                        var elementAccess = (BoundInlineArrayAccess)node;
                        if (!IsInside && node.Syntax != null && node.Syntax.Span.Contains(RegionSpan))
                        {
                            NoteReceiverWritten(elementAccess);
                        }

                        break;
                    }

                default:
                    base.AssignImpl(node, value, isRef, written, read);
                    break;
            }
        }

        public override BoundNode VisitUnboundLambda(UnboundLambda node)
        {
            return VisitLambda(node.BindForErrorRecovery());
        }

        public override BoundNode VisitRangeVariable(BoundRangeVariable node)
        {
            // Compute the "underlying symbol" for a read of the range variable
            ParameterSymbol rangeVariableUnderlyingParameter = GetRangeVariableUnderlyingParameter(node.Value);
            NoteRead(node.RangeVariableSymbol, rangeVariableUnderlyingParameter);
            return null;
        }

        /// <summary>
        /// Compute the underlying lambda parameter symbol for a range variable, if any.
        /// </summary>
        /// <param name="underlying">The bound node for the expansion of the range variable</param>
        private static ParameterSymbol GetRangeVariableUnderlyingParameter(BoundNode underlying)
        {
            while (underlying != null)
            {
                switch (underlying.Kind)
                {
                    case BoundKind.Parameter:
                        return ((BoundParameter)underlying).ParameterSymbol;
                    case BoundKind.PropertyAccess:
                        underlying = ((BoundPropertyAccess)underlying).ReceiverOpt;
                        continue;
                    default:
                        return null;
                }
            }

            return null;
        }

        public override BoundNode VisitQueryClause(BoundQueryClause node)
        {
            Assign(node, value: null);
            return base.VisitQueryClause(node);
        }
    }
}
