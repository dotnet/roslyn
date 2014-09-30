// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    internal sealed class AwaitExpressionSpiller : BoundTreeRewriter
    {
        private const BoundKind SpillSequenceBuilder = BoundKind.SequencePoint; // NOTE: this bound kind is hijacked during this phase to represent BoundSpillSequenceBuilder

        private readonly SyntheticBoundNodeFactory F;
        private readonly PooledDictionary<LocalSymbol, LocalSymbol> tempSubstitution;

        private AwaitExpressionSpiller(MethodSymbol method, CSharpSyntaxNode syntaxNode, TypeCompilationState compilationState, PooledDictionary<LocalSymbol, LocalSymbol> tempSubstitution, DiagnosticBag diagnostics)
        {
            this.F = new SyntheticBoundNodeFactory(method, syntaxNode, compilationState, diagnostics);
            this.tempSubstitution = tempSubstitution;
        }

        private sealed class BoundSpillSequenceBuilder : BoundExpression
        {
            public readonly BoundExpression Value;

            private ArrayBuilder<LocalSymbol> locals;
            private ArrayBuilder<BoundStatement> statements;

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
                    return statements != null;
                }
            }

            public bool HasLocals
            {
                get
                {
                    return locals != null;
                }
            }

            public ImmutableArray<LocalSymbol> GetLocals()
            {
                return (locals == null) ? ImmutableArray<LocalSymbol>.Empty : locals.ToImmutable();
            }

            public ImmutableArray<BoundStatement> GetStatements(LocalSubstituter substituterOpt = null)
            {
                if (statements == null)
                {
                    return ImmutableArray<BoundStatement>.Empty;
                }

                if (substituterOpt == null)
                {
                    return statements.ToImmutable();
                }

                return statements.SelectAsArray((statement, substituter) => (BoundStatement)substituter.Visit(statement), substituterOpt);
            }

            internal BoundSpillSequenceBuilder Update(BoundExpression value)
            {
                var result = new BoundSpillSequenceBuilder(value);
                result.locals = this.locals;
                result.statements = this.statements;
                return result;
            }

            public void Free()
            {
                if (locals != null) locals.Free();
                if (statements != null) statements.Free();
            }

            internal void Include(BoundSpillSequenceBuilder other)
            {
                if (other != null)
                {
                    IncludeAndFree(ref locals, ref other.locals);
                    IncludeAndFree(ref statements, ref other.statements);
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
                if (locals == null)
                {
                    locals = ArrayBuilder<LocalSymbol>.GetInstance();
                }

                if (local.Type.IsRestrictedType())
                {
                    diagnostics.Add(ErrorCode.ERR_ByRefTypeAndAwait, local.Locations[0], local.Type.ToDisplayString());
                }

                locals.Add(local);
            }

            internal void AddLocals(ImmutableArray<LocalSymbol> locals)
            {
                if (this.locals == null)
                {
                    this.locals = ArrayBuilder<LocalSymbol>.GetInstance();
                }

                foreach (var local in locals)
                {
                    this.locals.Add(local);
                }
            }

            public void AddStatement(BoundStatement statement)
            {
                if (statements == null)
                {
                    statements = ArrayBuilder<BoundStatement>.GetInstance();
                }

                statements.Add(statement);
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

        private sealed class LocalSubstituter : BoundTreeRewriter
        {
            private readonly PooledDictionary<LocalSymbol, LocalSymbol> tempSubstitution;

            public LocalSubstituter(PooledDictionary<LocalSymbol, LocalSymbol> tempSubstitution)
            {
                this.tempSubstitution = tempSubstitution;
            }

            public override BoundNode VisitLocal(BoundLocal node)
            {
                if (!node.LocalSymbol.SynthesizedLocalKind.IsLongLived())
                {
                    LocalSymbol longLived;
                    if (tempSubstitution.TryGetValue(node.LocalSymbol, out longLived))
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

                BoundAssignmentOperator assignToTemp;
                var replacement = F.StoreToTemp(awaitExpression, out assignToTemp, kind: SynthesizedLocalKind.AwaitSpill);
                if (builder == null)
                {
                    builder = new BoundSpillSequenceBuilder();
                }

                builder.AddLocal(replacement.LocalSymbol, F.Diagnostics);
                F.Syntax = awaitExpression.Syntax;
                builder.AddStatement(F.ExpressionStatement(assignToTemp));
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
                Debug.Assert(!substituteTemps || tempSubstitution.Count == 0);
                Debug.Assert(statement != null);
                return statement;
            }

            Debug.Assert(builder.Value == null);
            if (statement != null)
            {
                builder.AddStatement(statement);
            }

            var substituterOpt = (substituteTemps && tempSubstitution.Count > 0) ? new LocalSubstituter(tempSubstitution) : null;
            var result = F.Block(builder.GetLocals(), builder.GetStatements(substituterOpt));

            if (substituteTemps)
            {
                tempSubstitution.Clear();
            }

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
                        // since neith the side-effects nor the value of the sequence contains await 
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
                        if (local.LocalSymbol.SynthesizedLocalKind == SynthesizedLocalKind.AwaitSpill || refKind != RefKind.None)
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

                    case BoundKind.Literal:
                    case BoundKind.TypeExpression:
                        return expression;

                    default:
                        if (expression.Type.SpecialType == SpecialType.System_Void || sideEffectsOnly)
                        {
                            builder.AddStatement(F.ExpressionStatement(expression));
                            return null;
                        }
                        else
                        {
                            BoundAssignmentOperator assignToTemp;
                            var replacement = F.StoreToTemp(expression, out assignToTemp, refKind: refKind, kind: SynthesizedLocalKind.AwaitSpill);
                            builder.AddLocal(replacement.LocalSymbol, F.Diagnostics);
                            builder.AddStatement(F.ExpressionStatement(assignToTemp));
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

        public override BoundNode VisitSwitchStatement(BoundSwitchStatement node)
        {
            Debug.Assert(tempSubstitution.Count == 0);

            BoundSpillSequenceBuilder builder = null;
            var boundExpression = VisitExpression(ref builder, node.BoundExpression);
            var switchSections = this.VisitList(node.SwitchSections);
            return UpdateStatement(builder, node.Update(boundExpression, node.ConstantTargetOpt, node.InnerLocals, switchSections, node.BreakLabel, node.StringEquality), substituteTemps: true);
        }

        public override BoundNode VisitThrowStatement(BoundThrowStatement node)
        {
            Debug.Assert(tempSubstitution.Count == 0);

            BoundSpillSequenceBuilder builder = null;
            BoundExpression expression = VisitExpression(ref builder, node.ExpressionOpt);
            return UpdateStatement(builder, node.Update(expression), substituteTemps: true);
        }

        public override BoundNode VisitExpressionStatement(BoundExpressionStatement node)
        {
            Debug.Assert(tempSubstitution.Count == 0);

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
            Debug.Assert(tempSubstitution.Count == 0);

            BoundSpillSequenceBuilder builder = null;
            var condition = VisitExpression(ref builder, node.Condition);
            return UpdateStatement(builder, node.Update(condition, node.JumpIfTrue, node.Label), substituteTemps: true);
        }

        public override BoundNode VisitReturnStatement(BoundReturnStatement node)
        {
            Debug.Assert(tempSubstitution.Count == 0);

            BoundSpillSequenceBuilder builder = null;
            var expression = VisitExpression(ref builder, node.ExpressionOpt);
            return UpdateStatement(builder, node.Update(expression), substituteTemps: true);
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
                    leftBuilder.AddStatement(F.If(
                        node.OperatorKind == BinaryOperatorKind.LogicalBoolAnd ? left : F.Not(left),
                        UpdateStatement(builder, F.Assignment(left, right), substituteTemps: false)));

                    return UpdateExpression(leftBuilder, left);
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
                    F.If(condition,
                        UpdateStatement(consequenceBuilder, F.ExpressionStatement(consequence), substituteTemps: false),
                        UpdateStatement(alternativeBuilder, F.ExpressionStatement(alternative), substituteTemps: false)));

                return conditionBuilder.Update(F.Default(node.Type));
            }
            else
            {
                var tmp = F.SynthesizedLocal(node.Type, kind: SynthesizedLocalKind.AwaitSpill);
                conditionBuilder.AddLocal(tmp, F.Diagnostics);
                conditionBuilder.AddStatement(
                    F.If(condition,
                        UpdateStatement(consequenceBuilder, F.Assignment(F.Local(tmp), consequence), substituteTemps: false),
                        UpdateStatement(alternativeBuilder, F.Assignment(F.Local(tmp), alternative), substituteTemps: false)));

                return conditionBuilder.Update(F.Local(tmp));
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

                leftBuilder.AddStatement(F.If(
                    F.ObjectEqual(left, F.Null(left.Type)),
                    UpdateStatement(builder, F.Assignment(left, right), substituteTemps: false)));

                return UpdateExpression(leftBuilder, left);
            }

            return UpdateExpression(builder, node.Update(left, right, node.LeftConversion, node.Type));
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
                if (local.SynthesizedLocalKind.IsLongLived())
                {
                    builder.AddLocal(local, F.Diagnostics);
                }
                else
                {
                    SynthesizedLocal shortLived = (SynthesizedLocal)local;
                    SynthesizedLocal longLived = shortLived.WithSynthesizedLocalKind(SynthesizedLocalKind.AwaitSpill);
                    tempSubstitution.Add(shortLived, longLived);

                    builder.AddLocal(longLived, F.Diagnostics);
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
