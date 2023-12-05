// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A walker that computes the set of local variables of an iterator/async
    /// method that must be hoisted to the state machine.
    /// </summary>
    /// <remarks>
    /// Data flow analysis is used to calculate the locals. At yield/await we mark all variables as "unassigned".
    /// When a read from an unassigned variables is reported we add the variable to the captured set.
    /// "this" parameter is captured if a reference to "this", "base" or an instance field is encountered.
    /// Variables used in finally also need to be captured if there is a yield in the corresponding try block.
    /// </remarks>
    internal sealed class IteratorAndAsyncCaptureWalker : DefiniteAssignmentPass
    {
        // In Release builds we hoist only variables (locals and parameters) that are captured. 
        // This set will contain such variables after the bound tree is visited.
        private readonly OrderedSet<Symbol> _variablesToHoist = new OrderedSet<Symbol>();

        // Contains variables that are captured but can't be hoisted since their type can't be allocated on heap.
        // The value is a list of all uses of each such variable.
        private MultiDictionary<Symbol, SyntaxNode> _lazyDisallowedCaptures;

        private bool _seenYieldInCurrentTry;

        // The initializing expressions for compiler-generated ref local temps.  If the temp needs to be hoisted, then any
        // variables in its initializing expression will need to be hoisted too.
        private readonly Dictionary<LocalSymbol, BoundExpression> _boundRefLocalInitializers = new Dictionary<LocalSymbol, BoundExpression>();

        private IteratorAndAsyncCaptureWalker(CSharpCompilation compilation, MethodSymbol method, BoundNode node, HashSet<Symbol> initiallyAssignedVariables)
            : base(compilation,
                  method,
                  node,
                  EmptyStructTypeCache.CreateNeverEmpty(),
                  trackUnassignments: true,
                  initiallyAssignedVariables: initiallyAssignedVariables)
        {
        }

        // Returns deterministically ordered list of variables that ought to be hoisted.
        public static OrderedSet<Symbol> Analyze(CSharpCompilation compilation, MethodSymbol method, BoundNode node, DiagnosticBag diagnostics)
        {
            var initiallyAssignedVariables = UnassignedVariablesWalker.Analyze(compilation, method, node, convertInsufficientExecutionStackExceptionToCancelledByStackGuardException: true);
            var walker = new IteratorAndAsyncCaptureWalker(compilation, method, node, initiallyAssignedVariables);

            walker._convertInsufficientExecutionStackExceptionToCancelledByStackGuardException = true;

            bool badRegion = false;
            walker.Analyze(ref badRegion);
            Debug.Assert(!badRegion);

            if (!method.IsStatic && method.ContainingType.TypeKind == TypeKind.Struct)
            {
                // It is possible that the enclosing method only *writes* to the enclosing struct, but in that
                // case it should be considered captured anyway so that we have a proxy for it to write to.
                walker.CaptureVariable(method.ThisParameter, node.Syntax);
            }

            var lazyDisallowedCaptures = walker._lazyDisallowedCaptures;
            var allVariables = walker.variableBySlot;

            if (lazyDisallowedCaptures != null)
            {
                foreach (var kvp in lazyDisallowedCaptures)
                {
                    var variable = kvp.Key;
                    var type = (variable.Kind == SymbolKind.Local) ? ((LocalSymbol)variable).Type : ((ParameterSymbol)variable).Type;

                    if (variable is SynthesizedLocal local && local.SynthesizedKind == SynthesizedLocalKind.Spill)
                    {
                        Debug.Assert(local.TypeWithAnnotations.IsRestrictedType());
                        diagnostics.Add(ErrorCode.ERR_ByRefTypeAndAwait, local.GetFirstLocation(), local.TypeWithAnnotations);
                    }
                    else
                    {
                        foreach (CSharpSyntaxNode syntax in kvp.Value)
                        {
                            // CS4013: Instance of type '{0}' cannot be used inside an anonymous function, query expression, iterator block or async method
                            diagnostics.Add(ErrorCode.ERR_SpecialByRefInLambda, syntax.Location, type);
                        }
                    }
                }
            }

            var variablesToHoist = new OrderedSet<Symbol>();
            if (compilation.Options.OptimizationLevel != OptimizationLevel.Release)
            {
                // In debug build we hoist long-lived locals and parameters
                foreach (var v in allVariables)
                {
                    var symbol = v.Symbol;
                    if ((object)symbol != null && HoistInDebugBuild(symbol))
                    {
                        variablesToHoist.Add(symbol);
                    }
                }
            }

            // Hoist anything determined to be live across an await or yield
            variablesToHoist.AddRange(walker._variablesToHoist);

            walker.Free();

            return variablesToHoist;
        }

        private static bool HoistInDebugBuild(Symbol symbol)
        {
            return (symbol) switch
            {
                ParameterSymbol parameter =>
                    // in Debug build hoist all parameters that can be hoisted:
                    !parameter.Type.IsRestrictedType(),
                LocalSymbol { IsConst: false, IsPinned: false, IsRef: false } local =>
                    // hoist all user-defined locals and long-lived temps that can be hoisted:
                    local.SynthesizedKind.MustSurviveStateMachineSuspension() &&
                    !local.Type.IsRestrictedType(),
                _ => false
            };
        }

        private void MarkLocalsUnassigned()
        {
            for (int i = 0; i < variableBySlot.Count; i++)
            {
                var symbol = variableBySlot[i].Symbol;

                if ((object)symbol != null)
                {
                    switch (symbol.Kind)
                    {
                        case SymbolKind.Local:
                            if (!((LocalSymbol)symbol).IsConst)
                            {
                                SetSlotState(i, false);
                            }
                            break;

                        case SymbolKind.Parameter:
                            SetSlotState(i, false);
                            break;

                        case SymbolKind.Field:
                            if (!((FieldSymbol)symbol).IsConst)
                            {
                                SetSlotState(i, false);
                            }
                            break;

                        default:
                            throw ExceptionUtilities.UnexpectedValue(symbol.Kind);
                    }
                }
            }
        }

        public override BoundNode VisitAwaitExpression(BoundAwaitExpression node)
        {
            base.VisitAwaitExpression(node);
            MarkLocalsUnassigned();
            return null;
        }

        public override BoundNode VisitYieldReturnStatement(BoundYieldReturnStatement node)
        {
            base.VisitYieldReturnStatement(node);
            MarkLocalsUnassigned();
            _seenYieldInCurrentTry = true;
            return null;
        }

        protected override ImmutableArray<PendingBranch> Scan(ref bool badRegion)
        {
            _variablesToHoist.Clear();
            _lazyDisallowedCaptures?.Clear();

            return base.Scan(ref badRegion);
        }

        private void CaptureVariable(Symbol variable, SyntaxNode syntax)
        {
            var type = (variable.Kind == SymbolKind.Local) ? ((LocalSymbol)variable).Type : ((ParameterSymbol)variable).Type;
            if (type.IsRestrictedType())
            {
                (_lazyDisallowedCaptures ??= new MultiDictionary<Symbol, SyntaxNode>()).Add(variable, syntax);
            }
            else
            {
                if (_variablesToHoist.Add(variable) && variable is LocalSymbol local && _boundRefLocalInitializers.TryGetValue(local, out var variableInitializer))
                    CaptureRefInitializer(variableInitializer, syntax);
            }
        }

        private void CaptureRefInitializer(BoundExpression variableInitializer, SyntaxNode syntax)
        {
            switch (variableInitializer)
            {
                case BoundLocal { LocalSymbol: var symbol }:
                    CaptureVariable(symbol, syntax);
                    break;
                case BoundParameter { ParameterSymbol: var symbol }:
                    CaptureVariable(symbol, syntax);
                    break;
                case BoundFieldAccess { FieldSymbol: { IsStatic: false, ContainingType: { IsValueType: true } }, ReceiverOpt: BoundExpression receiver }:
                    CaptureRefInitializer(receiver, syntax);
                    break;
            }
        }

        protected override void EnterParameter(ParameterSymbol parameter)
        {
            // Async and iterators should never have ref parameters aside from `this`
            Debug.Assert(parameter.IsThis || parameter.RefKind == RefKind.None);

            // parameters are NOT initially assigned here - if that is a problem, then
            // the parameters must be captured.
            GetOrCreateSlot(parameter);
        }

        protected override void ReportUnassigned(Symbol symbol, SyntaxNode node, int slot, bool skipIfUseBeforeDeclaration)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Field:
                    symbol = GetNonMemberSymbol(slot);
                    goto case SymbolKind.Local;

                case SymbolKind.Local:
                case SymbolKind.Parameter:
                    CaptureVariable(symbol, node);
                    break;
            }
        }

        protected override void VisitLvalueParameter(BoundParameter node)
        {
            TryHoistTopLevelParameter(node);
            base.VisitLvalueParameter(node);
        }

        public override BoundNode VisitParameter(BoundParameter node)
        {
            TryHoistTopLevelParameter(node);
            return base.VisitParameter(node);
        }

        private void TryHoistTopLevelParameter(BoundParameter node)
        {
            if (node.ParameterSymbol.ContainingSymbol == topLevelMethod)
            {
                CaptureVariable(node.ParameterSymbol, node.Syntax);
            }
        }

        public override BoundNode VisitFieldAccess(BoundFieldAccess node)
        {
            if (node.ReceiverOpt != null && node.ReceiverOpt.Kind == BoundKind.ThisReference)
            {
                var thisSymbol = topLevelMethod.ThisParameter;
                CaptureVariable(thisSymbol, node.Syntax);
            }

            return base.VisitFieldAccess(node);
        }

        public override BoundNode VisitThisReference(BoundThisReference node)
        {
            CaptureVariable(topLevelMethod.ThisParameter, node.Syntax);
            return base.VisitThisReference(node);
        }

        public override BoundNode VisitBaseReference(BoundBaseReference node)
        {
            CaptureVariable(topLevelMethod.ThisParameter, node.Syntax);
            return base.VisitBaseReference(node);
        }

        public override BoundNode VisitTryStatement(BoundTryStatement node)
        {
            var origSeenYieldInCurrentTry = _seenYieldInCurrentTry;
            _seenYieldInCurrentTry = false;
            base.VisitTryStatement(node);
            _seenYieldInCurrentTry |= origSeenYieldInCurrentTry;
            return null;
        }

        protected override void VisitFinallyBlock(BoundStatement finallyBlock, ref LocalState unsetInFinally)
        {
            if (_seenYieldInCurrentTry)
            {
                // Locals cannot be used to communicate between the finally block and the rest of the method.
                // So we just capture any outside variables that are used inside.
                new OutsideVariablesUsedInside(this, this.topLevelMethod, this).Visit(finallyBlock);
            }

            base.VisitFinallyBlock(finallyBlock, ref unsetInFinally);
        }

        public override BoundNode VisitAssignmentOperator(BoundAssignmentOperator node)
        {
            base.VisitAssignmentOperator(node);
            // for compiler-generated ref local temp, save the initializer.
            if (node is { IsRef: true, Left: BoundLocal { LocalSymbol: LocalSymbol { IsCompilerGenerated: true } local } })
                _boundRefLocalInitializers[local] = node.Right;
            return null;
        }

        private sealed class OutsideVariablesUsedInside : BoundTreeWalkerWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator
        {
            private readonly HashSet<Symbol> _localsInScope;
            private readonly IteratorAndAsyncCaptureWalker _analyzer;
            private readonly MethodSymbol _topLevelMethod;
            private readonly IteratorAndAsyncCaptureWalker _parent;

            public OutsideVariablesUsedInside(IteratorAndAsyncCaptureWalker analyzer, MethodSymbol topLevelMethod, IteratorAndAsyncCaptureWalker parent)
                : base(parent._recursionDepth)
            {
                _analyzer = analyzer;
                _topLevelMethod = topLevelMethod;
                _localsInScope = new HashSet<Symbol>();
                _parent = parent;
            }

            protected override bool ConvertInsufficientExecutionStackExceptionToCancelledByStackGuardException()
            {
                return _parent.ConvertInsufficientExecutionStackExceptionToCancelledByStackGuardException();
            }

            public override BoundNode VisitBlock(BoundBlock node)
            {
                AddVariables(node.Locals);
                return base.VisitBlock(node);
            }

            private void AddVariables(ImmutableArray<LocalSymbol> locals)
            {
                foreach (var local in locals)
                {
                    AddVariable(local);
                }
            }

            public override BoundNode VisitCatchBlock(BoundCatchBlock node)
            {
                AddVariables(node.Locals);
                return base.VisitCatchBlock(node);
            }

            private void AddVariable(Symbol local)
            {
                if ((object)local != null) _localsInScope.Add(local);
            }

            public override BoundNode VisitSequence(BoundSequence node)
            {
                AddVariables(node.Locals);
                return base.VisitSequence(node);
            }

            public override BoundNode VisitThisReference(BoundThisReference node)
            {
                Capture(_topLevelMethod.ThisParameter, node.Syntax);
                return base.VisitThisReference(node);
            }

            public override BoundNode VisitBaseReference(BoundBaseReference node)
            {
                Capture(_topLevelMethod.ThisParameter, node.Syntax);
                return base.VisitBaseReference(node);
            }

            public override BoundNode VisitLocal(BoundLocal node)
            {
                Capture(node.LocalSymbol, node.Syntax);
                return base.VisitLocal(node);
            }

            public override BoundNode VisitParameter(BoundParameter node)
            {
                Capture(node.ParameterSymbol, node.Syntax);
                return base.VisitParameter(node);
            }

            private void Capture(Symbol s, SyntaxNode syntax)
            {
                if ((object)s != null && !_localsInScope.Contains(s))
                {
                    _analyzer.CaptureVariable(s, syntax);
                }
            }
        }
    }
}
