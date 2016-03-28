// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Semantics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class AwaitExpressionSpiller : BoundTreeRewriterWithStackGuard
    {
        private const BoundKind SpillSequenceBuilder = BoundKind.SequencePoint; // NOTE: this bound kind is hijacked during this phase to represent BoundSpillSequenceBuilder

        private readonly SyntheticBoundNodeFactory _F;
        private readonly PooledDictionary<LocalSymbol, LocalSymbol> _tempSubstitution;

        private AwaitExpressionSpiller(MethodSymbol method, CSharpSyntaxNode syntaxNode, TypeCompilationState compilationState, PooledDictionary<LocalSymbol, LocalSymbol> tempSubstitution, DiagnosticBag diagnostics)
        {
            _F = new SyntheticBoundNodeFactory(method, syntaxNode, compilationState, diagnostics);
            _tempSubstitution = tempSubstitution;
        }

        private sealed class BoundSpillSequenceBuilder : BoundExpression
        {
            public readonly BoundExpression Value;

            private ArrayBuilder<LocalSymbol> _locals;
            private ArrayBuilder<BoundStatement> _statements;

            public BoundSpillSequenceBuilder(BoundExpression value = null)
                : base(SpillSequenceBuilder, null, value?.Type)
            {
                Debug.Assert(value == null || value.Kind != SpillSequenceBuilder);
                this.Value = value;
            }

            public bool HasStatements
            {
                get
                {
                    return _statements != null;
                }
            }

            public bool HasLocals
            {
                get
                {
                    return _locals != null;
                }
            }

            protected override OperationKind ExpressionKind => OperationKind.None;

            public override void Accept(OperationVisitor visitor)
            {
                throw ExceptionUtilities.Unreachable;
            }

            public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            {
                throw ExceptionUtilities.Unreachable;
            }

            public ImmutableArray<LocalSymbol> GetLocals()
            {
                return (_locals == null) ? ImmutableArray<LocalSymbol>.Empty : _locals.ToImmutable();
            }

            public ImmutableArray<BoundStatement> GetStatements(LocalSubstituter substituterOpt = null)
            {
                if (_statements == null)
                {
                    return ImmutableArray<BoundStatement>.Empty;
                }

                if (substituterOpt == null)
                {
                    return _statements.ToImmutable();
                }

                return _statements.SelectAsArray((statement, substituter) => (BoundStatement)substituter.Visit(statement), substituterOpt);
            }

            internal BoundSpillSequenceBuilder Update(BoundExpression value)
            {
                var result = new BoundSpillSequenceBuilder(value);
                result._locals = _locals;
                result._statements = _statements;
                return result;
            }

            public void Free()
            {
                if (_locals != null) _locals.Free();
                if (_statements != null) _statements.Free();
            }

            internal void Include(BoundSpillSequenceBuilder other)
            {
                if (other != null)
                {
                    IncludeAndFree(ref _locals, ref other._locals);
                    IncludeAndFree(ref _statements, ref other._statements);
                }
            }

            private static void IncludeAndFree<T>(ref ArrayBuilder<T> left, ref ArrayBuilder<T> right)
            {
                if (right == null)
                {
                    return;
                }

                if (left == null)
                {
                    left = right;
                    return;
                }

                left.AddRange(right);
                right.Free();
            }

            public void AddLocal(LocalSymbol local, DiagnosticBag diagnostics)
            {
                if (_locals == null)
                {
                    _locals = ArrayBuilder<LocalSymbol>.GetInstance();
                }

                if (local.Type.IsRestrictedType())
                {
                    diagnostics.Add(ErrorCode.ERR_ByRefTypeAndAwait, local.Locations[0], local.Type.ToDisplayString());
                }

                _locals.Add(local);
            }

            internal void AddLocals(ImmutableArray<LocalSymbol> locals)
            {
                if (_locals == null)
                {
                    _locals = ArrayBuilder<LocalSymbol>.GetInstance();
                }

                foreach (var local in locals)
                {
                    _locals.Add(local);
                }
            }

            public void AddStatement(BoundStatement statement)
            {
                if (_statements == null)
                {
                    _statements = ArrayBuilder<BoundStatement>.GetInstance();
                }

                _statements.Add(statement);
            }

            internal void AddStatements(ImmutableArray<BoundStatement> statements)
            {
                foreach (var statement in statements)
                {
                    AddStatement(statement);
                }
            }

            internal void AddExpressions(ImmutableArray<BoundExpression> expressions)
            {
                foreach (var expression in expressions)
                {
                    AddStatement(new BoundExpressionStatement(expression.Syntax, expression) { WasCompilerGenerated = true });
                }
            }

#if DEBUG
            internal override string Dump()
            {
                var node = new TreeDumperNode("boundSpillSequenceBuilder", null, new TreeDumperNode[]
                    {
                        new TreeDumperNode("locals", this.GetLocals(), null),
                        new TreeDumperNode("statements", null, from x in this.GetStatements() select BoundTreeDumperNodeProducer.MakeTree(x)),
                        new TreeDumperNode("value", null, new TreeDumperNode[] { BoundTreeDumperNodeProducer.MakeTree(this.Value) }),
                        new TreeDumperNode("type", this.Type, null)
                    });
                return TreeDumper.DumpCompact(node);
            }
#endif
        }

        private sealed class LocalSubstituter : BoundTreeRewriterWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator
        {
            private readonly PooledDictionary<LocalSymbol, LocalSymbol> _tempSubstitution;

            public LocalSubstituter(PooledDictionary<LocalSymbol, LocalSymbol> tempSubstitution, int recursionDepth)
                : base(recursionDepth)
            {
                _tempSubstitution = tempSubstitution;
            }

            public override BoundNode VisitLocal(BoundLocal node)
            {
                if (!node.LocalSymbol.SynthesizedKind.IsLongLived())
                {
                    LocalSymbol longLived;
                    if (_tempSubstitution.TryGetValue(node.LocalSymbol, out longLived))
                    {
                        return node.Update(longLived, node.ConstantValueOpt, node.Type);
                    }
                }

                return base.VisitLocal(node);
            }
        }

        internal static BoundStatement Rewrite(BoundStatement body, MethodSymbol method, TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            var tempSubstitution = PooledDictionary<LocalSymbol, LocalSymbol>.GetInstance();
            var spiller = new AwaitExpressionSpiller(method, body.Syntax, compilationState, tempSubstitution, diagnostics);
            var result = (BoundStatement)spiller.Visit(body);
            tempSubstitution.Free();
            return result;
        }

        private BoundExpression VisitExpression(ref BoundSpillSequenceBuilder builder, BoundExpression expression)
        {
            // wrap the node in a spill sequence to mark the fact that it must be moved up the tree.
            // The caller will handle this node type if the result is discarded.
            if (expression != null && expression.Kind == BoundKind.AwaitExpression)
            {
                // we force the await expression to be assigned to a temp variable
                var awaitExpression = (BoundAwaitExpression)expression;
                awaitExpression = awaitExpression.Update(
                    VisitExpression(ref builder, awaitExpression.Expression),
                    awaitExpression.GetAwaiter,
                    awaitExpression.IsCompleted,
                    awaitExpression.GetResult,
                    awaitExpression.Type);

                var syntax = awaitExpression.Syntax;

                Debug.Assert(syntax.IsKind(SyntaxKind.AwaitExpression));
                _F.Syntax = syntax;

                BoundAssignmentOperator assignToTemp;
                var replacement = _F.StoreToTemp(awaitExpression, out assignToTemp, kind: SynthesizedLocalKind.AwaitSpill, syntaxOpt: syntax);
                if (builder == null)
                {
                    builder = new BoundSpillSequenceBuilder();
                }

                builder.AddLocal(replacement.LocalSymbol, _F.Diagnostics);
                builder.AddStatement(_F.ExpressionStatement(assignToTemp));
                return replacement;
            }

            var e = (BoundExpression)this.Visit(expression);
            if (e == null || e.Kind != SpillSequenceBuilder)
            {
                return e;
            }

            var newBuilder = (BoundSpillSequenceBuilder)e;
            if (builder == null)
            {
                builder = newBuilder.Update(null);
            }
            else
            {
                builder.Include(newBuilder);
            }

            return newBuilder.Value;
        }

        private static BoundExpression UpdateExpression(BoundSpillSequenceBuilder builder, BoundExpression expression)
        {
            if (builder == null)
            {
                return expression;
            }

            Debug.Assert(builder.Value == null);
            if (!builder.HasLocals && !builder.HasStatements)
            {
                builder.Free();
                return expression;
            }

            return builder.Update(expression);
        }

        private BoundStatement UpdateStatement(BoundSpillSequenceBuilder builder, BoundStatement statement, bool substituteTemps)
        {
            if (builder == null)
            {
                // statement doesn't contain any await
                Debug.Assert(!substituteTemps || _tempSubstitution.Count == 0);
                Debug.Assert(statement != null);
                return statement;
            }

            Debug.Assert(builder.Value == null);
            if (statement != null)
            {
                builder.AddStatement(statement);
            }

            var substituterOpt = (substituteTemps && _tempSubstitution.Count > 0) ? new LocalSubstituter(_tempSubstitution, RecursionDepth) : null;
            var result = _F.Block(builder.GetLocals(), builder.GetStatements(substituterOpt));

            builder.Free();
            return result;
        }

        private BoundExpression Spill(
            BoundSpillSequenceBuilder builder,
            BoundExpression expression,
            RefKind refKind = RefKind.None,
            bool sideEffectsOnly = false)
        {
            Debug.Assert(builder != null);

            while (true)
            {
                switch (expression.Kind)
                {
                    case BoundKind.ArrayInitialization:
                        Debug.Assert(refKind == RefKind.None);
                        Debug.Assert(!sideEffectsOnly);
                        var arrayInitialization = (BoundArrayInitialization)expression;
                        var newInitializers = VisitExpressionList(ref builder, arrayInitialization.Initializers, forceSpill: true);
                        return arrayInitialization.Update(newInitializers);

                    case BoundKind.ArgListOperator:
                        Debug.Assert(refKind == RefKind.None);
                        Debug.Assert(!sideEffectsOnly);
                        var argumentList = (BoundArgListOperator)expression;
                        var newArgs = VisitExpressionList(ref builder, argumentList.Arguments, argumentList.ArgumentRefKindsOpt, forceSpill: true);
                        return argumentList.Update(newArgs, argumentList.ArgumentRefKindsOpt, argumentList.Type);

                    case SpillSequenceBuilder:
                        var sequenceBuilder = (BoundSpillSequenceBuilder)expression;
                        builder.Include(sequenceBuilder);
                        expression = sequenceBuilder.Value;
                        continue;

                    case BoundKind.Sequence:
                        // We don't need promote short-lived variables defined by the sequence to long-lived,
                        // since neither the side-effects nor the value of the sequence contains await 
                        // (otherwise it would be converted to a SpillSequenceBuilder).
                        var sequence = (BoundSequence)expression;
                        builder.AddLocals(sequence.Locals);
                        builder.AddExpressions(sequence.SideEffects);
                        expression = sequence.Value;
                        continue;

                    case BoundKind.ThisReference:
                    case BoundKind.BaseReference:
                        if (refKind != RefKind.None || expression.Type.IsReferenceType)
                        {
                            return expression;
                        }

                        goto default;

                    case BoundKind.Parameter:
                        if (refKind != RefKind.None)
                        {
                            return expression;
                        }

                        goto default;

                    case BoundKind.Local:
                        var local = (BoundLocal)expression;
                        if (local.LocalSymbol.SynthesizedKind == SynthesizedLocalKind.AwaitSpill || refKind != RefKind.None)
                        {
                            return local;
                        }

                        goto default;

                    case BoundKind.FieldAccess:
                        var field = (BoundFieldAccess)expression;
                        if (field.FieldSymbol.IsReadOnly)
                        {
                            if (field.FieldSymbol.IsStatic) return field;
                            if (field.FieldSymbol.ContainingType.IsValueType) goto default;
                            // save the receiver; can get the field later.
                            var receiver = Spill(builder, field.ReceiverOpt, (refKind != RefKind.None && field.FieldSymbol.Type.IsReferenceType) ? refKind : RefKind.None, sideEffectsOnly);
                            return field.Update(receiver, field.FieldSymbol, field.ConstantValueOpt, field.ResultKind, field.Type);
                        }
                        goto default;

                    case BoundKind.Call:
                        var call = (BoundCall)expression;
                        if (refKind != RefKind.None)
                        {
                            Debug.Assert(call.Method.RefKind != RefKind.None);
                            _F.Diagnostics.Add(ErrorCode.ERR_RefReturningCallAndAwait, _F.Syntax.Location, call.Method);
                            refKind = RefKind.None; // Switch the RefKind to avoid asserting later in the pipeline
                        }
                        goto default;

                    case BoundKind.Literal:
                    case BoundKind.TypeExpression:
                        return expression;

                    case BoundKind.ConditionalReceiver:
                        // we will rewrite this as a part of rewriting whole LoweredConditionalAccess
                        // later, if needed
                        return expression;

                    default:
                        if (expression.Type.SpecialType == SpecialType.System_Void || sideEffectsOnly)
                        {
                            builder.AddStatement(_F.ExpressionStatement(expression));
                            return null;
                        }
                        else
                        {
                            BoundAssignmentOperator assignToTemp;
                            Debug.Assert(_F.Syntax.IsKind(SyntaxKind.AwaitExpression));

                            var replacement = _F.StoreToTemp(
                                expression,
                                out assignToTemp,
                                refKind: refKind,
                                kind: SynthesizedLocalKind.AwaitSpill,
                                syntaxOpt: _F.Syntax);

                            builder.AddLocal(replacement.LocalSymbol, _F.Diagnostics);
                            builder.AddStatement(_F.ExpressionStatement(assignToTemp));
                            return replacement;
                        }
                }
            }
        }

        private ImmutableArray<BoundExpression> VisitExpressionList(
            ref BoundSpillSequenceBuilder builder,
            ImmutableArray<BoundExpression> args,
            ImmutableArray<RefKind> refKinds = default(ImmutableArray<RefKind>),
            bool forceSpill = false,
            bool sideEffectsOnly = false)
        {
            var newList = VisitList(args);
            Debug.Assert(newList.Length == args.Length);

            int lastSpill;
            if (forceSpill)
            {
                lastSpill = newList.Length - 1;
            }
            else
            {
                lastSpill = -1;
                for (int i = newList.Length - 1; i >= 0; i--)
                {
                    if (newList[i].Kind == SpillSequenceBuilder)
                    {
                        lastSpill = i;
                        break;
                    }
                }
            }

            if (lastSpill == -1)
            {
                return newList;
            }

            if (builder == null)
            {
                builder = new BoundSpillSequenceBuilder();
            }

            var result = ArrayBuilder<BoundExpression>.GetInstance();
            for (int i = 0; i <= lastSpill; i++)
            {
                var refKind = (!refKinds.IsDefaultOrEmpty && refKinds.Length > i && refKinds[i] != RefKind.None) ? RefKind.Ref : RefKind.None;
                var replacement = Spill(builder, newList[i], refKind, sideEffectsOnly);

                Debug.Assert(sideEffectsOnly || replacement != null);
                if (!sideEffectsOnly)
                {
                    result.Add(replacement);
                }
            }

            for (int i = lastSpill + 1; i < newList.Length; i++)
            {
                result.Add(newList[i]);
            }

            return result.ToImmutableAndFree();
        }

        #region Statement Visitors

        private void EnterStatement(BoundNode boundStatement)
        {
            _tempSubstitution.Clear();
        }

        public override BoundNode VisitSwitchStatement(BoundSwitchStatement node)
        {
            EnterStatement(node);

            BoundSpillSequenceBuilder builder = null;
            var preambleOpt = (BoundStatement)this.Visit(node.LoweredPreambleOpt);
            var boundExpression = VisitExpression(ref builder, node.Expression);
            var switchSections = this.VisitList(node.SwitchSections);
            return UpdateStatement(builder, node.Update(preambleOpt, boundExpression, node.ConstantTargetOpt, node.InnerLocals, node.InnerLocalFunctions, switchSections, node.BreakLabel, node.StringEquality), substituteTemps: true);
        }

        public override BoundNode VisitThrowStatement(BoundThrowStatement node)
        {
            EnterStatement(node);

            BoundSpillSequenceBuilder builder = null;
            BoundExpression expression = VisitExpression(ref builder, node.ExpressionOpt);
            return UpdateStatement(builder, node.Update(expression), substituteTemps: true);
        }

        public override BoundNode VisitExpressionStatement(BoundExpressionStatement node)
        {
            EnterStatement(node);

            BoundSpillSequenceBuilder builder = null;
            BoundExpression expr;

            if (node.Expression.Kind == BoundKind.AwaitExpression)
            {
                // await expression with result discarded
                var awaitExpression = (BoundAwaitExpression)node.Expression;
                var expression = VisitExpression(ref builder, awaitExpression.Expression);
                expr = awaitExpression.Update(expression, awaitExpression.GetAwaiter, awaitExpression.IsCompleted, awaitExpression.GetResult, awaitExpression.Type);
            }
            else
            {
                expr = VisitExpression(ref builder, node.Expression);
            }

            Debug.Assert(expr != null);
            Debug.Assert(builder == null || builder.Value == null);
            return UpdateStatement(builder, node.Update(expr), substituteTemps: true);
        }

        public override BoundNode VisitConditionalGoto(BoundConditionalGoto node)
        {
            EnterStatement(node);

            BoundSpillSequenceBuilder builder = null;
            var condition = VisitExpression(ref builder, node.Condition);
            return UpdateStatement(builder, node.Update(condition, node.JumpIfTrue, node.Label), substituteTemps: true);
        }

        public override BoundNode VisitReturnStatement(BoundReturnStatement node)
        {
            EnterStatement(node);

            BoundSpillSequenceBuilder builder = null;
            var expression = VisitExpression(ref builder, node.ExpressionOpt);
            return UpdateStatement(builder, node.Update(node.RefKind, expression), substituteTemps: true);
        }

#if DEBUG
        public override BoundNode DefaultVisit(BoundNode node)
        {
            Debug.Assert(!(node is BoundStatement));
            return base.DefaultVisit(node);
        }
#endif

        #endregion

        #region Expression Visitors

        public override BoundNode VisitAwaitExpression(BoundAwaitExpression node)
        {
            var builder = new BoundSpillSequenceBuilder();
            var replacement = VisitExpression(ref builder, node);
            return builder.Update(replacement);
        }

        public override BoundNode VisitAddressOfOperator(BoundAddressOfOperator node)
        {
            BoundSpillSequenceBuilder builder = null;
            var expr = VisitExpression(ref builder, node.Operand);
            return UpdateExpression(builder, node.Update(expr, node.IsFixedStatementAddressOf, node.Type));
        }

        public override BoundNode VisitArgListOperator(BoundArgListOperator node)
        {
            BoundSpillSequenceBuilder builder = null;
            var newArgs = VisitExpressionList(ref builder, node.Arguments);
            return UpdateExpression(builder, node.Update(newArgs, node.ArgumentRefKindsOpt, node.Type));
        }

        public override BoundNode VisitArrayAccess(BoundArrayAccess node)
        {
            BoundSpillSequenceBuilder builder = null;
            var expression = VisitExpression(ref builder, node.Expression);

            BoundSpillSequenceBuilder indicesBuilder = null;
            var indices = this.VisitExpressionList(ref indicesBuilder, node.Indices);

            if (indicesBuilder != null)
            {
                // spill the array if there were await expressions in the indices
                if (builder == null)
                {
                    builder = new BoundSpillSequenceBuilder();
                }

                expression = Spill(builder, expression);
            }

            if (builder != null)
            {
                builder.Include(indicesBuilder);
                indicesBuilder = builder;
                builder = null;
            }

            return UpdateExpression(indicesBuilder, node.Update(expression, indices, node.Type));
        }

        public override BoundNode VisitArrayCreation(BoundArrayCreation node)
        {
            BoundSpillSequenceBuilder builder = null;
            var init = (BoundArrayInitialization)VisitExpression(ref builder, node.InitializerOpt);
            ImmutableArray<BoundExpression> bounds;
            if (builder == null)
            {
                bounds = VisitExpressionList(ref builder, node.Bounds);
            }
            else
            {
                // spill bounds expressions if initializers contain await
                var boundsBuilder = new BoundSpillSequenceBuilder();
                bounds = VisitExpressionList(ref boundsBuilder, node.Bounds, forceSpill: true);
                boundsBuilder.Include(builder);
                builder = boundsBuilder;
            }

            return UpdateExpression(builder, node.Update(bounds, init, node.Type));
        }

        public override BoundNode VisitArrayInitialization(BoundArrayInitialization node)
        {
            BoundSpillSequenceBuilder builder = null;
            var initializers = this.VisitExpressionList(ref builder, node.Initializers);
            return UpdateExpression(builder, node.Update(initializers));
        }

        public override BoundNode VisitArrayLength(BoundArrayLength node)
        {
            BoundSpillSequenceBuilder builder = null;
            var expression = VisitExpression(ref builder, node.Expression);
            return UpdateExpression(builder, node.Update(expression, node.Type));
        }

        public override BoundNode VisitAsOperator(BoundAsOperator node)
        {
            BoundSpillSequenceBuilder builder = null;
            var operand = VisitExpression(ref builder, node.Operand);
            return UpdateExpression(builder, node.Update(operand, node.TargetType, node.Conversion, node.Type));
        }

        public override BoundNode VisitAssignmentOperator(BoundAssignmentOperator node)
        {
            BoundSpillSequenceBuilder builder = null;
            var right = VisitExpression(ref builder, node.Right);
            BoundExpression left;
            if (builder == null || node.Left.Kind == BoundKind.Local)
            {
                left = VisitExpression(ref builder, node.Left);
            }
            else
            {
                // if the right-hand-side has await, spill the left
                var leftBuilder = new BoundSpillSequenceBuilder();
                left = VisitExpression(ref leftBuilder, node.Left);
                if (left.Kind != BoundKind.Local)
                {
                    left = Spill(leftBuilder, left, RefKind.Ref);
                }

                leftBuilder.Include(builder);
                builder = leftBuilder;
            }

            return UpdateExpression(builder, node.Update(left, right, node.RefKind, node.Type));
        }

        public override BoundNode VisitBadExpression(BoundBadExpression node)
        {
            // Cannot recurse into BadExpression children
            return node;
        }

        public override BoundNode VisitUserDefinedConditionalLogicalOperator(BoundUserDefinedConditionalLogicalOperator node)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override BoundNode VisitBinaryOperator(BoundBinaryOperator node)
        {
            BoundSpillSequenceBuilder builder = null;
            var right = VisitExpression(ref builder, node.Right);
            BoundExpression left;
            if (builder == null)
            {
                left = VisitExpression(ref builder, node.Left);
            }
            else
            {
                var leftBuilder = new BoundSpillSequenceBuilder();
                left = VisitExpression(ref leftBuilder, node.Left);
                left = Spill(leftBuilder, left);
                if (node.OperatorKind == BinaryOperatorKind.LogicalBoolOr || node.OperatorKind == BinaryOperatorKind.LogicalBoolAnd)
                {
                    var tmp = _F.SynthesizedLocal(node.Type, kind: SynthesizedLocalKind.AwaitSpill, syntax: _F.Syntax);
                    leftBuilder.AddLocal(tmp, _F.Diagnostics);
                    leftBuilder.AddStatement(_F.Assignment(_F.Local(tmp), left));
                    leftBuilder.AddStatement(_F.If(
                        node.OperatorKind == BinaryOperatorKind.LogicalBoolAnd ? _F.Local(tmp) : _F.Not(_F.Local(tmp)),
                        UpdateStatement(builder, _F.Assignment(_F.Local(tmp), right), substituteTemps: false)));

                    return UpdateExpression(leftBuilder, _F.Local(tmp));
                }
                else
                {
                    // if the right-hand-side has await, spill the left
                    leftBuilder.Include(builder);
                    builder = leftBuilder;
                }
            }

            return UpdateExpression(builder, node.Update(node.OperatorKind, left, right, node.ConstantValue, node.MethodOpt, node.ResultKind, node.Type));
        }

        public override BoundNode VisitCall(BoundCall node)
        {
            BoundSpillSequenceBuilder builder = null;
            var arguments = this.VisitExpressionList(ref builder, node.Arguments, node.ArgumentRefKindsOpt);

            BoundExpression receiver = null;
            if (builder == null)
            {
                receiver = VisitExpression(ref builder, node.ReceiverOpt);
            }
            else if (!node.Method.IsStatic)
            {
                // spill the receiver if there were await expressions in the arguments
                var receiverBuilder = new BoundSpillSequenceBuilder();

                receiver = node.ReceiverOpt;
                var refKind = ReceiverSpillRefKind(receiver);

                receiver = Spill(receiverBuilder, VisitExpression(ref receiverBuilder, receiver), refKind: refKind);
                receiverBuilder.Include(builder);
                builder = receiverBuilder;
            }

            return UpdateExpression(builder, node.Update(receiver, node.Method, arguments));
        }

        private static RefKind ReceiverSpillRefKind(BoundExpression receiver)
        {
            if (!receiver.Type.IsReferenceType)
            {
                switch (receiver.Kind)
                {
                    case BoundKind.Parameter:
                    case BoundKind.Local:
                    case BoundKind.ArrayAccess:
                    case BoundKind.ThisReference:
                    case BoundKind.BaseReference:
                    case BoundKind.PointerIndirectionOperator:
                    case BoundKind.RefValueOperator:
                    case BoundKind.FieldAccess:
                        return RefKind.Ref;

                    case BoundKind.Call:
                        return ((BoundCall)receiver).Method.RefKind;
                }
            }

            return RefKind.None;
        }

        public override BoundNode VisitConditionalOperator(BoundConditionalOperator node)
        {
            BoundSpillSequenceBuilder conditionBuilder = null;
            var condition = VisitExpression(ref conditionBuilder, node.Condition);

            BoundSpillSequenceBuilder consequenceBuilder = null;
            var consequence = VisitExpression(ref consequenceBuilder, node.Consequence);

            BoundSpillSequenceBuilder alternativeBuilder = null;
            var alternative = VisitExpression(ref alternativeBuilder, node.Alternative);

            if (consequenceBuilder == null && alternativeBuilder == null)
            {
                return UpdateExpression(conditionBuilder, node.Update(condition, consequence, alternative, node.ConstantValueOpt, node.Type));
            }

            if (conditionBuilder == null) conditionBuilder = new BoundSpillSequenceBuilder();
            if (consequenceBuilder == null) consequenceBuilder = new BoundSpillSequenceBuilder();
            if (alternativeBuilder == null) alternativeBuilder = new BoundSpillSequenceBuilder();

            if (node.Type.SpecialType == SpecialType.System_Void)
            {
                conditionBuilder.AddStatement(
                    _F.If(condition,
                        UpdateStatement(consequenceBuilder, _F.ExpressionStatement(consequence), substituteTemps: false),
                        UpdateStatement(alternativeBuilder, _F.ExpressionStatement(alternative), substituteTemps: false)));

                return conditionBuilder.Update(_F.Default(node.Type));
            }
            else
            {
                Debug.Assert(_F.Syntax.IsKind(SyntaxKind.AwaitExpression));
                var tmp = _F.SynthesizedLocal(node.Type, kind: SynthesizedLocalKind.AwaitSpill, syntax: _F.Syntax);

                conditionBuilder.AddLocal(tmp, _F.Diagnostics);
                conditionBuilder.AddStatement(
                    _F.If(condition,
                        UpdateStatement(consequenceBuilder, _F.Assignment(_F.Local(tmp), consequence), substituteTemps: false),
                        UpdateStatement(alternativeBuilder, _F.Assignment(_F.Local(tmp), alternative), substituteTemps: false)));

                return conditionBuilder.Update(_F.Local(tmp));
            }
        }

        public override BoundNode VisitConversion(BoundConversion node)
        {
            BoundSpillSequenceBuilder builder = null;
            var operand = VisitExpression(ref builder, node.Operand);
            return UpdateExpression(
                builder,
                node.Update(
                    operand,
                    node.ConversionKind,
                    node.ResultKind,
                    isBaseConversion: node.IsBaseConversion,
                    symbolOpt: node.SymbolOpt,
                    @checked: node.Checked,
                    explicitCastInCode: node.ExplicitCastInCode,
                    isExtensionMethod: node.IsExtensionMethod,
                    isArrayIndex: node.IsArrayIndex,
                    constantValueOpt: node.ConstantValueOpt,
                    type: node.Type));
        }

        public override BoundNode VisitDelegateCreationExpression(BoundDelegateCreationExpression node)
        {
            BoundSpillSequenceBuilder builder = null;
            var argument = VisitExpression(ref builder, node.Argument);
            return UpdateExpression(builder, node.Update(argument, node.MethodOpt, node.IsExtensionMethod, node.Type));
        }

        public override BoundNode VisitFieldAccess(BoundFieldAccess node)
        {
            BoundSpillSequenceBuilder builder = null;
            var receiver = VisitExpression(ref builder, node.ReceiverOpt);
            return UpdateExpression(builder, node.Update(receiver, node.FieldSymbol, node.ConstantValueOpt, node.ResultKind, node.Type));
        }

        public override BoundNode VisitIsOperator(BoundIsOperator node)
        {
            BoundSpillSequenceBuilder builder = null;
            var operand = VisitExpression(ref builder, node.Operand);
            return UpdateExpression(builder, node.Update(operand, node.TargetType, node.Conversion, node.Type));
        }

        public override BoundNode VisitMakeRefOperator(BoundMakeRefOperator node)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override BoundNode VisitNullCoalescingOperator(BoundNullCoalescingOperator node)
        {
            BoundSpillSequenceBuilder builder = null;
            var right = VisitExpression(ref builder, node.RightOperand);
            BoundExpression left;
            if (builder == null)
            {
                left = VisitExpression(ref builder, node.LeftOperand);
            }
            else
            {
                var leftBuilder = new BoundSpillSequenceBuilder();
                left = VisitExpression(ref leftBuilder, node.LeftOperand);
                left = Spill(leftBuilder, left);

                var tmp = _F.SynthesizedLocal(node.Type, kind: SynthesizedLocalKind.AwaitSpill, syntax: _F.Syntax);
                leftBuilder.AddLocal(tmp, _F.Diagnostics);
                leftBuilder.AddStatement(_F.Assignment(_F.Local(tmp), left));
                leftBuilder.AddStatement(_F.If(
                    _F.ObjectEqual(_F.Local(tmp), _F.Null(left.Type)),
                    UpdateStatement(builder, _F.Assignment(_F.Local(tmp), right), substituteTemps: false)));

                return UpdateExpression(leftBuilder, _F.Local(tmp));
            }

            return UpdateExpression(builder, node.Update(left, right, node.LeftConversion, node.Type));
        }

        public override BoundNode VisitLoweredConditionalAccess(BoundLoweredConditionalAccess node)
        {
            var receiverRefKind = ReceiverSpillRefKind(node.Receiver);

            BoundSpillSequenceBuilder receiverBuilder = null;
            var receiver = VisitExpression(ref receiverBuilder, node.Receiver);

            BoundSpillSequenceBuilder whenNotNullBuilder = null;
            var whenNotNull = VisitExpression(ref whenNotNullBuilder, node.WhenNotNull);

            BoundSpillSequenceBuilder whenNullBuilder = null;
            var whenNullOpt = VisitExpression(ref whenNullBuilder, node.WhenNullOpt);

            if (whenNotNullBuilder == null && whenNullBuilder == null)
            {
                return UpdateExpression(receiverBuilder, node.Update(receiver, node.HasValueMethodOpt, whenNotNull, whenNullOpt, node.Id, node.Type));
            }

            if (receiverBuilder == null) receiverBuilder = new BoundSpillSequenceBuilder();
            if (whenNotNullBuilder == null) whenNotNullBuilder = new BoundSpillSequenceBuilder();
            if (whenNullBuilder == null) whenNullBuilder = new BoundSpillSequenceBuilder();


            BoundExpression condition;
            if (receiver.Type.IsReferenceType || receiver.Type.IsValueType || receiverRefKind == RefKind.None)
            {
                // spill to a clone
                receiver = Spill(receiverBuilder, receiver, RefKind.None);
                var hasValueOpt = node.HasValueMethodOpt;

                if (hasValueOpt == null)
                {
                    condition = _F.ObjectNotEqual(
                        _F.Convert(_F.SpecialType(SpecialType.System_Object), receiver),
                        _F.Null(_F.SpecialType(SpecialType.System_Object)));
                }
                else
                {
                    condition = _F.Call(receiver, hasValueOpt);
                }
            }
            else
            {
                Debug.Assert(node.HasValueMethodOpt == null);
                receiver = Spill(receiverBuilder, receiver, RefKind.Ref);

                var clone = _F.SynthesizedLocal(receiver.Type, _F.Syntax, refKind: RefKind.None, kind: SynthesizedLocalKind.AwaitSpill);
                receiverBuilder.AddLocal(clone, _F.Diagnostics);

                //  (object)default(T) != null
                var isNotClass = _F.ObjectNotEqual(
                                _F.Convert(_F.SpecialType(SpecialType.System_Object), _F.Default(receiver.Type)),
                                _F.Null(_F.SpecialType(SpecialType.System_Object)));

                // isNotCalss || {clone = receiver; (object)clone != null}
                condition = _F.LogicalOr(
                                    isNotClass,
                                    _F.Sequence(
                                        _F.AssignmentExpression(_F.Local(clone), receiver),
                                        _F.ObjectNotEqual(
                                            _F.Convert(_F.SpecialType(SpecialType.System_Object), _F.Local(clone)),
                                            _F.Null(_F.SpecialType(SpecialType.System_Object))))
                                    );

                receiver = _F.ComplexConditionalReceiver(receiver, _F.Local(clone));
            }

            if (node.Type.SpecialType == SpecialType.System_Void)
            {
                var whenNotNullStatement = UpdateStatement(whenNotNullBuilder, _F.ExpressionStatement(whenNotNull), substituteTemps: false);
                whenNotNullStatement = ConditionalReceiverReplacer.Replace(whenNotNullStatement, receiver, node.Id, RecursionDepth);

                Debug.Assert(whenNullOpt == null || !LocalRewriter.ReadIsSideeffecting(whenNullOpt));

                receiverBuilder.AddStatement(_F.If(condition, whenNotNullStatement));

                return receiverBuilder.Update(_F.Default(node.Type));
            }
            else
            {
                Debug.Assert(_F.Syntax.IsKind(SyntaxKind.AwaitExpression));
                var tmp = _F.SynthesizedLocal(node.Type, kind: SynthesizedLocalKind.AwaitSpill, syntax: _F.Syntax);
                var whenNotNullStatement = UpdateStatement(whenNotNullBuilder, _F.Assignment(_F.Local(tmp), whenNotNull), substituteTemps: false);
                whenNotNullStatement = ConditionalReceiverReplacer.Replace(whenNotNullStatement, receiver, node.Id, RecursionDepth);

                whenNullOpt = whenNullOpt ?? _F.Default(node.Type);

                receiverBuilder.AddLocal(tmp, _F.Diagnostics);
                receiverBuilder.AddStatement(
                    _F.If(condition,
                        whenNotNullStatement,
                        UpdateStatement(whenNullBuilder, _F.Assignment(_F.Local(tmp), whenNullOpt), substituteTemps: false)));

                return receiverBuilder.Update(_F.Local(tmp));
            }
        }

        private sealed class ConditionalReceiverReplacer : BoundTreeRewriterWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator
        {
            private readonly BoundExpression _receiver;
            private readonly int _receiverId;

#if DEBUG
            // we must replace exactly one node
            private int _replaced;
#endif

            private ConditionalReceiverReplacer(BoundExpression receiver, int receiverId, int recursionDepth)
                : base(recursionDepth)
            {
                _receiver = receiver;
                _receiverId = receiverId;
            }

            public static BoundStatement Replace(BoundNode node, BoundExpression receiver, int receiverID, int recursionDepth)
            {
                var replacer = new ConditionalReceiverReplacer(receiver, receiverID, recursionDepth);
                var result = (BoundStatement)replacer.Visit(node);
#if DEBUG
                Debug.Assert(replacer._replaced == 1, "should have replaced exactly one node");
#endif

                return result;
            }

            public override BoundNode VisitConditionalReceiver(BoundConditionalReceiver node)
            {
                if (node.Id == _receiverId)
                {
#if DEBUG
                    _replaced++;
#endif
                    return _receiver;
                }

                return node;
            }
        }


        public override BoundNode VisitObjectCreationExpression(BoundObjectCreationExpression node)
        {
            Debug.Assert(node.InitializerExpressionOpt == null);
            BoundSpillSequenceBuilder builder = null;
            var arguments = this.VisitExpressionList(ref builder, node.Arguments, node.ArgumentRefKindsOpt);
            return UpdateExpression(builder, node.Update(node.Constructor, arguments, node.ArgumentNamesOpt, node.ArgumentRefKindsOpt, node.Expanded, node.ArgsToParamsOpt, node.ConstantValueOpt, node.InitializerExpressionOpt, node.Type));
        }

        public override BoundNode VisitPointerElementAccess(BoundPointerElementAccess node)
        {
            BoundSpillSequenceBuilder builder = null;
            var index = VisitExpression(ref builder, node.Index);
            BoundExpression expression;
            if (builder == null)
            {
                expression = VisitExpression(ref builder, node.Expression);
            }
            else
            {
                var expressionBuilder = new BoundSpillSequenceBuilder();
                expression = VisitExpression(ref expressionBuilder, node.Expression);
                expression = Spill(expressionBuilder, expression);
                expressionBuilder.Include(builder);
                builder = expressionBuilder;
            }

            return UpdateExpression(builder, node.Update(expression, index, node.Checked, node.Type));
        }

        public override BoundNode VisitPointerIndirectionOperator(BoundPointerIndirectionOperator node)
        {
            BoundSpillSequenceBuilder builder = null;
            var operand = VisitExpression(ref builder, node.Operand);
            return UpdateExpression(builder, node.Update(operand, node.Type));
        }

        public override BoundNode VisitSequence(BoundSequence node)
        {
            BoundSpillSequenceBuilder valueBuilder = null;
            var value = VisitExpression(ref valueBuilder, node.Value);

            BoundSpillSequenceBuilder builder = null;
            var sideEffects = VisitExpressionList(ref builder, node.SideEffects, forceSpill: valueBuilder != null, sideEffectsOnly: true);

            if (builder == null && valueBuilder == null)
            {
                return node.Update(node.Locals, sideEffects, value, node.Type);
            }

            if (builder == null)
            {
                builder = new BoundSpillSequenceBuilder();
            }

            PromoteAndAddLocals(builder, node.Locals);
            builder.AddExpressions(sideEffects);
            builder.Include(valueBuilder);

            return builder.Update(value);
        }

        /// <summary>
        /// If an expression node that declares synthesized short-lived locals (currently only sequence) contains an await, these locals become long-lived since their 
        /// values may be read by code that follows the await. We promote these variables to long-lived of kind <see cref="SynthesizedLocalKind.AwaitSpill"/>. 
        /// </summary>
        private void PromoteAndAddLocals(BoundSpillSequenceBuilder builder, ImmutableArray<LocalSymbol> locals)
        {
            foreach (var local in locals)
            {
                if (local.SynthesizedKind.IsLongLived())
                {
                    builder.AddLocal(local, _F.Diagnostics);
                }
                else
                {
                    Debug.Assert(_F.Syntax.IsKind(SyntaxKind.AwaitExpression));
                    LocalSymbol longLived = local.WithSynthesizedLocalKindAndSyntax(SynthesizedLocalKind.AwaitSpill, _F.Syntax);
                    _tempSubstitution.Add(local, longLived);
                    builder.AddLocal(longLived, _F.Diagnostics);
                }
            }
        }

        public override BoundNode VisitUnaryOperator(BoundUnaryOperator node)
        {
            BoundSpillSequenceBuilder builder = null;
            BoundExpression operand = VisitExpression(ref builder, node.Operand);
            return UpdateExpression(builder, node.Update(node.OperatorKind, operand, node.ConstantValueOpt, node.MethodOpt, node.ResultKind, node.Type));
        }

        #endregion
    }
}
