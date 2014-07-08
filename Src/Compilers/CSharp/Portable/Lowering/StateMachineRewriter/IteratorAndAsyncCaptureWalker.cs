// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A walker that computes the set of local variables of an iterator
    /// method that must be moved to fields of the generated class.
    /// </summary>
    internal sealed class IteratorAndAsyncCaptureWalker : DataFlowPass
    {
        // The analyzer collects captured variables and their usages. The syntax locations are used to report errors.
        private readonly MultiDictionary<Symbol, CSharpSyntaxNode> variablesCaptured = new MultiDictionary<Symbol, CSharpSyntaxNode>();

        private readonly Dictionary<LocalSymbol, BoundExpression> refLocalInitializers = new Dictionary<LocalSymbol, BoundExpression>();
        private bool seenYieldInCurrentTry = false;

        private IteratorAndAsyncCaptureWalker(CSharpCompilation compilation, MethodSymbol method, BoundNode node, CaptureWalkerEmptyStructTypeCache emptyStructCache, HashSet<Symbol> initiallyAssignedVariables)
            : base(compilation, 
                  method, 
                  node, 
                  emptyStructCache, 
                  trackUnassignments: true, 
                  initiallyAssignedVariables: initiallyAssignedVariables)
        {
        }

        public static MultiDictionary<Symbol, CSharpSyntaxNode> Analyze(CSharpCompilation compilation, MethodSymbol method, BoundNode node)
        {
            var emptyStructs = new CaptureWalkerEmptyStructTypeCache();
            var initiallyAssignedVariables = UnassignedVariablesWalker.Analyze(compilation, method, node, emptyStructs);

            var walker = new IteratorAndAsyncCaptureWalker(compilation, method, node, emptyStructs, initiallyAssignedVariables);

            bool badRegion = false;
            walker.Analyze(ref badRegion);
            Debug.Assert(!badRegion);

            var result = walker.variablesCaptured;


            if (!method.IsStatic && method.ContainingType.TypeKind == TypeKind.Struct)
            {
                // It is possible that the enclosing method only *writes* to the enclosing struct, but in that
                // case it should be considered captured anyway so that we have a proxy for it to write to.
                result.Add(method.ThisParameter, node.Syntax);
            }

            foreach (var variable in result.Keys.ToArray()) // take a snapshot, as we are modifying the underlying multidictionary
            {
                var local = variable as LocalSymbol;
                if ((object)local != null && local.RefKind != RefKind.None)
                {
                    walker.AddSpillsForRef(walker.refLocalInitializers[local], result[local]);
                }
            }

            walker.Free();
            return result;
        }

        /// <summary>
        /// If a ref variable is to be spilled, sometimes that causes us to need to spill
        /// the thing the ref variable was initialized with.  For example, if the variable
        /// was initialized with "structVariable.field", then the struct variable needs to
        /// be spilled.  This method adds to the spill set things that need to be spilled
        /// based on the given refInitializer expression.
        /// </summary>
        private void AddSpillsForRef(BoundExpression refInitializer, IEnumerable<CSharpSyntaxNode> locations)
        {
            while (true)
            {
                if (refInitializer == null) return;
                switch (refInitializer.Kind)
                {
                    case BoundKind.Local:
                        var local = (BoundLocal)refInitializer;
                        if (!variablesCaptured.ContainsKey(local.LocalSymbol))
                        {
                            foreach (var loc in locations)
                            {
                                variablesCaptured.Add(local.LocalSymbol, loc);
                            }

                            if (local.LocalSymbol.RefKind != RefKind.None)
                            {
                                refInitializer = refLocalInitializers[local.LocalSymbol];
                                continue;
                            }
                        }
                        return;

                    case BoundKind.FieldAccess:
                        var field = (BoundFieldAccess)refInitializer;
                        if (!field.FieldSymbol.IsStatic && field.FieldSymbol.ContainingType.IsValueType)
                        {
                            refInitializer = field.ReceiverOpt;
                            continue;
                        }
                        return;

                    default:
                        return;
                }
            }
        }

        public override BoundNode VisitAssignmentOperator(BoundAssignmentOperator node)
        {
            if (node.Left.Kind == BoundKind.Local && node.RefKind != RefKind.None)
            {
                var localSymbol = ((BoundLocal)node.Left).LocalSymbol;
                Debug.Assert(localSymbol.RefKind != RefKind.None);
                refLocalInitializers.Add(localSymbol, node.Right);
            }

            return base.VisitAssignmentOperator(node);
        }

        protected override ImmutableArray<PendingBranch> Scan(ref bool badRegion)
        {
            variablesCaptured.Clear();
            refLocalInitializers.Clear();
            return base.Scan(ref badRegion);
        }

        protected override void EnterParameter(ParameterSymbol parameter)
        {
            // parameters are NOT intitially assigned here - if that is a problem, then
            // the parameters must be captured.
            MakeSlot(parameter);
        }

        protected override void ReportUnassigned(Symbol symbol, CSharpSyntaxNode node)
        {
            if (symbol is LocalSymbol || symbol is ParameterSymbol)
            {
                variablesCaptured.Add(symbol, node);
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
            variablesCaptured.Add(GetNonFieldSymbol(unassignedSlot), node);
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
                variablesCaptured.Add(node.ParameterSymbol, node.Syntax);
            }
        }

        public override BoundNode VisitFieldAccess(BoundFieldAccess node)
        {
            if (node.ReceiverOpt != null && node.ReceiverOpt.Kind == BoundKind.ThisReference)
            {
                var thisSymbol = topLevelMethod.ThisParameter;
                variablesCaptured.Add(thisSymbol, node.Syntax);
            }

            return base.VisitFieldAccess(node);
        }

        public override BoundNode VisitThisReference(BoundThisReference node)
        {
            variablesCaptured.Add(topLevelMethod.ThisParameter, node.Syntax);
            return base.VisitThisReference(node);
        }

        public override BoundNode VisitBaseReference(BoundBaseReference node)
        {
            variablesCaptured.Add(topLevelMethod.ThisParameter, node.Syntax);
            return base.VisitBaseReference(node);
        }

        private void MarkLocalsUnassigned()
        {
            for (int i = 0; i < nextVariableSlot; i++)
            {
                var symbol = variableBySlot[i].Symbol;
                var local = symbol as LocalSymbol;
                if ((object)local != null && !local.IsConst)
                {
                    SetSlotState(i, false);
                    continue;
                }

                var parameter = symbol as ParameterSymbol;
                if ((object)parameter != null)
                {
                    SetSlotState(i, false);
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
            seenYieldInCurrentTry = true;
            return null;
        }

        public override BoundNode VisitTryStatement(BoundTryStatement node)
        {
            var origSeenYieldInCurrentTry = this.seenYieldInCurrentTry;
            this.seenYieldInCurrentTry = false;
            base.VisitTryStatement(node);
            this.seenYieldInCurrentTry |= origSeenYieldInCurrentTry;
            return null;
        }

        protected override void VisitFinallyBlock(BoundStatement finallyBlock, ref LocalState unsetInFinally)
        {
            if (seenYieldInCurrentTry)
            {
                // Locals cannot be used to communicate between the finally block and the rest of the method.
                // So we just capture any outside variables that are used inside.
                new OutsideVariablesUsedInside(variablesCaptured, this.topLevelMethod).Visit(finallyBlock);
            }

            base.VisitFinallyBlock(finallyBlock, ref unsetInFinally);
        }

        private sealed class OutsideVariablesUsedInside : BoundTreeWalker
    {
        private HashSet<Symbol> localsInScope = new HashSet<Symbol>();
        private readonly MultiDictionary<Symbol, CSharpSyntaxNode> variablesCaptured;
        private readonly MethodSymbol topLevelMethod;

        public OutsideVariablesUsedInside(MultiDictionary<Symbol, CSharpSyntaxNode> variablesCaptured, MethodSymbol topLevelMethod)
        {
            this.variablesCaptured = variablesCaptured;
            this.topLevelMethod = topLevelMethod;
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
            if ((object)local != null) localsInScope.Add(local);
        }

        public override BoundNode VisitSequence(BoundSequence node)
        {
            AddVariables(node.Locals);
            return base.VisitSequence(node);
        }

        public override BoundNode VisitThisReference(BoundThisReference node)
        {
            Capture(this.topLevelMethod.ThisParameter, node.Syntax);
            return base.VisitThisReference(node);
        }

        public override BoundNode VisitBaseReference(BoundBaseReference node)
        {
            Capture(this.topLevelMethod.ThisParameter, node.Syntax);
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
            if ((object)s != null && !localsInScope.Contains(s))
            {
                this.variablesCaptured.Add(s, syntax);
            }
        }
    }
}
}