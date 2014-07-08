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
    internal sealed class AwaitLiftingRewriter : BoundTreeRewriter
    {
        private readonly SyntheticBoundNodeFactory F;
        private readonly HashSet<LocalSymbol> writeOnceTemps = new HashSet<LocalSymbol>();

        public AwaitLiftingRewriter(MethodSymbol method, CSharpSyntaxNode syntaxNode, TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            this.F = new SyntheticBoundNodeFactory(method, syntaxNode, compilationState, diagnostics);
        }

        private const BoundKind SpillSequence2 = BoundKind.SequencePoint; // NOTE: this bound kind is Hijacked during this phase for expressions
        class BoundSpillSequence2 : BoundExpression
        {
            private ArrayBuilder<LocalSymbol> locals;
            private ArrayBuilder<BoundStatement> statements;
            public readonly BoundExpression Value;

            public ImmutableArray<LocalSymbol> Locals
            {
                get
                {
                    return (locals == null) ? ImmutableArray<LocalSymbol>.Empty : locals.ToImmutable();
                }
            }
            public void Add(LocalSymbol local)
            {
                if (locals == null) locals = ArrayBuilder<LocalSymbol>.GetInstance();
                locals.Add(local);
            }

            public ImmutableArray<BoundStatement> Statements
            {
                get
                {
                    return (statements == null) ? ImmutableArray<BoundStatement>.Empty : statements.ToImmutable();
                }
            }
            public void Add(BoundStatement statement)
            {
                if (statements == null) statements = ArrayBuilder<BoundStatement>.GetInstance();
                statements.Add(statement);
            }

            public BoundSpillSequence2(BoundExpression value = null)
                : base(SpillSequence2, null, value != null ? value.Type : null)
            {
                Debug.Assert(value == null || value.Kind != SpillSequence2);
                this.Value = value;
            }

            internal BoundSpillSequence2 Update(BoundExpression value)
            {
                var result = new BoundSpillSequence2(value);
                result.locals = this.locals;
                result.statements = this.statements;
                return result;
            }

            public void Free()
            {
                if (locals != null) locals.Free();
                if (statements != null) statements.Free();
            }

            internal void IncludeSequence(BoundSpillSequence2 ss)
            {
                if (ss != null)
                {
                    AddRange(ss.Locals);
                    AddRange(ss.Statements);
                    ss.Free();
                }
            }

            internal void AddRange(ImmutableArray<LocalSymbol> locals)
            {
                if (locals.IsDefaultOrEmpty) return;
                foreach (var l in locals) this.Add(l);
            }

            internal void AddRange(ImmutableArray<BoundStatement> statements)
            {
                if (statements.IsDefaultOrEmpty) return;
                foreach (var s in statements) this.Add(s);
            }

            internal void AddRange(ImmutableArray<BoundExpression> sideEffects, Func<BoundExpression, BoundStatement> MakeExpressionStatement)
            {
                if (sideEffects.IsDefaultOrEmpty) return;
                foreach (var e in sideEffects) this.Add(MakeExpressionStatement(e));
            }

#if DEBUG
            internal override string Dump()
            {
                var node = new TreeDumperNode("boundSpillSequence2", null, new TreeDumperNode[]
                    {
                        new TreeDumperNode("locals", this.Locals, null),
                        new TreeDumperNode("statements", null, from x in this.Statements select BoundTreeDumperNodeProducer.MakeTree(x)),
                        new TreeDumperNode("value", null, new TreeDumperNode[] { BoundTreeDumperNodeProducer.MakeTree(this.Value) }),
                        new TreeDumperNode("type", this.Type, null)
                    });
                return TreeDumper.DumpCompact(node);
            }
#endif
        }

        internal static BoundStatement Rewrite(BoundStatement body, MethodSymbol method, TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            return new AwaitLiftingRewriter(method, body.Syntax, compilationState, diagnostics).Rewrite(body);
        }

        private BoundStatement Rewrite(BoundStatement body)
        {
            return (BoundStatement)this.Visit(body);
        }

        private BoundExpression VisitExpression(BoundExpression expression)
        {
            return (BoundExpression)this.Visit(expression);
        }

        private BoundExpression VisitExpression(ref BoundSpillSequence2 ss, BoundExpression expression)
        {
            // wrap the node in a spill sequence to mark the fact that it must be moved up the tree.
            // The caller will handle this node type if the result is discarded.
            if (expression != null && expression.Kind == BoundKind.AwaitExpression)
            {
                // we force the await expression to be assigned to a temp variable
                var awaitExpression = (BoundAwaitExpression)expression;
                awaitExpression = awaitExpression.Update(
                    VisitExpression(ref ss, awaitExpression.Expression), awaitExpression.GetAwaiter, awaitExpression.IsCompleted, awaitExpression.GetResult, awaitExpression.Type);
                BoundAssignmentOperator assignToTemp;
                var replacement = F.StoreToTemp(awaitExpression, out assignToTemp, kind: SynthesizedLocalKind.AwaitSpilledTemp);
                if (ss == null) ss = new BoundSpillSequence2();
                ss.Add(replacement.LocalSymbol);
                writeOnceTemps.Add(replacement.LocalSymbol);
                F.Syntax = awaitExpression.Syntax;
                ss.Add(F.ExpressionStatement(assignToTemp));
                return replacement;
            }

            var e = VisitExpression(expression);
            if (e == null || e.Kind != SpillSequence2)
            {
                return e;
            }
            var newss = (BoundSpillSequence2)e;
            if (ss == null)
            {
                ss = newss.Update(null);
            }
            else
            {
                ss.IncludeSequence(newss);
            }
            return newss.Value;
        }

        private static BoundExpression UpdateExpression(BoundSpillSequence2 ss, BoundExpression expression)
        {
            if (ss == null) return expression;
            Debug.Assert(ss.Value == null);
            if (ss.Locals.Length == 0 && ss.Statements.Length == 0)
            {
                ss.Free();
                return expression;
            }

            return ss.Update(expression);
        }

        private BoundStatement UpdateStatement(BoundSpillSequence2 ss, BoundStatement stmt)
        {
            if (ss == null)
            {
                Debug.Assert(stmt != null);
                return stmt;
            }

            Debug.Assert(ss.Value == null);
            if (stmt != null) ss.Add(stmt);
            var result = F.Block(ss.Locals, ss.Statements);
            ss.Free();
            return result;
        }

        public override BoundNode VisitAwaitExpression(BoundAwaitExpression node)
        {
            var ss = new BoundSpillSequence2();
            var replacement = VisitExpression(ref ss, node);
            return ss.Update(replacement);
        }

        public override BoundNode VisitExpressionStatement(BoundExpressionStatement node)
        {
            BoundSpillSequence2 ss = null;
            BoundExpression expr;

            if (node.Expression.Kind == BoundKind.AwaitExpression)
            {
                // await expression with result discarded
                var awaitExpression = (BoundAwaitExpression)node.Expression;
                var expression = VisitExpression(ref ss, awaitExpression.Expression);
                expr = awaitExpression.Update(expression, awaitExpression.GetAwaiter, awaitExpression.IsCompleted, awaitExpression.GetResult, awaitExpression.Type);
            }
            else
            {
                expr = VisitExpression(ref ss, node.Expression);
            }

            Debug.Assert(expr != null);
            Debug.Assert(ss == null || ss.Value == null);
            BoundStatement replacement = UpdateStatement(ss, node.Update(expr));
            return replacement;
        }

        private BoundExpression Spill(
            BoundSpillSequence2 spill,
            BoundExpression e,
            RefKind refKind = RefKind.None,
            bool sideEffectsOnly = false)
        {
            Debug.Assert(spill != null);
            while (true)
            {
                switch (e.Kind)
                {
                    case BoundKind.ArrayInitialization:
                        {
                            Debug.Assert(refKind == RefKind.None);
                            Debug.Assert(!sideEffectsOnly);
                            var ai = (BoundArrayInitialization)e;
                            var newInitializers = VisitExpressionList(ref spill, ai.Initializers, forceSpill: true);
                            return ai.Update(newInitializers);
                        }
                    case BoundKind.ArgListOperator:
                        {
                            Debug.Assert(refKind == RefKind.None);
                            Debug.Assert(!sideEffectsOnly);
                            var al = (BoundArgListOperator)e;
                            var newArgs = VisitExpressionList(ref spill, al.Arguments, al.ArgumentRefKindsOpt, forceSpill: true);
                            return al.Update(newArgs, al.ArgumentRefKindsOpt, al.Type);
                        }
                    case SpillSequence2:
                        {
                            var ss = (BoundSpillSequence2)e;
                            spill.IncludeSequence(ss);
                            e = ss.Value;
                            continue;
                        }
                    case BoundKind.Sequence:
                        {
                            var ss = (BoundSequence)e;
                            spill.AddRange(ss.Locals);
                            spill.AddRange(ss.SideEffects, MakeExpressionStatement);
                            e = ss.Value;
                            continue;
                        }
                    case BoundKind.ThisReference:
                    case BoundKind.BaseReference:
                        {
                            if (refKind != RefKind.None || e.Type.IsReferenceType) return e;
                            goto default;
                        }
                    case BoundKind.Parameter:
                        {
                            if (refKind != RefKind.None) return e;
                            goto default;
                        }
                    case BoundKind.Local:
                        {
                            var local = (BoundLocal)e;
                            if (writeOnceTemps.Contains(local.LocalSymbol) || refKind != RefKind.None) return local;
                            goto default;
                        }
                    case BoundKind.FieldAccess:
                        {
                            var field = (BoundFieldAccess)e;
                            if (field.FieldSymbol.IsReadOnly)
                            {
                                if (field.FieldSymbol.IsStatic) return field;
                                if (field.FieldSymbol.ContainingType.IsValueType) goto default;
                                // save the receiver; can get the field later.
                                var receiver = Spill(spill, field.ReceiverOpt, (refKind != RefKind.None && field.FieldSymbol.Type.IsReferenceType) ? refKind : RefKind.None, sideEffectsOnly);
                                return field.Update(receiver, field.FieldSymbol, field.ConstantValueOpt, field.ResultKind, field.Type);
                            }
                            goto default;
                        }
                    case BoundKind.Literal:
                    case BoundKind.TypeExpression:
                        return e;
                    default:
                        {
                            if (e.Type.SpecialType == SpecialType.System_Void || sideEffectsOnly)
                            {
                                spill.Add(F.ExpressionStatement(e));
                                return null;
                            }
                            else
                            { 
                                BoundAssignmentOperator assignToTemp;
                                var replacement = F.StoreToTemp(e, out assignToTemp, refKind: refKind, kind: SynthesizedLocalKind.AwaitSpilledTemp);
                                spill.Add(replacement.LocalSymbol);
                                writeOnceTemps.Add(replacement.LocalSymbol);
                                spill.Add(F.ExpressionStatement(assignToTemp));
                                return replacement;
                            }
                        }
                }
            }
        }

        private ImmutableArray<BoundExpression> VisitExpressionList(
            ref BoundSpillSequence2 spill,
            ImmutableArray<BoundExpression> args,
            ImmutableArray<RefKind> refKinds = default(ImmutableArray<RefKind>),
            bool forceSpill = false,
            bool sideEffectsOnly = false)
        {
            var newList = VisitList(args);
            int lastSpill;
            if (forceSpill)
            {
                lastSpill = args.Length - 1;
            }
            else
            {
                lastSpill = -1;
                for (int i = 0; i < newList.Length; i++)
                {
                    if (newList[i].Kind == SpillSequence2) lastSpill = i;
                }
            }
            if (lastSpill == -1) return newList;
            if (spill == null) spill = new BoundSpillSequence2();
            var builder = ArrayBuilder<BoundExpression>.GetInstance();
            for (int i = 0; i <= lastSpill; i++)
            {
                var replacement = Spill(spill, newList[i], (!refKinds.IsDefaultOrEmpty && refKinds.Length > i && refKinds[i] != RefKind.None) ? RefKind.Ref : RefKind.None, sideEffectsOnly);
                Debug.Assert(sideEffectsOnly || replacement != null);
                if (!sideEffectsOnly) builder.Add(replacement);
            }
            for (int i = lastSpill + 1; i < newList.Length; i++)
            {
                builder.Add(newList[i]);
            }
            return builder.ToImmutableAndFree();
        }

        #region Visitors
        public override BoundNode VisitAddressOfOperator(BoundAddressOfOperator node)
        {
            BoundSpillSequence2 ss = null;
            var expr = VisitExpression(ref ss, node.Operand);
            return UpdateExpression(ss, node.Update(expr, node.IsFixedStatementAddressOf, node.Type));
        }
        public override BoundNode VisitArgListOperator(BoundArgListOperator node)
        {
            BoundSpillSequence2 ss = null;
            var newArgs = VisitExpressionList(ref ss, node.Arguments);
            return UpdateExpression(ss, node.Update(newArgs, node.ArgumentRefKindsOpt, node.Type));
        }
        public override BoundNode VisitArrayAccess(BoundArrayAccess node)
        {
            BoundSpillSequence2 ssArray = null;
            var expression = VisitExpression(ref ssArray, node.Expression);

            BoundSpillSequence2 ssIndices = null;
            var indices = this.VisitExpressionList(ref ssIndices, node.Indices);

            if (ssIndices != null)
            {
                // spill the array if there were await expressions in the indices
                if (ssArray == null) ssArray = new BoundSpillSequence2();
                expression = Spill(ssArray, expression);
            }

            if (ssArray != null)
            {
                ssArray.IncludeSequence(ssIndices);
                ssIndices = ssArray;
                ssArray = null;
            }
            return UpdateExpression(ssIndices, node.Update(expression, indices, node.Type));
        }
        public override BoundNode VisitArrayCreation(BoundArrayCreation node)
        {
            BoundSpillSequence2 ss = null;
            var init = (BoundArrayInitialization)VisitExpression(ref ss, node.InitializerOpt);
            ImmutableArray<BoundExpression> bounds;
            if (ss == null)
            {
                bounds = VisitExpressionList(ref ss, node.Bounds);
            }
            else
            {
                // spill bounds expressions if initializers contain await
                var ss2 = new BoundSpillSequence2();
                bounds = VisitExpressionList(ref ss2, node.Bounds, forceSpill: true);
                ss2.IncludeSequence(ss);
                ss = ss2;
            }

            return UpdateExpression(ss, node.Update(bounds, init, node.Type));
        }
        public override BoundNode VisitArrayInitialization(BoundArrayInitialization node)
        {
            BoundSpillSequence2 ss = null;
            var initializers = this.VisitExpressionList(ref ss, node.Initializers);
            return UpdateExpression(ss, node.Update(initializers));
        }
        public override BoundNode VisitArrayLength(BoundArrayLength node)
        {
            BoundSpillSequence2 ss = null;
            var expression = VisitExpression(ref ss, node.Expression);
            return UpdateExpression(ss, node.Update(expression, node.Type));
        }
        public override BoundNode VisitAsOperator(BoundAsOperator node)
        {
            BoundSpillSequence2 ss = null;
            var operand = VisitExpression(ref ss, node.Operand);
            return UpdateExpression(ss, node.Update(operand, node.TargetType, node.Conversion, node.Type));
        }
        public override BoundNode VisitAssignmentOperator(BoundAssignmentOperator node)
        {
            BoundSpillSequence2 ss = null;
            var right = VisitExpression(ref ss, node.Right);
            BoundExpression left;
            if (ss == null || node.Left.Kind == BoundKind.Local)
            {
                left = VisitExpression(ref ss, node.Left);
            }
            else
            {
                // if the right-hand-side has await, spill the left
                var ss2 = new BoundSpillSequence2();
                left = VisitExpression(ref ss2, node.Left);
                if (left.Kind != BoundKind.Local) left = Spill(ss2, left, RefKind.Ref);
                ss2.IncludeSequence(ss);
                ss = ss2;
            }

            return UpdateExpression(ss, node.Update(left, right, node.RefKind, node.Type));
        }

        public override BoundNode VisitBadExpression(BoundBadExpression node)
        {
            // Cannot recurse into BadExpression children
            return node;
        }

        public override BoundNode VisitBinaryOperator(BoundBinaryOperator node)
        {
            BoundSpillSequence2 ss = null;
            var right = VisitExpression(ref ss, node.Right);
            BoundExpression left;
            if (ss == null)
            {
                left = VisitExpression(ref ss, node.Left);
            }
            else
            {
                var ssLeft = new BoundSpillSequence2();
                left = VisitExpression(ref ssLeft, node.Left);
                left = Spill(ssLeft, left);
                if (node.OperatorKind == BinaryOperatorKind.LogicalBoolOr || node.OperatorKind == BinaryOperatorKind.LogicalBoolAnd)
                {
                    ssLeft.Add(F.If(
                        node.OperatorKind == BinaryOperatorKind.LogicalBoolAnd ? left : F.Not(left),
                        UpdateStatement(ss, F.Assignment(left, right))
                        ));
                    return UpdateExpression(ssLeft, left);
                }
                else
                {
                    // if the right-hand-side has await, spill the left
                    ssLeft.IncludeSequence(ss);
                    ss = ssLeft;
                }
            }

            return UpdateExpression(ss, node.Update(node.OperatorKind, left, right, node.ConstantValue, node.MethodOpt, node.ResultKind, node.Type));
        }
        public override BoundNode VisitCall(BoundCall node)
        {
            BoundSpillSequence2 ss = null;
            var arguments = this.VisitExpressionList(ref ss, node.Arguments, node.ArgumentRefKindsOpt);

            BoundExpression receiver = null;
            if (ss == null)
            {
                receiver = VisitExpression(ref ss, node.ReceiverOpt);
            }
            else if (!node.Method.IsStatic)
            {
                // spill the receiver if there were await expressions in the arguments
                var ss2 = new BoundSpillSequence2();
                receiver = Spill(ss2, VisitExpression(ref ss2, node.ReceiverOpt), refKind: node.ReceiverOpt.Type.IsReferenceType ? RefKind.None : RefKind.Ref);
                ss2.IncludeSequence(ss);
                ss = ss2;
            }

            return UpdateExpression(ss, node.Update(receiver, node.Method, arguments));
        }
        public override BoundNode VisitConditionalGoto(BoundConditionalGoto node)
        {
            BoundSpillSequence2 ss = null;
            var condition = VisitExpression(ref ss, node.Condition);
            return UpdateStatement(ss, node.Update(condition, node.JumpIfTrue, node.Label));
        }
        public override BoundNode VisitConditionalOperator(BoundConditionalOperator node)
        {
            BoundSpillSequence2 ss1 = null;
            var condition = VisitExpression(ref ss1, node.Condition);

            BoundSpillSequence2 ss2 = null;
            var consequence = VisitExpression(ref ss2, node.Consequence);

            BoundSpillSequence2 ss3 = null;
            var alternative = VisitExpression(ref ss3, node.Alternative);

            if (ss2 == null && ss3 == null)
            {
                return UpdateExpression(ss1, node.Update(condition, consequence, alternative, node.ConstantValueOpt, node.Type));
            }

            var tmp = F.SynthesizedLocal(node.Type);
            if (ss1 == null) ss1 = new BoundSpillSequence2();
            if (ss2 == null) ss2 = new BoundSpillSequence2();
            if (ss3 == null) ss3 = new BoundSpillSequence2();

            ss1.Add(tmp);
            ss1.Add(
                F.If(condition,
                    UpdateStatement(ss2, F.Assignment(F.Local(tmp), consequence)),
                    UpdateStatement(ss3, F.Assignment(F.Local(tmp), alternative))));
            return ss1.Update(F.Local(tmp));
        }
        public override BoundNode VisitConversion(BoundConversion node)
        {
            BoundSpillSequence2 ss = null;
            var operand = VisitExpression(ref ss, node.Operand);
            return UpdateExpression(
                ss,
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
            BoundSpillSequence2 ss = null;
            var argument = VisitExpression(ref ss, node.Argument);
            return UpdateExpression(ss, node.Update(argument, node.MethodOpt, node.IsExtensionMethod, node.Type));
        }
        public override BoundNode VisitFieldAccess(BoundFieldAccess node)
        {
            BoundSpillSequence2 ss = null;
            var receiver = VisitExpression(ref ss, node.ReceiverOpt);
            return UpdateExpression(ss, node.Update(receiver, node.FieldSymbol, node.ConstantValueOpt, node.ResultKind, node.Type));
        }
        public override BoundNode VisitIsOperator(BoundIsOperator node)
        {
            BoundSpillSequence2 ss = null;
            var operand = VisitExpression(ref ss, node.Operand);
            return UpdateExpression(ss, node.Update(operand, node.TargetType, node.Conversion, node.Type));
        }
        public override BoundNode VisitMakeRefOperator(BoundMakeRefOperator node)
        {
            throw ExceptionUtilities.Unreachable;
        }
        public override BoundNode VisitNullCoalescingOperator(BoundNullCoalescingOperator node)
        {
            BoundSpillSequence2 ss = null;
            var right = VisitExpression(ref ss, node.RightOperand);
            BoundExpression left;
            if (ss == null)
            {
                left = VisitExpression(ref ss, node.LeftOperand);
            }
            else
            {
                var ssLeft = new BoundSpillSequence2();
                left = VisitExpression(ref ssLeft, node.LeftOperand);
                left = Spill(ssLeft, left);

                ssLeft.Add(F.If(
                    F.ObjectEqual(left, F.Null(left.Type)),
                    UpdateStatement(ss, F.Assignment(left, right))
                    ));
                return UpdateExpression(ssLeft, left);
            }

            return UpdateExpression(ss, node.Update(left, right, node.LeftConversion, node.Type));
        }
        public override BoundNode VisitObjectCreationExpression(BoundObjectCreationExpression node)
        {
            Debug.Assert(node.InitializerExpressionOpt == null);
            BoundSpillSequence2 ss = null;
            var arguments = this.VisitExpressionList(ref ss, node.Arguments, node.ArgumentRefKindsOpt);
            return UpdateExpression(ss, node.Update(node.Constructor, arguments, node.ArgumentNamesOpt, node.ArgumentRefKindsOpt, node.Expanded, node.ArgsToParamsOpt, node.ConstantValueOpt, node.InitializerExpressionOpt, node.Type));
        }
        public override BoundNode VisitPointerElementAccess(BoundPointerElementAccess node)
        {
            BoundSpillSequence2 ss = null;
            var index = VisitExpression(ref ss, node.Index);
            BoundExpression expression;
            if (ss == null)
            {
                expression = VisitExpression(ref ss, node.Expression);
            }
            else
            {
                // if the right-hand-side has await, spill the left
                var ss2 = new BoundSpillSequence2();
                expression = VisitExpression(ref ss2, node.Expression);
                expression = Spill(ss2, expression);
                ss2.IncludeSequence(ss);
                ss = ss2;
            }

            return UpdateExpression(ss, node.Update(expression, index, node.Checked, node.Type));
        }
        public override BoundNode VisitPointerIndirectionOperator(BoundPointerIndirectionOperator node)
        {
            BoundSpillSequence2 ss = null;
            var operand = VisitExpression(ref ss, node.Operand);
            return UpdateExpression(ss, node.Update(operand, node.Type));
        }
        public override BoundNode VisitReturnStatement(BoundReturnStatement node)
        {
            BoundSpillSequence2 ss = null;
            var expression = VisitExpression(ref ss, node.ExpressionOpt);
            return UpdateStatement(ss, node.Update(expression));
        }
        public override BoundNode VisitSequence(BoundSequence node)
        {
            BoundSpillSequence2 ss2 = null;
            var value = VisitExpression(ref ss2, node.Value);

            BoundSpillSequence2 ss1 = null;
            var sideEffects = VisitExpressionList(ref ss1, node.SideEffects, forceSpill: ss2 != null, sideEffectsOnly: true);

            if (ss1 == null && ss2 == null)
            {
                return node.Update(node.Locals, sideEffects, value, node.Type);
            }

            if (ss1 == null) ss1 = new BoundSpillSequence2(); // possible if sideEffects is empty
            ss1.AddRange(sideEffects, MakeExpressionStatement);
            ss1.AddRange(node.Locals);
            ss1.IncludeSequence(ss2);
            return ss1.Update(value);
        }
        BoundStatement MakeExpressionStatement(BoundExpression e)
        {
            return F.ExpressionStatement(e);
        }
        public override BoundNode VisitSwitchStatement(BoundSwitchStatement node)
        {
            BoundSpillSequence2 ss = null;
            BoundExpression boundExpression = VisitExpression(ref ss, node.BoundExpression);
            ImmutableArray<BoundSwitchSection> switchSections = (ImmutableArray<BoundSwitchSection>)this.VisitList(node.SwitchSections);
            return UpdateStatement(ss, node.Update(node.OuterLocals, boundExpression, node.ConstantTargetOpt, node.InnerLocals, switchSections, node.BreakLabel, node.StringEquality));
        }
        public override BoundNode VisitThrowStatement(BoundThrowStatement node)
        {
            BoundSpillSequence2 ss = null;
            BoundExpression expression = VisitExpression(ref ss, node.ExpressionOpt);
            return UpdateStatement(ss, node.Update(expression));
        }
        public override BoundNode VisitUnaryOperator(BoundUnaryOperator node)
        {
            BoundSpillSequence2 ss = null;
            BoundExpression operand = VisitExpression(ref ss, node.Operand);
            return UpdateExpression(ss, node.Update(node.OperatorKind, operand, node.ConstantValueOpt, node.MethodOpt, node.ResultKind, node.Type));
        }
        #endregion
    }
}
