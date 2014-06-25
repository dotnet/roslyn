// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal class DisposeCheckerPass : PreciseAbstractFlowPass<DisposeCheckerPass.LocalState>
    {
        private readonly TypeSymbol IDisposableType;
        private readonly HashSet<CSharpSyntaxNode> reported = new HashSet<CSharpSyntaxNode>();

        internal DisposeCheckerPass(CSharpCompilation compilation, MethodSymbol method, BoundNode node)
            : base(compilation, method, node, trackExceptions: true)
        {
            this.IDisposableType = compilation.GetSpecialType(SpecialType.System_IDisposable);
        }

        internal static void Analyze(CSharpCompilation compilation, MethodSymbol method, BoundNode node, DiagnosticBag diagnostics)
        {
            if (compilation.Feature("checkdispose") == null)
            {
                return;
            }

            Debug.Assert(diagnostics != null);
            var walker = new DisposeCheckerPass(compilation, method, node);
            try
            {
                bool badRegion = false;
                var returns = walker.Analyze(ref badRegion);
                Debug.Assert(!badRegion);
                walker.AnalyzeResult(returns);
                if (walker.Diagnostics != null) diagnostics.AddRange(walker.Diagnostics);
            }
            finally
            {
                walker.Free();
            }
        }

        protected override ImmutableArray<PendingBranch> Scan(ref bool badRegion)
        {
            reported.Clear();
            return base.Scan(ref badRegion);
        }

        private void ReportUndisposed(LocalState state, ErrorCode code)
        {
            if (state.Reachable)
            {
                foreach (var missingDispose in state.possiblyUndisposedCreations)
                {
                    if (reported.Add(missingDispose.Syntax))
                    {
                        Diagnostics.Add(code, missingDispose.Syntax.Location, missingDispose.Type);
                    }
                }
            }
        }

        private void AnalyzeResult(ImmutableArray<PendingBranch> returns)
        {
            foreach (var branch in returns)
            {
                ReportUndisposed(branch.State, ErrorCode.WRN_CA2000_DisposeObjectsBeforeLosingScope1);
            }

            ReportUndisposed(this.State, ErrorCode.WRN_CA2000_DisposeObjectsBeforeLosingScope1);

            var pending = this.SavePending();
            ReportUndisposed(pending.PendingBranches[0].State, ErrorCode.WRN_CA2000_DisposeObjectsBeforeLosingScope2);
            RestorePending(pending);
        }

        private const BoundKind HijackedBoundKindForValueHolder = BoundKind.SequencePoint;

        internal class Value
        {
            // the set of (possibly undisposed) creations that are definitely contained by this value
            public readonly HashSet<BoundExpression> creations;

            public Value(HashSet<BoundExpression> creations)
            {
                this.creations = creations;
            }

            public Value(BoundExpression expr, DisposeCheckerPass pass)
            {
                Debug.Assert(expr.Kind != HijackedBoundKindForValueHolder);
                this.creations = new HashSet<BoundExpression>();
                expr = SkipReferenceConversions(expr);
                switch (expr.Kind)
                {
                    case HijackedBoundKindForValueHolder:
                        {
                            var holder = (BoundValueHolder)expr;
                            var value = holder.value;
                            creations = value.creations;
                        }
                        break;
                    case BoundKind.ObjectCreationExpression:
                    case BoundKind.NewT:
                        HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                        if (Conversions.IsBaseInterface(pass.IDisposableType, expr.Type, ref useSiteDiagnostics))
                        {
                            creations.Add(expr);
                        }
                        break;
                    case BoundKind.Local:
                        {
                            var local = (BoundLocal)expr;
                            Value value;
                            pass.State.variables.TryGetValue(local.LocalSymbol, out value);
                            if (value != null)
                            {
                                creations = value.creations;
                            }
                        }
                        break;
                    case BoundKind.DeclarationExpression:
                        {
                            var local = (BoundDeclarationExpression)expr;
                            Value value;
                            pass.State.variables.TryGetValue(local.LocalSymbol, out value);
                            if (value != null)
                            {
                                creations = value.creations;
                            }
                        }
                        break;
                    case BoundKind.Parameter:
                        {
                            var parameter = (BoundParameter)expr;
                            Value value;
                            pass.State.variables.TryGetValue(parameter.ParameterSymbol, out value);
                            if (value != null)
                            {
                                creations = value.creations;
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        internal class LocalState : DisposeCheckerPass.AbstractLocalState
        {
            private readonly bool reachable;
            internal HashSet<BoundExpression> possiblyUndisposedCreations;
            internal HashSet<BoundExpression> possiblyDisposedCreations;
            internal Dictionary<Symbol, Value> variables;

            private LocalState(
                bool reachable,
                HashSet<BoundExpression> possiblyUndisposedCreations = null,
                HashSet<BoundExpression> possiblyDisposedCreations = null,
                Dictionary<Symbol, Value> variables = null)
            {
                this.reachable = reachable;
                this.possiblyUndisposedCreations = possiblyUndisposedCreations ?? new HashSet<BoundExpression>();
                this.possiblyDisposedCreations = possiblyDisposedCreations ?? new HashSet<BoundExpression>();
                this.variables = variables ?? new Dictionary<Symbol, Value>();
            }

            internal static LocalState UnreachableState()
            {
                return new LocalState(reachable: false);
            }

            internal static LocalState ReachableState()
            {
                return new LocalState(reachable: true);
            }

            public LocalState Clone()
            {
                return new LocalState(
                    reachable,
                    new HashSet<BoundExpression>(this.possiblyUndisposedCreations),
                    new HashSet<BoundExpression>(this.possiblyDisposedCreations),
                    new Dictionary<Symbol, Value>(this.variables));
            }

            public bool Reachable
            {
                get
                {
                    return this.reachable;
                }
            }

            internal bool IntersectWith(LocalState other)
            {
                Debug.Assert(this.Reachable);
                Debug.Assert(other.Reachable);
                var variables = new Dictionary<Symbol, Value>();
                var locals = new HashSet<Symbol>(other.variables.Keys);
                bool changed = false;
                foreach (var l in this.variables.Keys) locals.Add(l);
                foreach (var variable in locals)
                {
                    Value myValue;
                    this.variables.TryGetValue(variable, out myValue);
                    Value otherValue;
                    other.variables.TryGetValue(variable, out otherValue);
                    Value combined;
                    if (myValue == null)
                    {
                        combined = otherValue;
                        changed = true;
                    }
                    else if (otherValue == null)
                    {
                        combined = myValue;
                    }
                    else
                    {
                        var creations = new HashSet<BoundExpression>();
                        foreach (var c in myValue.creations)
                        {
                            if (true /* || otherValue.creations.Contains(c) || !other.possiblyUndisposedCreations.Contains(c)*/)
                            {
                                creations.Add(c);
                            }
                        }
                        foreach (var c in otherValue.creations)
                        {
                            if (true /* || myValue.creations.Contains(c) || !this.possiblyUndisposedCreations.Contains(c)*/)
                            {
                                if (creations.Add(c)) changed = true;
                            }
                        }

                        combined = changed ? new Value(creations) : myValue;
                    }

                    variables.Add(variable, combined);
                }

                var possiblyUndisposedCreations = new HashSet<BoundExpression>(this.possiblyUndisposedCreations);
                foreach (var uc in other.possiblyUndisposedCreations)
                {
                    if (possiblyUndisposedCreations.Add(uc))
                    {
                        changed = true;
                    }
                }

                var possiblyDisposedCreations = new HashSet<BoundExpression>(this.possiblyDisposedCreations);
                foreach (var uc in other.possiblyDisposedCreations)
                {
                    if (possiblyDisposedCreations.Add(uc))
                    {
                        changed = true;
                    }
                }

                if (changed)
                {
                    this.possiblyUndisposedCreations = possiblyUndisposedCreations;
                    this.possiblyDisposedCreations = possiblyDisposedCreations;
                    this.variables = variables;
                }

                return changed;
            }
        }

        protected override LocalState ReachableState()
        {
            return LocalState.ReachableState();
        }

        protected override LocalState UnreachableState()
        {
            return LocalState.UnreachableState();
        }

        protected override bool IntersectWith(ref LocalState self, ref LocalState other)
        {
            if (!other.Reachable) return false;
            if (!self.Reachable)
            {
                self = other.Clone();
                return true;
            }

            return self.IntersectWith(other);
        }

        protected override string Dump(LocalState state)
        {
            var b = new StringBuilder();
            b.Append("DisposeCheckerPass.LocalState[reachable: " + state.Reachable);
            b.Append("; possiblyUndisposed: " + state.possiblyUndisposedCreations);
            b.Append("; possiblyDisposed: " + state.possiblyDisposedCreations);
            b.Append("]");
            return b.ToString();
        }

        public override BoundNode VisitObjectCreationExpression(BoundObjectCreationExpression node)
        {
            base.VisitObjectCreationExpression(node);
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            if (Conversions.IsBaseInterface(IDisposableType, node.Type, ref useSiteDiagnostics))
            {
                if (!this.State.possiblyUndisposedCreations.Add(node) && reported.Add(node.Syntax))
                {
                    Diagnostics.Add(ErrorCode.WRN_CA2000_DisposeObjectsBeforeLosingScope1, node.Syntax.Location, node.Type);
                }

                return node;
            }

            return null;
        }

        public override BoundNode VisitNewT(BoundNewT node)
        {
            base.VisitNewT(node);
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            if (Conversions.IsBaseInterface(IDisposableType, node.Type, ref useSiteDiagnostics))
            {
                if (!this.State.possiblyUndisposedCreations.Add(node) && reported.Add(node.Syntax))
                {
                    Diagnostics.Add(ErrorCode.WRN_CA2000_DisposeObjectsBeforeLosingScope1, node.Syntax.Location, node.Type);
                }

                return node;
            }

            return null;
        }

        class BoundValueHolder : BoundExpression
        {
            public readonly Value value;
            public BoundValueHolder(Value value, TypeSymbol type, CSharpSyntaxNode syntax)
                : base(HijackedBoundKindForValueHolder, syntax, type)
            {
                this.value = value;
            }

            public BoundValueHolder(BoundExpression expr, DisposeCheckerPass pass)
                : base(HijackedBoundKindForValueHolder, expr.Syntax, expr.Type)
            {
                Debug.Assert(expr.Kind != HijackedBoundKindForValueHolder);
                this.value = new Value(expr, pass);
            }
        }

        private Value MakeValue(BoundExpression expr)
        {
            if (expr == null)
            {
                return new Value(new HashSet<BoundExpression>());
            }

            var v = expr as BoundValueHolder;
            return (v != null) ? v.value : new Value(expr, this);
        }

        private BoundValueHolder MakeValueHolder(BoundExpression expr)
        {
            if (expr == null)
            {
                return new BoundValueHolder(new Value(new HashSet<BoundExpression>()), null, null);
            }

            var result = expr as BoundValueHolder;
            return result ?? new BoundValueHolder(expr, this);
        }

        protected override void PropertySetter(BoundExpression node, BoundExpression receiver, MethodSymbol setter, BoundExpression value = null)
        {
            if (value != null)
            {
                foreach (var e in MakeValue(value).creations) // assigning to a property is considered to give up responsibility for disposing
                {
                    this.State.possiblyUndisposedCreations.Remove(e);
                }
            }

            base.PropertySetter(node, receiver, setter, value);
        }

        public override BoundNode VisitAssignmentOperator(BoundAssignmentOperator node)
        {
            base.VisitAssignmentOperator(node);
            var rvalue = MakeValueHolder(node.Right);
            switch (node.Left.Kind)
            {
                case BoundKind.Local:
                    {
                        var left = (BoundLocal)node.Left;
                        this.State.variables[left.LocalSymbol] = rvalue.value;
                    }
                    break;
                case BoundKind.DeclarationExpression:
                    {
                        var left = (BoundDeclarationExpression)node.Left;
                        this.State.variables[left.LocalSymbol] = rvalue.value;
                    }
                    break;
                case BoundKind.Parameter:
                    {
                        var left = (BoundParameter)node.Left;
                        this.State.variables[left.ParameterSymbol] = rvalue.value;
                    }
                    break;
                case BoundKind.FieldAccess:
                case BoundKind.PropertyAccess:
                    {
                        foreach (var e in rvalue.value.creations)
                        {
                            this.State.possiblyUndisposedCreations.Remove(e);
                        }
                    }
                    break;
                default:
                    break;
            }

            return rvalue;
        }

        public override BoundNode VisitLocalDeclaration(BoundLocalDeclaration node)
        {
            if (node.InitializerOpt != null)
            {
                var rvalue = VisitRvalue(node.InitializerOpt) as BoundExpression;
                var valueHolder = MakeValueHolder(rvalue);
                this.State.variables[node.LocalSymbol] = valueHolder.value;
            }

            return null;
        }

        public override BoundNode VisitDeclarationExpression(BoundDeclarationExpression node)
        {
            if (node.InitializerOpt != null)
            {
                var rvalue = VisitRvalue(node.InitializerOpt) as BoundExpression;
                var valueHolder = MakeValueHolder(rvalue);
                this.State.variables[node.LocalSymbol] = valueHolder.value;
            }

            // Treat similar to BoundLocal. This might need an adjustment for a semicolon operators.
            base.VisitDeclarationExpression(node);
            Value result;
            this.State.variables.TryGetValue(node.LocalSymbol, out result);
            return result != null ? new BoundValueHolder(result, node.Type, node.Syntax) : null;
        }

        public override BoundNode VisitLocal(BoundLocal node)
        {
            base.VisitLocal(node);
            Value result;
            this.State.variables.TryGetValue(node.LocalSymbol, out result);
            return result != null ? new BoundValueHolder(result, node.Type, node.Syntax) : null;
        }

        public override BoundNode VisitParameter(BoundParameter node)
        {
            base.VisitParameter(node);
            Value result;
            this.State.variables.TryGetValue(node.ParameterSymbol, out result);
            return result != null ? new BoundValueHolder(result, node.Type, node.Syntax) : null;
        }

        public override BoundNode VisitReturnStatement(BoundReturnStatement node)
        {
            var result = base.VisitReturnStatement(node);
            // After processing a return statement, the very last pending branch is for that return statement.
            // If it is returning an allocated object, consider it to be disposed.

            if (result != null)
                switch (result.Kind)
                {
                    case HijackedBoundKindForValueHolder:
                        {
                            var holder = (BoundValueHolder)result;
                            var returnBranch = PendingBranches.Last();
                            foreach (var c in holder.value.creations)
                            {
                                returnBranch.State.possiblyUndisposedCreations.Remove(c);
                                if (returnBranch.State.possiblyDisposedCreations.Contains(c))
                                {
                                    // TODO: error: returning a value that may already have been disposed.
                                }
                            }
                            break;
                        }
                    case BoundKind.NewT:
                    case BoundKind.ObjectCreationExpression:
                        PendingBranches.Last().State.possiblyUndisposedCreations.Remove((BoundExpression)result);
                        break;
                    default:
                        break;
                }

            return result;
        }

        protected override void UpdateStateForCall(BoundCall node)
        {
            // Are we calling a dispose method?  For now, we just check if the method is named Dispose and protected or better.
            // TODO: we need a better way of determining if a method should be considered to dispose its receiver.
            bool isDispose = node.Method.Name == "Dispose" && node.Method.DeclaredAccessibility >= Accessibility.Protected;
            if (node.ReceiverOpt != null && isDispose)
            {
                Value v = MakeValue(node.ReceiverOpt);
                foreach (var e in v.creations)
                {
                    this.State.possiblyUndisposedCreations.Remove(e);
                    if (!this.State.possiblyDisposedCreations.Add(e))
                    {
                        Diagnostics.Add(ErrorCode.WRN_CA2202_DoNotDisposeObjectsMultipleTimes, node.Syntax.Location, e.Syntax.ToString());
                    }
                }
            }
            else if ((object)node.Method.AssociatedSymbol != null)
            {
                foreach (var a in node.Arguments)
                {
                    Value v = MakeValue(a);
                    foreach (var c in v.creations)
                    {
                        this.State.possiblyUndisposedCreations.Remove(c);
                    }
                }
            }

            base.UpdateStateForCall(node);
        }

        public override BoundNode VisitPropertyAccess(BoundPropertyAccess node)
        {
            return base.VisitPropertyAccess(node);
        }

        private static BoundExpression SkipReferenceConversions(BoundExpression expr)
        {
            while (true)
            {
                switch (expr.Kind)
                {
                    case BoundKind.Conversion:
                        var conversion = (BoundConversion)expr;
                        switch (conversion.ConversionKind)
                        {
                            case ConversionKind.ExplicitReference:
                            case ConversionKind.Identity:
                            case ConversionKind.ImplicitReference:
                            case ConversionKind.NullLiteral:
                                expr = conversion.Operand;
                                continue;
                            default:
                                return expr;
                        }
                    default:
                        return expr;
                }
            }
        }

        public override BoundNode VisitBinaryOperator(BoundBinaryOperator node)
        {
            var result = base.VisitBinaryOperator(node);
            switch (node.OperatorKind)
            {
                case BinaryOperatorKind.ObjectEqual:
                case BinaryOperatorKind.ObjectNotEqual:
                    var left = SkipReferenceConversions(node.Left);
                    var right = SkipReferenceConversions(node.Right);
                    if (left.IsLiteralNull())
                    {
                        var tmp = left;
                        left = right;
                        right = tmp;
                    }
                    if (right.IsLiteralNull())
                    {
                        Value v = MakeValue(left);
                        if (v.creations.Any())
                        {
                            Split();
                            foreach (var e in v.creations)
                            {
                                if (node.OperatorKind == BinaryOperatorKind.ObjectEqual)
                                {
                                    this.StateWhenTrue.possiblyUndisposedCreations.Remove(e);
                                }
                                else
                                {
                                    this.StateWhenFalse.possiblyUndisposedCreations.Remove(e);
                                }
                            }
                        }
                    }
                    break;
            }
            return result;
        }
    }
}

/*
 * 
 * TODO:
 * 
 * XX (1) When allocating a disposable object, record the possibly unallocated syntax in the state.
 * XX (2) Return the allocated object (syntax) from the appropriate visitor.
 * XX (3) Also pass them through the () operator.
 * XX (4) Handle the assignment operator, so it is known which variables contain which allocated objects.
 * XX (5) Handle the local variable read operator, so it is known the allocation(s) represented by the variable.
 * XX (6) A return statement is considered to dispose the object(s) returned.
 * XX (7) Pattern match for a call to Dispose().  Handle some common variants, such as a virtual
 *     method by that name (with and without a bool parameter).
 * XX (8) Handle allocations in loops.  If the object is in the undisposed set, give an error at the allocation site.
 * XX (9) Handle the null test for variables referring to undisposed objects.
 * XX (10) Handle a cast of a value to some other type (e.g. object, IDisposable, etc)
 * XX (11) Handle an initializing declaration.
 * (12) Assigning a value to a field is considered to have "saved" it
 * (13) Assigning a value to a property is considered to have "saved" it.
 * (14) Learn about the destructor declaration syntax ~T() and its relationship to these checks.
 * (-) Test, test, test.
 */
