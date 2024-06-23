// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class SpillSequenceSpiller : BoundTreeRewriterWithStackGuard
    {
        private const BoundKind SpillSequenceBuilderKind = (BoundKind)byte.MaxValue;

        private readonly SyntheticBoundNodeFactory _F;
        private readonly PooledDictionary<LocalSymbol, LocalSymbol> _tempSubstitution;
        private readonly PooledDictionary<LocalSymbol, BoundComplexConditionalReceiver> _receiverSubstitution;

        private SpillSequenceSpiller(
            MethodSymbol method, SyntaxNode syntaxNode, TypeCompilationState compilationState,
            PooledDictionary<LocalSymbol, LocalSymbol> tempSubstitution,
            PooledDictionary<LocalSymbol, BoundComplexConditionalReceiver> receiverSubstitution,
            BindingDiagnosticBag diagnostics)
        {
            _F = new SyntheticBoundNodeFactory(method, syntaxNode, compilationState, diagnostics);
            _F.CurrentFunction = method;
            _tempSubstitution = tempSubstitution;
            _receiverSubstitution = receiverSubstitution;
        }

        private sealed class BoundSpillSequenceBuilder : BoundExpression
        {
            public readonly BoundExpression Value;

            private ArrayBuilder<LocalSymbol> _locals;
            private ArrayBuilder<BoundStatement> _statements;

            public BoundSpillSequenceBuilder(SyntaxNode syntax, BoundExpression value = null)
                : base(SpillSequenceBuilderKind, syntax, value?.Type)
            {
                Debug.Assert(value?.Kind != SpillSequenceBuilderKind);
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

            public ImmutableArray<LocalSymbol> GetLocals()
            {
                return (_locals == null) ? ImmutableArray<LocalSymbol>.Empty : _locals.ToImmutable();
            }

            public ImmutableArray<BoundStatement> GetStatements()
            {
                if (_statements == null)
                {
                    return ImmutableArray<BoundStatement>.Empty;
                }

                return _statements.ToImmutable();
            }

            internal BoundSpillSequenceBuilder Update(BoundExpression value)
            {
                var result = new BoundSpillSequenceBuilder(this.Syntax, value);
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

            public void AddLocal(LocalSymbol local)
            {
                if (_locals == null)
                {
                    _locals = ArrayBuilder<LocalSymbol>.GetInstance();
                }

                _locals.Add(local);
            }

            public void AddLocals(ImmutableArray<LocalSymbol> locals)
            {
                foreach (var local in locals)
                {
                    AddLocal(local);
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

            public void AddStatements(ImmutableArray<BoundStatement> statements)
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
            private readonly PooledDictionary<LocalSymbol, BoundComplexConditionalReceiver> _receiverSubstitution;

            private LocalSubstituter(
                PooledDictionary<LocalSymbol, LocalSymbol> tempSubstitution,
                PooledDictionary<LocalSymbol, BoundComplexConditionalReceiver> receiverSubstitution,
                int recursionDepth = 0)
                : base(recursionDepth)
            {
                _tempSubstitution = tempSubstitution;
                _receiverSubstitution = receiverSubstitution;
            }

            public static BoundNode Rewrite(
                PooledDictionary<LocalSymbol, LocalSymbol> tempSubstitution,
                PooledDictionary<LocalSymbol, BoundComplexConditionalReceiver> receiverSubstitution,
                BoundNode node)
            {
                if (tempSubstitution.Count == 0)
                {
                    return node;
                }

                var substituter = new LocalSubstituter(tempSubstitution, receiverSubstitution);
                return substituter.Visit(node);
            }

            public override BoundNode VisitLocal(BoundLocal node)
            {
                if (!node.LocalSymbol.SynthesizedKind.IsLongLived())
                {
                    LocalSymbol longLived;
                    if (_tempSubstitution.TryGetValue(node.LocalSymbol, out longLived))
                    {
                        Debug.Assert(!_receiverSubstitution.ContainsKey(node.LocalSymbol));

                        return node.Update(longLived, node.ConstantValueOpt, node.Type);
                    }

                    if (_receiverSubstitution.TryGetValue(node.LocalSymbol, out var receiver))
                    {
                        return Visit(receiver);
                    }
                }

                return base.VisitLocal(node);
            }
        }

        internal static BoundStatement Rewrite(BoundStatement body, MethodSymbol method, TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
        {
            var tempSubstitution = PooledDictionary<LocalSymbol, LocalSymbol>.GetInstance();
            var receiverSubstitution = PooledDictionary<LocalSymbol, BoundComplexConditionalReceiver>.GetInstance();
            var spiller = new SpillSequenceSpiller(method, body.Syntax, compilationState, tempSubstitution, receiverSubstitution, diagnostics);
            BoundNode result = spiller.Visit(body);
            result = LocalSubstituter.Rewrite(tempSubstitution, receiverSubstitution, result);
            tempSubstitution.Free();
            receiverSubstitution.Free();
            return (BoundStatement)result;
        }

        private BoundExpression VisitExpression(ref BoundSpillSequenceBuilder builder, BoundExpression expression)
        {
            var e = (BoundExpression)this.Visit(expression);
            if (e == null || e.Kind != SpillSequenceBuilderKind)
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

        private BoundStatement UpdateStatement(BoundSpillSequenceBuilder builder, BoundStatement statement)
        {
            if (builder == null)
            {
                Debug.Assert(statement != null);
                return statement;
            }

            Debug.Assert(builder.Value == null);
            if (statement != null)
            {
                builder.AddStatement(statement);
            }

            var result = new BoundBlock(statement.Syntax, builder.GetLocals(), builder.GetStatements()) { WasCompilerGenerated = true };

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
            if (builder.Syntax != null)
                _F.Syntax = builder.Syntax;

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

                    case SpillSequenceBuilderKind:
                        var sequenceBuilder = (BoundSpillSequenceBuilder)expression;
                        builder.Include(sequenceBuilder);
                        expression = sequenceBuilder.Value;
                        continue;

                    case BoundKind.Sequence:
                        if (refKind != RefKind.None || expression.Type?.IsRefLikeOrAllowsRefLikeType() == true)
                        {
                            var sequence = (BoundSequence)expression;

                            PromoteAndAddLocals(builder, sequence.Locals);
                            builder.AddExpressions(sequence.SideEffects);
                            expression = sequence.Value;
                            continue;
                        }

                        goto default;

                    case BoundKind.AssignmentOperator:
                        var assignment = (BoundAssignmentOperator)expression;
                        if (assignment.IsRef &&
                            assignment is not { Left.Kind: BoundKind.Local, Right.Kind: BoundKind.ArrayAccess }) // Optimize for some known to be safe scenarios.
                        {
                            if (sideEffectsOnly &&
                                IsComplexConditionalInitializationOfReceiverRef(
                                    assignment,
                                    out LocalSymbol receiverRefLocal,
                                    out BoundComplexConditionalReceiver complexReceiver,
                                    out BoundLocal valueTypeReceiver,
                                    out BoundLocal referenceTypeReceiver))
                            {
                                Debug.Assert(receiverRefLocal.IsKnownToReferToTempIfReferenceType);
                                builder.AddStatement(_F.ExpressionStatement(complexReceiver));

                                _receiverSubstitution.Add(receiverRefLocal, complexReceiver.Update(valueTypeReceiver, referenceTypeReceiver, complexReceiver.Type));
                                return null;
                            }
                            else
                            {
                                var left = Spill(builder, assignment.Left, RefKind.Ref);
                                var right = Spill(builder, assignment.Right, RefKind.Ref);
                                expression = assignment.Update(left, right, assignment.IsRef, assignment.Type);
                            }
                        }

                        goto default;

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
                        if (local.LocalSymbol.SynthesizedKind == SynthesizedLocalKind.Spill || refKind != RefKind.None)
                        {
                            return local;
                        }

                        goto default;

                    case BoundKind.FieldAccess:
                        var field = (BoundFieldAccess)expression;
                        var fieldSymbol = field.FieldSymbol;
                        if (fieldSymbol.IsStatic)
                        {
                            // no need to spill static fields if used as locations or if readonly
                            if (refKind != RefKind.None || fieldSymbol.IsReadOnly)
                            {
                                return field;
                            }
                            goto default;
                        }

                        if (refKind == RefKind.None) goto default;

                        var receiver = Spill(builder, field.ReceiverOpt, fieldSymbol.ContainingType.IsValueType ? refKind : RefKind.None);
                        return field.Update(receiver, fieldSymbol, field.ConstantValueOpt, field.ResultKind, field.Type);

                    case BoundKind.Literal:
                    case BoundKind.TypeExpression:
                        return expression;

                    case BoundKind.ConditionalReceiver:
                        // we will rewrite this as a part of rewriting whole LoweredConditionalAccess
                        // later, if needed
                        return expression;

                    case BoundKind.Call:
                        var call = (BoundCall)expression;

                        // If the method is known to have no observable side-effects, we can spill its receiver and arguments,
                        // and call it later, after some other (unrelated) side-effects are evaluated.
                        // It is similar to spilling a field/array access.
                        if (refKind != RefKind.None)
                        {
                            if (call.Method.OriginalDefinition is SynthesizedInlineArrayFirstElementRefMethod or SynthesizedInlineArrayFirstElementRefReadOnlyMethod)
                            {
                                Debug.Assert(call.Arguments.Length == 1);
                                return call.Update(ImmutableArray.Create(Spill(builder, call.Arguments[0], call.ArgumentRefKindsOpt[0])));
                            }
                            else if (call.Method.OriginalDefinition is SynthesizedInlineArrayElementRefMethod or SynthesizedInlineArrayElementRefReadOnlyMethod)
                            {
                                return spillInlineArrayHelperWithTwoArguments(builder, call);
                            }
                            else if (call.Method.OriginalDefinition == _F.Compilation.GetWellKnownTypeMember(WellKnownMember.System_Span_T__get_Item) ||
                                call.Method.OriginalDefinition == _F.Compilation.GetWellKnownTypeMember(WellKnownMember.System_ReadOnlySpan_T__get_Item))
                            {
                                Debug.Assert(call.Arguments.Length == 1);
                                return call.Update(Spill(builder, call.ReceiverOpt, ReceiverSpillRefKind(call.ReceiverOpt)),
                                                   initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown,
                                                   call.Method,
                                                   ImmutableArray.Create(Spill(builder, call.Arguments[0])));
                            }
                        }
                        else if (call.Method.OriginalDefinition is SynthesizedInlineArrayAsSpanMethod or SynthesizedInlineArrayAsReadOnlySpanMethod)
                        {
                            return spillInlineArrayHelperWithTwoArguments(builder, call);
                        }
                        else if (call.Method.OriginalDefinition == _F.Compilation.GetWellKnownTypeMember(WellKnownMember.System_Span_T__Slice_Int_Int) ||
                                 call.Method.OriginalDefinition == _F.Compilation.GetWellKnownTypeMember(WellKnownMember.System_ReadOnlySpan_T__Slice_Int_Int))
                        {
                            Debug.Assert(call.Arguments.Length == 2);
                            return call.Update(Spill(builder, call.ReceiverOpt, ReceiverSpillRefKind(call.ReceiverOpt)),
                                               initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown,
                                               call.Method,
                                               ImmutableArray.Create(Spill(builder, call.Arguments[0]), Spill(builder, call.Arguments[1])));
                        }
                        else if (call.Method == _F.Compilation.GetSpecialTypeMember(SpecialMember.System_String__op_Implicit_ToReadOnlySpanOfChar))
                        {
                            Debug.Assert(call.Arguments.Length == 1);
                            return call.Update([Spill(builder, call.Arguments[0])]);
                        }

                        goto default;

                    case BoundKind.ObjectCreationExpression:
                        var objectCreationExpression = (BoundObjectCreationExpression)expression;

                        if (refKind == RefKind.None &&
                            objectCreationExpression.InitializerExpressionOpt is null &&
                            objectCreationExpression.Constructor.OriginalDefinition == _F.Compilation.GetSpecialTypeMember(SpecialMember.System_ReadOnlySpan_T__ctor_Reference))
                        {
                            Debug.Assert(objectCreationExpression.Arguments.Length == 1);
                            var argRefKinds = objectCreationExpression.ArgumentRefKindsOpt;
                            return objectCreationExpression.Update(objectCreationExpression.Constructor,
                                                                   [Spill(builder, objectCreationExpression.Arguments[0], argRefKinds.IsDefault ? RefKind.None : argRefKinds[0])],
                                                                   objectCreationExpression.ArgumentRefKindsOpt,
                                                                   newInitializerExpression: null);
                        }

                        goto default;

                    default:
                        if (expression.Type.IsVoidType() || sideEffectsOnly)
                        {
                            builder.AddStatement(_F.ExpressionStatement(expression));
                            return null;
                        }
                        else
                        {
                            BoundAssignmentOperator assignToTemp;

                            var replacement = _F.StoreToTemp(
                                expression,
                                out assignToTemp,
                                refKind: refKind,
                                kind: SynthesizedLocalKind.Spill,
                                syntaxOpt: _F.Syntax);

                            builder.AddLocal(replacement.LocalSymbol);
                            builder.AddStatement(_F.ExpressionStatement(assignToTemp));
                            return replacement;
                        }
                }
            }

            BoundExpression spillInlineArrayHelperWithTwoArguments(BoundSpillSequenceBuilder builder, BoundCall call)
            {
                Debug.Assert(call.Arguments.Length == 2);
                return call.Update(ImmutableArray.Create(Spill(builder, call.Arguments[0], call.ArgumentRefKindsOpt[0]),
                                                         call.Arguments[1].ConstantValueOpt is { } ? call.Arguments[1] : Spill(builder, call.Arguments[1])));
            }
        }

        internal static bool IsComplexConditionalInitializationOfReceiverRef(
            BoundAssignmentOperator assignment,
            out LocalSymbol outReceiverRefLocal,
            out BoundComplexConditionalReceiver outComplexReceiver,
            out BoundLocal outValueTypeReceiver,
            out BoundLocal outReferenceTypeReceiver)
        {
            if (assignment is
                {
                    IsRef: true,
                    Left: BoundLocal { LocalSymbol: { SynthesizedKind: SynthesizedLocalKind.LoweringTemp, RefKind: RefKind.Ref } receiverRefLocal },
                    Right: BoundComplexConditionalReceiver
                    {
                        ValueTypeReceiver: BoundLocal { LocalSymbol: { SynthesizedKind: SynthesizedLocalKind.LoweringTemp, RefKind: RefKind.Ref } } valueTypeReceiver,
                        ReferenceTypeReceiver: BoundSequence
                        {
                            Locals.IsEmpty: true,
                            SideEffects:
                            [
                            BoundAssignmentOperator
                            {
                                IsRef: false,
                                Left: BoundLocal { LocalSymbol: { SynthesizedKind: SynthesizedLocalKind.LoweringTemp, RefKind: RefKind.None } referenceTypeClone },
                                Right: BoundLocal { LocalSymbol: { SynthesizedKind: SynthesizedLocalKind.LoweringTemp, RefKind: RefKind.Ref } originalReceiverReference }
                            }
                            ],
                            Value: BoundLocal { LocalSymbol: { SynthesizedKind: SynthesizedLocalKind.LoweringTemp, RefKind: RefKind.None } } referenceTypeReceiver
                        }
                    } complexReceiver,
                }
                && (object)referenceTypeClone == referenceTypeReceiver.LocalSymbol
                && (object)originalReceiverReference == valueTypeReceiver.LocalSymbol
                && (object)receiverRefLocal != valueTypeReceiver.LocalSymbol
                && (object)receiverRefLocal != referenceTypeClone
                && receiverRefLocal.Type.IsTypeParameter()
                && !receiverRefLocal.Type.IsReferenceType
                && !receiverRefLocal.Type.IsValueType
                && valueTypeReceiver.Type.Equals(receiverRefLocal.Type, TypeCompareKind.AllIgnoreOptions)
                && referenceTypeReceiver.Type.Equals(receiverRefLocal.Type, TypeCompareKind.AllIgnoreOptions)
            )
            {
                outReceiverRefLocal = receiverRefLocal;
                outComplexReceiver = complexReceiver;
                outValueTypeReceiver = valueTypeReceiver;
                outReferenceTypeReceiver = referenceTypeReceiver;
                return true;
            }

            outReceiverRefLocal = null;
            outComplexReceiver = null;
            outValueTypeReceiver = null;
            outReferenceTypeReceiver = null;
            return false;
        }

        private ImmutableArray<BoundExpression> VisitExpressionList(
            ref BoundSpillSequenceBuilder builder,
            ImmutableArray<BoundExpression> args,
            ImmutableArray<RefKind> refKinds = default(ImmutableArray<RefKind>),
            bool forceSpill = false,
            bool sideEffectsOnly = false)
        {
            Debug.Assert(!sideEffectsOnly || refKinds.IsDefault);
            Debug.Assert(refKinds.IsDefault || refKinds.Length == args.Length);

            if (args.Length == 0)
            {
                return args;
            }

            var newList = VisitList(args);
            Debug.Assert(newList.Length == args.Length);

            int lastSpill;
            if (forceSpill)
            {
                lastSpill = newList.Length;
            }
            else
            {
                lastSpill = -1;
                for (int i = newList.Length - 1; i >= 0; i--)
                {
                    if (newList[i].Kind == SpillSequenceBuilderKind)
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
                builder = new BoundSpillSequenceBuilder(lastSpill < newList.Length ? (newList[lastSpill] as BoundSpillSequenceBuilder)?.Syntax : null);
            }

            var result = ArrayBuilder<BoundExpression>.GetInstance(newList.Length);

            // everything up until the last spill must be spilled entirely
            for (int i = 0; i < lastSpill; i++)
            {
                var refKind = refKinds.IsDefault ? RefKind.None : refKinds[i];
                var replacement = Spill(builder, newList[i], refKind, sideEffectsOnly);

                Debug.Assert(sideEffectsOnly || replacement != null);

                if (!sideEffectsOnly)
                {
                    result.Add(replacement);
                }
            }

            // the value of the last spill and everything that follows is not spilled
            if (lastSpill < newList.Length)
            {
                var lastSpillNode = (BoundSpillSequenceBuilder)newList[lastSpill];
                builder.Include(lastSpillNode);
                result.Add(lastSpillNode.Value);

                for (int i = lastSpill + 1; i < newList.Length; i++)
                {
                    result.Add(newList[i]);
                }
            }

            return result.ToImmutableAndFree();
        }

        #region Statement Visitors

        public override BoundNode VisitSwitchDispatch(BoundSwitchDispatch node)
        {
            BoundSpillSequenceBuilder builder = null;
            var expression = VisitExpression(ref builder, node.Expression);
            return UpdateStatement(builder, node.Update(expression, node.Cases, node.DefaultLabel, node.LengthBasedStringSwitchDataOpt));
        }

        public override BoundNode VisitThrowStatement(BoundThrowStatement node)
        {
            BoundSpillSequenceBuilder builder = null;
            BoundExpression expression = VisitExpression(ref builder, node.ExpressionOpt);
            return UpdateStatement(builder, node.Update(expression));
        }

        public override BoundNode VisitExpressionStatement(BoundExpressionStatement node)
        {
            BoundSpillSequenceBuilder builder = null;
            BoundExpression expr = VisitExpression(ref builder, node.Expression);
            Debug.Assert(expr != null);
            Debug.Assert(builder == null || builder.Value == null);
            return UpdateStatement(builder, node.Update(expr));
        }

        public override BoundNode VisitConditionalGoto(BoundConditionalGoto node)
        {
            BoundSpillSequenceBuilder builder = null;
            var condition = VisitExpression(ref builder, node.Condition);
            return UpdateStatement(builder, node.Update(condition, node.JumpIfTrue, node.Label));
        }

        public override BoundNode VisitReturnStatement(BoundReturnStatement node)
        {
            BoundSpillSequenceBuilder builder = null;
            var expression = VisitExpression(ref builder, node.ExpressionOpt);
            return UpdateStatement(builder, node.Update(node.RefKind, expression, @checked: node.Checked));
        }

        public override BoundNode VisitYieldReturnStatement(BoundYieldReturnStatement node)
        {
            BoundSpillSequenceBuilder builder = null;
            var expression = VisitExpression(ref builder, node.Expression);
            return UpdateStatement(builder, node.Update(expression));
        }

        public override BoundNode VisitCatchBlock(BoundCatchBlock node)
        {
            BoundExpression exceptionSourceOpt = (BoundExpression)this.Visit(node.ExceptionSourceOpt);
            var locals = node.Locals;

            var exceptionFilterPrologueOpt = node.ExceptionFilterPrologueOpt;
            Debug.Assert(exceptionFilterPrologueOpt is null); // it is introduced by this pass
            BoundSpillSequenceBuilder builder = null;
            var exceptionFilterOpt = VisitExpression(ref builder, node.ExceptionFilterOpt);
            if (builder is { })
            {
                Debug.Assert(builder.Value is null);
                locals = locals.AddRange(builder.GetLocals());
                exceptionFilterPrologueOpt = new BoundStatementList(node.Syntax, builder.GetStatements());
            }

            BoundBlock body = (BoundBlock)this.Visit(node.Body);
            TypeSymbol exceptionTypeOpt = this.VisitType(node.ExceptionTypeOpt);
            return node.Update(locals, exceptionSourceOpt, exceptionTypeOpt, exceptionFilterPrologueOpt, exceptionFilterOpt, body, node.IsSynthesizedAsyncCatchAll);
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
            // An await expression has already been wrapped in a BoundSpillSequence if not at the top level, so
            // the spilling will occur in the enclosing node.
            BoundSpillSequenceBuilder builder = null;
            var expr = VisitExpression(ref builder, node.Expression);
            return UpdateExpression(builder, node.Update(expr, node.AwaitableInfo, node.DebugInfo, node.Type));
        }

        public override BoundNode VisitSpillSequence(BoundSpillSequence node)
        {
            var builder = new BoundSpillSequenceBuilder(node.Syntax);

            // Ensure later errors (e.g. in async rewriting) are associated with the correct node.
            _F.Syntax = node.Syntax;

            builder.AddStatements(VisitList(node.SideEffects));
            builder.AddLocals(node.Locals);
            var value = VisitExpression(ref builder, node.Value);
            return builder.Update(value);
        }

        public override BoundNode VisitAddressOfOperator(BoundAddressOfOperator node)
        {
            BoundSpillSequenceBuilder builder = null;
            var expr = VisitExpression(ref builder, node.Operand);
            return UpdateExpression(builder, node.Update(expr, node.IsManaged, node.Type));
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
                    builder = new BoundSpillSequenceBuilder(indicesBuilder.Syntax);
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
                var boundsBuilder = new BoundSpillSequenceBuilder(builder.Syntax);
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

        public override BoundNode VisitConvertedStackAllocExpression(BoundConvertedStackAllocExpression node)
        {
            BoundSpillSequenceBuilder builder = null;
            BoundExpression count = VisitExpression(ref builder, node.Count);
            var initializerOpt = (BoundArrayInitialization)VisitExpression(ref builder, node.InitializerOpt);
            return UpdateExpression(builder, node.Update(node.ElementType, count, initializerOpt, node.Type));
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
            Debug.Assert(node.OperandPlaceholder is null);
            Debug.Assert(node.OperandConversion is null);
            return UpdateExpression(builder, node.Update(operand, node.TargetType, node.OperandPlaceholder, node.OperandConversion, node.Type));
        }

        public override BoundNode VisitAssignmentOperator(BoundAssignmentOperator node)
        {
            BoundSpillSequenceBuilder builder = null;
            var right = VisitExpression(ref builder, node.Right);

            BoundExpression left = node.Left;
            if (builder == null)
            {
                left = VisitExpression(ref builder, left);
            }
            else
            {
                // if the right-hand-side has await, spill the left
                var leftBuilder = new BoundSpillSequenceBuilder(builder.Syntax);

                switch (left.Kind)
                {
                    case BoundKind.Local:
                    case BoundKind.Parameter:
                        // locals and parameters are directly assignable, LHS is not on the stack so nothing to spill
                        break;

                    case BoundKind.FieldAccess:
                        var field = (BoundFieldAccess)left;
                        // static fields are directly assignable, LHS is not on the stack, nothing to spill
                        if (field.FieldSymbol.IsStatic) break;

                        // instance fields are directly assignable, but receiver is pushed, so need to spill that.
                        left = fieldWithSpilledReceiver(field, ref leftBuilder, isAssignmentTarget: true);
                        break;

                    case BoundKind.ArrayAccess:
                        var arrayAccess = (BoundArrayAccess)left;
                        // array and indices are pushed on stack so need to spill that
                        var expression = VisitExpression(ref leftBuilder, arrayAccess.Expression);
                        expression = Spill(leftBuilder, expression, RefKind.None);
                        var indices = this.VisitExpressionList(ref leftBuilder, arrayAccess.Indices, forceSpill: true);
                        left = arrayAccess.Update(expression, indices, arrayAccess.Type);
                        break;

                    default:
                        // must be something indirectly assignable, just visit and spill as an ordinary Ref  (not a RefReadOnly!!)
                        //
                        // NOTE: in some cases this will result in spiller producing an error.
                        //       For example if the LHS is a ref-returning method like
                        //
                        //       obj.RefReturning(a, b, c) = await Something();
                        //
                        //       the spiller would eventually have to spill the evaluation result of "refReturning" call as an ordinary Ref, 
                        //       which it can't.
                        left = Spill(leftBuilder, VisitExpression(ref leftBuilder, left), RefKind.Ref);
                        break;
                }

                leftBuilder.Include(builder);
                builder = leftBuilder;
            }

            return UpdateExpression(builder, node.Update(left, right, node.IsRef, node.Type));

            BoundExpression fieldWithSpilledReceiver(BoundFieldAccess field, ref BoundSpillSequenceBuilder leftBuilder, bool isAssignmentTarget)
            {
                var generateDummyFieldAccess = false;
                if (!field.FieldSymbol.IsStatic)
                {
                    Debug.Assert(field.ReceiverOpt is object);
                    BoundExpression receiver;
                    if (field.FieldSymbol.ContainingType.IsReferenceType)
                    {
                        // a reference type can always live across await so Spill using leftBuilder
                        receiver = Spill(leftBuilder, VisitExpression(ref leftBuilder, field.ReceiverOpt));

                        // dummy field access to trigger NRE
                        // a.b = c will trigger a NRE if a is null on assignment,
                        // but a.b.c = d will trigger a NRE if a is null before evaluating d
                        // so check whether we assign to the field directly
                        generateDummyFieldAccess = !isAssignmentTarget;
                    }
                    else if (field.ReceiverOpt is BoundArrayAccess arrayAccess)
                    {
                        // an arrayAccess returns a ref so can only be called after the await, but spill expression and indices
                        var expression = VisitExpression(ref leftBuilder, arrayAccess.Expression);
                        expression = Spill(leftBuilder, expression, RefKind.None);
                        var indices = this.VisitExpressionList(ref leftBuilder, arrayAccess.Indices, forceSpill: true);
                        receiver = arrayAccess.Update(expression, indices, arrayAccess.Type);
                        // dummy array access to trigger IndexOutRangeException or NRE
                        // we only need this if the array access is a receiver since
                        // a[0] = b triggers a NRE/IORE on assignment
                        // but a[0].b = c triggers an NRE/IORE before evaluating c
                        Spill(leftBuilder, receiver, sideEffectsOnly: true);
                    }
                    else if (field.ReceiverOpt is BoundFieldAccess receiverField)
                    {
                        receiver = fieldWithSpilledReceiver(receiverField, ref leftBuilder, isAssignmentTarget: false);
                    }
                    else
                    {
                        receiver = Spill(leftBuilder, VisitExpression(ref leftBuilder, field.ReceiverOpt), RefKind.Ref);
                    }

                    field = field.Update(receiver, field.FieldSymbol, field.ConstantValueOpt, field.ResultKind, field.Type);
                }

                if (generateDummyFieldAccess)
                {
                    Spill(leftBuilder, field, sideEffectsOnly: true);
                }

                return field;
            }
        }

        public override BoundNode VisitBadExpression(BoundBadExpression node)
        {
            // Cannot recurse into BadExpression children
            return node;
        }

        public override BoundNode VisitUserDefinedConditionalLogicalOperator(BoundUserDefinedConditionalLogicalOperator node)
        {
            throw ExceptionUtilities.Unreachable();
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
                var leftBuilder = new BoundSpillSequenceBuilder(builder.Syntax);
                left = VisitExpression(ref leftBuilder, node.Left);
                left = Spill(leftBuilder, left);
                if (node.OperatorKind == BinaryOperatorKind.LogicalBoolOr || node.OperatorKind == BinaryOperatorKind.LogicalBoolAnd)
                {
                    var tmp = _F.SynthesizedLocal(node.Type, kind: SynthesizedLocalKind.Spill, syntax: _F.Syntax);
                    leftBuilder.AddLocal(tmp);
                    leftBuilder.AddStatement(_F.Assignment(_F.Local(tmp), left));
                    leftBuilder.AddStatement(_F.If(
                        node.OperatorKind == BinaryOperatorKind.LogicalBoolAnd ? _F.Local(tmp) : _F.Not(_F.Local(tmp)),
                        UpdateStatement(builder, _F.Assignment(_F.Local(tmp), right))));

                    return UpdateExpression(leftBuilder, _F.Local(tmp));
                }
                else
                {
                    // if the right-hand-side has await, spill the left
                    leftBuilder.Include(builder);
                    builder = leftBuilder;
                }
            }

            return UpdateExpression(builder, node.Update(node.OperatorKind, node.ConstantValueOpt, node.Method, node.ConstrainedToType, node.ResultKind, left, right, node.Type));
        }

        public override BoundNode VisitCall(BoundCall node)
        {
            BoundSpillSequenceBuilder builder = null;
            var arguments = this.VisitExpressionList(ref builder, node.Arguments, node.ArgumentRefKindsOpt);

            BoundExpression receiver = null;
            if (builder == null || node.ReceiverOpt is BoundTypeExpression)
            {
                receiver = VisitExpression(ref builder, node.ReceiverOpt);
            }
            else if (node.Method.RequiresInstanceReceiver)
            {
                // spill the receiver if there were await expressions in the arguments
                var receiverBuilder = new BoundSpillSequenceBuilder(builder.Syntax);

                receiver = node.ReceiverOpt;
                RefKind refKind = ReceiverSpillRefKind(receiver);

                Debug.Assert(refKind == RefKind.None || !receiver.Type.IsReferenceType);

                receiver = Spill(receiverBuilder, VisitExpression(ref receiverBuilder, receiver), refKind: refKind);

                if (refKind != RefKind.None &&
                    CodeGenerator.IsPossibleReferenceTypeReceiverOfConstrainedCall(receiver) &&
                    !CodeGenerator.ReceiverIsKnownToReferToTempIfReferenceType(receiver) &&
                    !CodeGenerator.IsSafeToDereferenceReceiverRefAfterEvaluatingArguments(node.Arguments))
                {
                    var receiverType = receiver.Type;
                    Debug.Assert(!receiverType.IsReferenceType);

                    // A case where T is actually a class must be handled specially.
                    // Taking a reference to a class instance is fragile because the value behind the 
                    // reference might change while arguments are evaluated. However, the call should be
                    // performed on the instance that is behind reference at the time we push the
                    // reference to the stack. So, for a class we need to emit a reference to a temporary
                    // location, rather than to the original location

                    var save_Syntax = _F.Syntax;
                    _F.Syntax = node.Syntax;

                    var cache = _F.Local(_F.SynthesizedLocal(receiverType));
                    receiverBuilder.AddLocal(cache.LocalSymbol);
                    receiverBuilder.AddStatement(_F.ExpressionStatement(new BoundComplexConditionalReceiver(node.Syntax, cache, _F.Sequence(new[] { _F.AssignmentExpression(cache, receiver) }, cache), receiverType) { WasCompilerGenerated = true }));

                    receiver = _F.ComplexConditionalReceiver(receiver, cache);
                    _F.Syntax = save_Syntax;
                }

                receiverBuilder.Include(builder);
                builder = receiverBuilder;
            }

            return UpdateExpression(builder, node.Update(receiver, initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown, node.Method, arguments));
        }

        private static RefKind ReceiverSpillRefKind(BoundExpression receiver)
        {
            var result = RefKind.None;
            if (!receiver.Type.IsReferenceType && LocalRewriter.CanBePassedByReference(receiver))
            {
                result = receiver.Type.IsReadOnly ? RefKind.In : RefKind.Ref;
            }

            return result;
        }

        public override BoundNode VisitFunctionPointerInvocation(BoundFunctionPointerInvocation node)
        {
            BoundSpillSequenceBuilder builder = null;
            var arguments = this.VisitExpressionList(ref builder, node.Arguments, node.ArgumentRefKindsOpt);

            BoundExpression invokedExpression;
            if (builder == null)
            {
                invokedExpression = VisitExpression(ref builder, node.InvokedExpression);
            }
            else
            {
                var invokedExpressionBuilder = new BoundSpillSequenceBuilder(builder.Syntax);

                invokedExpression = Spill(invokedExpressionBuilder, VisitExpression(ref invokedExpressionBuilder, node.InvokedExpression));
                invokedExpressionBuilder.Include(builder);
                builder = invokedExpressionBuilder;
            }

            return UpdateExpression(builder, node.Update(invokedExpression, arguments, node.ArgumentRefKindsOpt, node.ResultKind, node.Type));
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
                return UpdateExpression(conditionBuilder, node.Update(node.IsRef, condition, consequence, alternative, node.ConstantValueOpt, node.NaturalTypeOpt, node.WasTargetTyped, node.Type));
            }

            if (conditionBuilder == null) conditionBuilder = new BoundSpillSequenceBuilder((consequenceBuilder ?? alternativeBuilder).Syntax);
            if (consequenceBuilder == null) consequenceBuilder = new BoundSpillSequenceBuilder(alternativeBuilder.Syntax);
            if (alternativeBuilder == null) alternativeBuilder = new BoundSpillSequenceBuilder(consequenceBuilder.Syntax);

            if (node.Type.IsVoidType())
            {
                conditionBuilder.AddStatement(
                    _F.If(condition,
                        UpdateStatement(consequenceBuilder, _F.ExpressionStatement(consequence)),
                        UpdateStatement(alternativeBuilder, _F.ExpressionStatement(alternative))));

                return conditionBuilder.Update(_F.Default(node.Type));
            }
            else
            {
                var tmp = _F.SynthesizedLocal(node.Type, kind: SynthesizedLocalKind.Spill, syntax: _F.Syntax,
                    refKind: node.GetRefKind());

                conditionBuilder.AddLocal(tmp);
                conditionBuilder.AddStatement(
                    _F.If(condition,
                        UpdateStatement(consequenceBuilder, _F.Assignment(_F.Local(tmp), consequence)),
                        UpdateStatement(alternativeBuilder, _F.Assignment(_F.Local(tmp), alternative))));

                return conditionBuilder.Update(_F.Local(tmp));
            }
        }

        public override BoundNode VisitConversion(BoundConversion node)
        {
            if (node.ConversionKind == ConversionKind.AnonymousFunction && node.Type.IsExpressionTree())
            {
                // Expression trees do not contain any code that requires spilling.
                return node;
            }

            BoundSpillSequenceBuilder builder = null;
            var operand = VisitExpression(ref builder, node.Operand);
            return UpdateExpression(
                builder,
                node.UpdateOperand(operand));
        }

        public override BoundNode VisitPassByCopy(BoundPassByCopy node)
        {
            BoundSpillSequenceBuilder builder = null;
            var expression = VisitExpression(ref builder, node.Expression);
            return UpdateExpression(
                builder,
                node.Update(
                    expression,
                    type: node.Type));
        }

        public override BoundNode VisitMethodGroup(BoundMethodGroup node)
        {
            throw ExceptionUtilities.Unreachable();
        }

        public override BoundNode VisitDelegateCreationExpression(BoundDelegateCreationExpression node)
        {
            BoundSpillSequenceBuilder builder = null;
            var argument = VisitExpression(ref builder, node.Argument);
            return UpdateExpression(builder, node.Update(argument, node.MethodOpt, node.IsExtensionMethod, node.WasTargetTyped, node.Type));
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
            return UpdateExpression(builder, node.Update(operand, node.TargetType, node.ConversionKind, node.Type));
        }

        public override BoundNode VisitMakeRefOperator(BoundMakeRefOperator node)
        {
            BoundSpillSequenceBuilder builder = null;
            var operand = VisitExpression(ref builder, node.Operand);
            return UpdateExpression(builder, node.Update(operand, node.Type));
        }

        public override BoundNode VisitNullCoalescingOperator(BoundNullCoalescingOperator node)
        {
            Debug.Assert(node.LeftPlaceholder is null);
            Debug.Assert(node.LeftConversion is null);
            BoundSpillSequenceBuilder builder = null;
            var right = VisitExpression(ref builder, node.RightOperand);
            BoundExpression left;
            if (builder == null)
            {
                left = VisitExpression(ref builder, node.LeftOperand);
            }
            else
            {
                var leftBuilder = new BoundSpillSequenceBuilder(builder.Syntax);
                left = VisitExpression(ref leftBuilder, node.LeftOperand);
                left = Spill(leftBuilder, left);

                var tmp = _F.SynthesizedLocal(node.Type, kind: SynthesizedLocalKind.Spill, syntax: _F.Syntax);
                leftBuilder.AddLocal(tmp);
                leftBuilder.AddStatement(_F.Assignment(_F.Local(tmp), left));
                leftBuilder.AddStatement(_F.If(
                    _F.ObjectEqual(_F.Local(tmp), _F.Null(left.Type)),
                    UpdateStatement(builder, _F.Assignment(_F.Local(tmp), right))));

                return UpdateExpression(leftBuilder, _F.Local(tmp));
            }

            return UpdateExpression(builder, node.Update(left, right, node.LeftPlaceholder, node.LeftConversion, node.OperatorResultKind, @checked: node.Checked, node.Type));
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
                return UpdateExpression(receiverBuilder, node.Update(receiver, node.HasValueMethodOpt, whenNotNull, whenNullOpt, node.Id, node.ForceCopyOfNullableValueType, node.Type));
            }

            if (receiverBuilder == null) receiverBuilder = new BoundSpillSequenceBuilder((whenNotNullBuilder ?? whenNullBuilder).Syntax);
            if (whenNotNullBuilder == null) whenNotNullBuilder = new BoundSpillSequenceBuilder(whenNullBuilder.Syntax);
            if (whenNullBuilder == null) whenNullBuilder = new BoundSpillSequenceBuilder(whenNotNullBuilder.Syntax);

            BoundExpression condition;
            if (receiver.Type.IsReferenceType || receiver.Type.IsValueType || receiverRefKind == RefKind.None)
            {
                // spill to a clone
                receiver = Spill(receiverBuilder, receiver, RefKind.None);
                var hasValueOpt = node.HasValueMethodOpt;

                if (hasValueOpt == null)
                {
                    condition = _F.IsNotNullReference(receiver);
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

                var clone = _F.SynthesizedLocal(receiver.Type, _F.Syntax, refKind: RefKind.None, kind: SynthesizedLocalKind.Spill);
                receiverBuilder.AddLocal(clone);

                //  (object)default(T) != null
                var isNotClass = _F.IsNotNullReference(_F.Default(receiver.Type));

                // isNotCalss || {clone = receiver; (object)clone != null}
                condition = _F.LogicalOr(
                                    isNotClass,
                                    _F.MakeSequence(
                                        _F.AssignmentExpression(_F.Local(clone), receiver),
                                        _F.IsNotNullReference(_F.Local(clone)))
                                    );

                receiver = _F.ComplexConditionalReceiver(receiver, _F.Local(clone));
            }

            if (node.Type.IsVoidType())
            {
                var whenNotNullStatement = UpdateStatement(whenNotNullBuilder, _F.ExpressionStatement(whenNotNull));
                whenNotNullStatement = ConditionalReceiverReplacer.Replace(whenNotNullStatement, receiver, node.Id, RecursionDepth);

                Debug.Assert(whenNullOpt == null || !LocalRewriter.ReadIsSideeffecting(whenNullOpt));

                receiverBuilder.AddStatement(_F.If(condition, whenNotNullStatement));

                return receiverBuilder.Update(_F.Default(node.Type));
            }
            else
            {
                var tmp = _F.SynthesizedLocal(node.Type, kind: SynthesizedLocalKind.Spill, syntax: _F.Syntax);
                var whenNotNullStatement = UpdateStatement(whenNotNullBuilder, _F.Assignment(_F.Local(tmp), whenNotNull));
                whenNotNullStatement = ConditionalReceiverReplacer.Replace(whenNotNullStatement, receiver, node.Id, RecursionDepth);

                whenNullOpt = whenNullOpt ?? _F.Default(node.Type);

                receiverBuilder.AddLocal(tmp);
                receiverBuilder.AddStatement(
                    _F.If(condition,
                        whenNotNullStatement,
                        UpdateStatement(whenNullBuilder, _F.Assignment(_F.Local(tmp), whenNullOpt))));

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

        public override BoundNode VisitLambda(BoundLambda node)
        {
            var oldCurrentFunction = _F.CurrentFunction;
            _F.CurrentFunction = node.Symbol;
            var result = base.VisitLambda(node);
            _F.CurrentFunction = oldCurrentFunction;
            return result;
        }

        public override BoundNode VisitLocalFunctionStatement(BoundLocalFunctionStatement node)
        {
            var oldCurrentFunction = _F.CurrentFunction;
            _F.CurrentFunction = node.Symbol;
            var result = base.VisitLocalFunctionStatement(node);
            _F.CurrentFunction = oldCurrentFunction;
            return result;
        }

        public override BoundNode VisitObjectCreationExpression(BoundObjectCreationExpression node)
        {
            Debug.Assert(node.InitializerExpressionOpt == null);
            BoundSpillSequenceBuilder builder = null;
            var arguments = this.VisitExpressionList(ref builder, node.Arguments, node.ArgumentRefKindsOpt);
            return UpdateExpression(builder, node.Update(node.Constructor, arguments, node.ArgumentNamesOpt, node.ArgumentRefKindsOpt, node.Expanded, node.ArgsToParamsOpt, node.DefaultArguments, node.ConstantValueOpt, node.InitializerExpressionOpt, node.Type));
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
                var expressionBuilder = new BoundSpillSequenceBuilder(builder.Syntax);
                expression = VisitExpression(ref expressionBuilder, node.Expression);
                expression = Spill(expressionBuilder, expression);
                expressionBuilder.Include(builder);
                builder = expressionBuilder;
            }

            return UpdateExpression(builder, node.Update(expression, index, node.Checked, node.RefersToLocation, node.Type));
        }

        public override BoundNode VisitPointerIndirectionOperator(BoundPointerIndirectionOperator node)
        {
            BoundSpillSequenceBuilder builder = null;
            var operand = VisitExpression(ref builder, node.Operand);
            return UpdateExpression(builder, node.Update(operand, node.RefersToLocation, node.Type));
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
                builder = new BoundSpillSequenceBuilder(valueBuilder.Syntax);
            }

            PromoteAndAddLocals(builder, node.Locals);
            builder.AddExpressions(sideEffects);
            builder.Include(valueBuilder);

            return builder.Update(value);
        }

        public override BoundNode VisitThrowExpression(BoundThrowExpression node)
        {
            BoundSpillSequenceBuilder builder = null;
            BoundExpression operand = VisitExpression(ref builder, node.Expression);
            return UpdateExpression(builder, node.Update(operand, node.Type));
        }

        /// <summary>
        /// If an expression node that declares synthesized short-lived locals (currently only sequence) contains
        /// a spill sequence (from an await or switch expression), these locals become long-lived since their
        /// values may be read by code that follows. We promote these variables to long-lived of kind
        /// <see cref="SynthesizedLocalKind.Spill"/>. 
        /// </summary>
        private void PromoteAndAddLocals(BoundSpillSequenceBuilder builder, ImmutableArray<LocalSymbol> locals)
        {
            foreach (var local in locals)
            {
                if (local.SynthesizedKind.IsLongLived())
                {
                    builder.AddLocal(local);
                }
                else if (!_receiverSubstitution.ContainsKey(local))
                {
                    LocalSymbol longLived = local.WithSynthesizedLocalKindAndSyntax(SynthesizedLocalKind.Spill, _F.Syntax);
                    _tempSubstitution.Add(local, longLived);
                    builder.AddLocal(longLived);
                }
            }
        }

        public override BoundNode VisitUnaryOperator(BoundUnaryOperator node)
        {
            BoundSpillSequenceBuilder builder = null;
            BoundExpression operand = VisitExpression(ref builder, node.Operand);
            return UpdateExpression(builder, node.Update(node.OperatorKind, operand, node.ConstantValueOpt, node.MethodOpt, node.ConstrainedToTypeOpt, node.ResultKind, node.Type));
        }

        public override BoundNode VisitReadOnlySpanFromArray(BoundReadOnlySpanFromArray node)
        {
            BoundSpillSequenceBuilder builder = null;
            BoundExpression operand = VisitExpression(ref builder, node.Operand);
            return UpdateExpression(builder, node.Update(operand, node.ConversionMethod, node.Type));
        }

        public override BoundNode VisitSequencePointExpression(BoundSequencePointExpression node)
        {
            BoundSpillSequenceBuilder builder = null;
            BoundExpression expression = VisitExpression(ref builder, node.Expression);
            return UpdateExpression(builder, node.Update(expression, node.Type));
        }

        #endregion
    }
}
