// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
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
    internal sealed class IteratorAndAsyncCaptureWalker : DataFlowPass
    {
        // In Release builds we hoist only variables (locals and parameters) that are captured. 
        // This set will contain such variables after the bound tree is visited.
        private readonly OrderedSet<Symbol> _variablesToHoist;

        // Contains variables that are captured but can't be hoisted since their type can't be allocated on heap.
        // The value is a list of all uses of each such variable.
        private MultiDictionary<Symbol, CSharpSyntaxNode> _lazyDisallowedCaptures;

        private bool _seenYieldInCurrentTry;

        private IteratorAndAsyncCaptureWalker(CSharpCompilation compilation, MethodSymbol method, BoundNode node, NeverEmptyStructTypeCache emptyStructCache, HashSet<Symbol> initiallyAssignedVariables)
            : base(compilation,
                  method,
                  node,
                  emptyStructCache,
                  trackUnassignments: true,
                  initiallyAssignedVariables: initiallyAssignedVariables)
        {
            _variablesToHoist = new OrderedSet<Symbol>();
        }

        // Returns deterministically ordered list of variables that ought to be hoisted.
        public static OrderedSet<Symbol> Analyze(CSharpCompilation compilation, MethodSymbol method, BoundNode node, DiagnosticBag diagnostics)
        {
            var initiallyAssignedVariables = UnassignedVariablesWalker.Analyze(compilation, method, node, convertInsufficientExecutionStackExceptionToCancelledByStackGuardException:true);
            var walker = new IteratorAndAsyncCaptureWalker(compilation, method, node, new NeverEmptyStructTypeCache(), initiallyAssignedVariables);

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

            var variablesToHoist = walker._variablesToHoist;
            var lazyDisallowedCaptures = walker._lazyDisallowedCaptures;
            var allVariables = walker.variableBySlot;

            walker.Free();

            if (lazyDisallowedCaptures != null)
            {
                foreach (var kvp in lazyDisallowedCaptures)
                {
                    var variable = kvp.Key;
                    var type = (variable.Kind == SymbolKind.Local) ? ((LocalSymbol)variable).Type.TypeSymbol : ((ParameterSymbol)variable).Type.TypeSymbol;

                    foreach (CSharpSyntaxNode syntax in kvp.Value)
                    {
                        // CS4013: Instance of type '{0}' cannot be used inside an anonymous function, query expression, iterator block or async method
                        diagnostics.Add(ErrorCode.ERR_SpecialByRefInLambda, syntax.Location, type);
                    }
                }
            }

            if (compilation.Options.OptimizationLevel != OptimizationLevel.Release)
            {
                Debug.Assert(variablesToHoist.Count == 0);

                // In debug build we hoist all locals and parameters:
                variablesToHoist.AddRange(from v in allVariables
                                          where v.Symbol != null && HoistInDebugBuild(v.Symbol)
                                          select v.Symbol);
            }

            return variablesToHoist;
        }

        private static bool HoistInDebugBuild(Symbol symbol)
        {
            // in Debug build hoist all parameters that can be hoisted:
            if (symbol.Kind == SymbolKind.Parameter)
            {
                var parameter = (ParameterSymbol)symbol;
                return !parameter.Type.IsRestrictedType();
            }

            if (symbol.Kind == SymbolKind.Local)
            {
                LocalSymbol local = (LocalSymbol)symbol;

                if (local.IsConst)
                {
                    return false;
                }

                // hoist all user-defined locals that can be hoisted:
                if (local.SynthesizedKind == SynthesizedLocalKind.UserDefined)
                {
                    return !local.Type.IsRestrictedType();
                }

                // hoist all synthesized variables that have to survive state machine suspension:
                return local.SynthesizedKind.MustSurviveStateMachineSuspension();
            }

            return false;
        }

        private void MarkLocalsUnassigned()
        {
            for (int i = 0; i < nextVariableSlot; i++)
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

        private void CaptureVariable(Symbol variable, CSharpSyntaxNode syntax)
        {
            var type = (variable.Kind == SymbolKind.Local) ? ((LocalSymbol)variable).Type.TypeSymbol : ((ParameterSymbol)variable).Type.TypeSymbol;
            if (type.IsRestrictedType())
            {
                // error has already been reported:
                if (variable is SynthesizedLocal)
                {
                    return;
                }

                if (_lazyDisallowedCaptures == null)
                {
                    _lazyDisallowedCaptures = new MultiDictionary<Symbol, CSharpSyntaxNode>();
                }

                _lazyDisallowedCaptures.Add(variable, syntax);
            }
            else if (compilation.Options.OptimizationLevel == OptimizationLevel.Release)
            {
                _variablesToHoist.Add(variable);
            }
        }

        protected override void EnterParameter(ParameterSymbol parameter)
        {
            // parameters are NOT initially assigned here - if that is a problem, then
            // the parameters must be captured.
            GetOrCreateSlot(parameter);
        }

        protected override void ReportUnassigned(Symbol symbol, CSharpSyntaxNode node)
        {
            if (symbol is LocalSymbol || symbol is ParameterSymbol)
            {
                CaptureVariable(symbol, node);
            }
        }

        // The iterator transformation causes some unreachable code to become
        // reachable from the code gen's point of view, so we analyze the unreachable code too.
        protected override LocalState UnreachableState()
        {
            return this.State;
        }

        protected override void ReportUnassigned(FieldSymbol fieldSymbol, int unassignedSlot, CSharpSyntaxNode node)
        {
            CaptureVariable(GetNonMemberSymbol(unassignedSlot), node);
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
                AddVariable(node.LocalOpt);
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

            private void Capture(Symbol s, CSharpSyntaxNode syntax)
            {
                if ((object)s != null && !_localsInScope.Contains(s))
                {
                    _analyzer.CaptureVariable(s, syntax);
                }
            }
        }
    }
}
